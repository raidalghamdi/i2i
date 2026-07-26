import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';

interface NetworkNode {
  x: number;
  y: number;
  vx: number;
  vy: number;
  r: number;
  gold: boolean;
}

const NODE_COUNT_DESKTOP = 46;
const NODE_COUNT_MOBILE = 28;
const CONNECT_DIST = 140;
const GOLD_RATIO = 0.14;
const MIN_SPEED = 0.15;
const MAX_SPEED = 0.4;
const MIN_SEPARATION = 24;
const TEAL_FALLBACK = '#7CE0DA';
const GOLD_FALLBACK = '#F5B843';

/**
 * Animated node/edge network canvas — the hero section's full-bleed
 * background. Ported verbatim (physics constants, repulsion, periodic
 * jitter, edge bounce) from the legacy app's hero-network.tsx so the two
 * systems' hero sections are visually identical, not just structurally.
 */
@Component({
  selector: 'app-hero-network',
  templateUrl: './hero-network.component.html',
  host: { class: 'pointer-events-none absolute inset-0 block h-full w-full', 'aria-hidden': 'true' },
})
export class HeroNetworkComponent implements AfterViewInit, OnDestroy {
  @ViewChild('canvasEl', { static: true }) private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private raf = 0;
  private resizeListener?: () => void;

  ngAfterViewInit(): void {
    const canvas = this.canvasRef.nativeElement;
    const ctx = canvas.getContext('2d');
    const parent = canvas.parentElement;
    if (!ctx || !parent) return;

    const prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const styles = getComputedStyle(document.documentElement);
    const teal = styles.getPropertyValue('--brand-cyan-light').trim() || TEAL_FALLBACK;
    const gold = styles.getPropertyValue('--brand-gold').trim() || GOLD_FALLBACK;

    let width = parent.clientWidth;
    let height = parent.clientHeight;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);

    const setSize = () => {
      width = parent.clientWidth;
      height = parent.clientHeight;
      canvas.width = width * dpr;
      canvas.height = height * dpr;
      canvas.style.width = `${width}px`;
      canvas.style.height = `${height}px`;
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };
    setSize();

    const isMobile = width < 640;
    const count = isMobile ? NODE_COUNT_MOBILE : NODE_COUNT_DESKTOP;
    const randomVelocity = () => {
      const angle = Math.random() * Math.PI * 2;
      const speed = MIN_SPEED + Math.random() * (MAX_SPEED - MIN_SPEED);
      return { vx: Math.cos(angle) * speed, vy: Math.sin(angle) * speed };
    };
    const nodes: NetworkNode[] = Array.from({ length: count }, () => {
      const { vx, vy } = randomVelocity();
      return {
        x: Math.random() * width,
        y: Math.random() * height,
        vx,
        vy,
        r: 1.6 + Math.random() * 1.8,
        gold: Math.random() < GOLD_RATIO,
      };
    });

    let frame = 0;

    const draw = () => {
      ctx.clearRect(0, 0, width, height);

      // Edges — draw first so nodes sit on top.
      for (let i = 0; i < nodes.length; i++) {
        for (let j = i + 1; j < nodes.length; j++) {
          const dx = nodes[i].x - nodes[j].x;
          const dy = nodes[i].y - nodes[j].y;
          const d = Math.sqrt(dx * dx + dy * dy);
          if (d > CONNECT_DIST) continue;
          const alpha = 1 - d / CONNECT_DIST;
          ctx.strokeStyle = hexToRgba(teal, alpha * 0.35);
          ctx.lineWidth = 0.6;
          ctx.beginPath();
          ctx.moveTo(nodes[i].x, nodes[i].y);
          ctx.lineTo(nodes[j].x, nodes[j].y);
          ctx.stroke();
        }
      }

      // Nodes
      for (const n of nodes) {
        ctx.fillStyle = n.gold ? gold : teal;
        ctx.beginPath();
        ctx.arc(n.x, n.y, n.r, 0, Math.PI * 2);
        ctx.fill();
        if (n.gold) {
          ctx.strokeStyle = hexToRgba(gold, 0.35);
          ctx.lineWidth = 1;
          ctx.beginPath();
          ctx.arc(n.x, n.y, n.r + 3, 0, Math.PI * 2);
          ctx.stroke();
        }
      }
    };

