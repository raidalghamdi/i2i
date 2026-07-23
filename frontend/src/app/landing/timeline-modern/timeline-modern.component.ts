import { AfterViewInit, Component, ElementRef, OnDestroy, QueryList, ViewChildren, input, signal } from '@angular/core';

export interface TimelineStage {
  id: string;
  title: string;
  date: string;
  description: string;
  tone: 'cyan' | 'gold';
}

@Component({
  selector: 'app-timeline-modern',
  templateUrl: './timeline-modern.component.html',
})
export class TimelineModernComponent implements AfterViewInit, OnDestroy {
  readonly stages = input.required<TimelineStage[]>();
  readonly visibleIds = signal<ReadonlySet<string>>(new Set());

  @ViewChildren('stageEl') private stageEls!: QueryList<ElementRef<HTMLElement>>;
  private observer: IntersectionObserver | undefined;

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            const id = (entry.target as HTMLElement).dataset['stageId'];
            if (id) this.visibleIds.update((set) => new Set(set).add(id));
            this.observer?.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.2, rootMargin: '0px 0px -10% 0px' },
    );
    this.stageEls.forEach((el) => this.observer?.observe(el.nativeElement));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  isEnd(index: number): boolean {
    return index % 2 === 1;
  }
}
