import { Component, OnDestroy, OnInit, computed, input, signal } from '@angular/core';
import { IconComponent } from '../../shared/icon/icon.component';

@Component({
  selector: 'app-hero-rotator',
  imports: [IconComponent],
  templateUrl: './hero-rotator.component.html',
})
export class HeroRotatorComponent implements OnInit, OnDestroy {
  readonly words = input.required<string[]>();
  readonly activeIndex = signal(0);
  private intervalId: ReturnType<typeof setInterval> | undefined;

  readonly longestWord = computed(() =>
    this.words().reduce((longest, w) => (w.length > longest.length ? w : longest), ''),
  );

  ngOnInit(): void {
    this.intervalId = setInterval(() => {
      this.activeIndex.update((i) => (i + 1) % this.words().length);
    }, 2000);
  }

  ngOnDestroy(): void {
    if (this.intervalId) clearInterval(this.intervalId);
  }
}