    const tick = () => {
      frame++;

      // Mutual repulsion — keeps nodes from stagnating on top of each other.
      for (let i = 0; i < nodes.length; i++) {
        for (let j = i + 1; j < nodes.length; j++) {
          const dx = nodes[j].x - nodes[i].x;
          const dy = nodes[j].y - nodes[i].y;
          const d2 = dx * dx + dy * dy;
          if (d2 > MIN_SEPARATION * MIN_SEPARATION) continue;
          const d = Math.max(Math.sqrt(d2), 0.5);
          const push = (MIN_SEPARATION - d) / MIN_SEPARATION;
          const ux = dx / d;
          const uy = dy / d;
          const impulse = 0.02 * push;
          nodes[i].vx -= ux * impulse;
          nodes[i].vy -= uy * impulse;
          nodes[j].vx += ux * impulse;
          nodes[j].vy += uy * impulse;
        }
      }

      for (const n of nodes) {
        n.x += n.vx;
        n.y += n.vy;

        if (n.x < 0) {
          n.x = 0;
          n.vx = Math.abs(n.vx);
        } else if (n.x > width) {
          n.x = width;
          n.vx = -Math.abs(n.vx);
        }
        if (n.y < 0) {
          n.y = 0;
          n.vy = Math.abs(n.vy);
        } else if (n.y > height) {
          n.y = height;
          n.vy = -Math.abs(n.vy);
        }

        // Clamp speed so repulsion impulses can't runaway.
        const speed = Math.sqrt(n.vx * n.vx + n.vy * n.vy);
        if (speed > MAX_SPEED) {
          n.vx = (n.vx / speed) * MAX_SPEED;
          n.vy = (n.vy / speed) * MAX_SPEED;
        } else if (speed < MIN_SPEED / 2) {
          const v = randomVelocity();
          n.vx = v.vx;
          n.vy = v.vy;
        }
      }

      // Every ~4 seconds, nudge every node so long-lived sessions never
      // settle into a static pattern.
      if (frame % 240 === 0) {
        for (const n of nodes) {
          n.vx += (Math.random() - 0.5) * 0.05;
          n.vy += (Math.random() - 0.5) * 0.05;
        }
      }

      draw();
      this.raf = requestAnimationFrame(tick);
    };

    if (prefersReduced) {
      draw();
    } else {
      this.raf = requestAnimationFrame(tick);
    }

    this.resizeListener = () => {
      setSize();
      for (const n of nodes) {
        n.x = Math.min(Math.max(n.x, 0), width);
        n.y = Math.min(Math.max(n.y, 0), height);
      }
      draw();
    };
    window.addEventListener('resize', this.resizeListener);
  }

  ngOnDestroy(): void {
    if (this.resizeListener) window.removeEventListener('resize', this.resizeListener);
    if (this.raf) cancelAnimationFrame(this.raf);
  }
}

// Convert #RRGGBB (or rgb() as raw text from CSS vars) to rgba(..., alpha).
function hexToRgba(color: string, alpha: number): string {
  const c = color.trim();
  if (c.startsWith('#')) {
    const hex = c.slice(1);
    const full = hex.length === 3 ? hex.split('').map((h) => h + h).join('') : hex;
    const r = parseInt(full.slice(0, 2), 16);
    const g = parseInt(full.slice(2, 4), 16);
    const b = parseInt(full.slice(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
  }
  if (c.startsWith('rgb')) {
    return c.replace(/^rgb\s*\(?/, 'rgba(').replace(/\)$/, `, ${alpha})`);
  }
  return c;
}
