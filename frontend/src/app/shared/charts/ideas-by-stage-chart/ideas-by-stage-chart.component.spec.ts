import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IdeasByStageChartComponent } from './ideas-by-stage-chart.component';

describe('IdeasByStageChartComponent', () => {
  let fixture: ComponentFixture<IdeasByStageChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [IdeasByStageChartComponent] }).compileComponents();
    fixture = TestBed.createComponent(IdeasByStageChartComponent);
  });

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('renders one rect per row with the exact geometry constants and teal fill', () => {
    fixture.componentRef.setInput('data', [
      { stage: 0, count: 5 },
      { stage: 1, count: 10 },
      { stage: 2, count: 0 },
    ]);
    fixture.componentRef.setInput('stageLabels', ['Draft', 'Submitted', 'Screening']);
    fixture.detectChanges();

    const svg = el().querySelector('svg')!;
    expect(svg).toBeTruthy();
    // W = max(rows*56, 320) = max(168, 320) = 320
    expect(svg.getAttribute('width')).toBe('320');
    expect(svg.getAttribute('height')).toBe('220');

    const rects = Array.from(el().querySelectorAll('rect'));
    expect(rects.length).toBe(3);
    rects.forEach((r) => {
      expect(r.getAttribute('fill')).toBe('#20808D');
      expect(r.getAttribute('rx')).toBe('3');
    });

    // baseline line present
    expect(el().querySelectorAll('line').length).toBe(1);

    const text = el().textContent ?? '';
    expect(text).toContain('5');
    expect(text).toContain('10');
    expect(text).toContain('Draft');
    expect(text).toContain('Submitted');
    expect(text).toContain('Screening');
  });

  it('sizes width from row count: rows*56 when it exceeds the 320 floor', () => {
    const data = Array.from({ length: 9 }, (_, i) => ({ stage: i, count: i + 1 }));
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const svg = el().querySelector('svg')!;
    expect(svg.getAttribute('width')).toBe(String(9 * 56));
  });

  it('shows an empty state and renders no chart when total count is zero', () => {
    fixture.componentRef.setInput('data', [
      { stage: 0, count: 0 },
      { stage: 1, count: 0 },
    ]);
    fixture.detectChanges();

    expect(el().querySelector('svg')).toBeNull();
    expect((el().textContent ?? '').trim().length).toBeGreaterThan(0);
  });
});
