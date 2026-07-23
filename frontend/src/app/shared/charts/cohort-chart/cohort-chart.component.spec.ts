import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CohortChartComponent } from './cohort-chart.component';

describe('CohortChartComponent', () => {
  let fixture: ComponentFixture<CohortChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CohortChartComponent] }).compileComponents();
    fixture = TestBed.createComponent(CohortChartComponent);
  });

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('renders rows*4 series rects with the per-series colors, a legend, and truncated month labels', () => {
    const data = [
      { month: '2026-01-01', submitted: 10, approved: 4, rejected: 2, implemented: 1 },
      { month: '2026-02-01', submitted: 8, approved: 3, rejected: 1, implemented: 2 },
      { month: '2026-03-01', submitted: 12, approved: 5, rejected: 3, implemented: 4 },
    ];
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const svg = el().querySelector('svg')!;
    // W = max(rows*84, 320) = max(252, 320) = 320
    expect(svg.getAttribute('width')).toBe('320');
    expect(svg.getAttribute('height')).toBe('200');

    const rects = Array.from(el().querySelectorAll('rect'));
    expect(rects.length).toBe(data.length * 4);

    const colors = rects.map((r) => r.getAttribute('fill'));
    expect(colors.filter((c) => c === '#20808D').length).toBe(3); // submitted
    expect(colors.filter((c) => c === '#1B474D').length).toBe(3); // approved
    expect(colors.filter((c) => c === '#A84B2F').length).toBe(3); // rejected
    expect(colors.filter((c) => c === '#944454').length).toBe(3); // implemented

    // legend swatches: 4 series
    const legendItems = el().querySelectorAll('[data-legend-item]');
    expect(legendItems.length).toBe(4);

    const text = el().textContent ?? '';
    expect(text).toContain('2026-01');
    expect(text).not.toContain('2026-01-01');
  });
});
