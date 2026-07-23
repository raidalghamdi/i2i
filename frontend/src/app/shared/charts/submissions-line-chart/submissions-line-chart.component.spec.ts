import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { SubmissionsLineChartComponent } from './submissions-line-chart.component';

describe('SubmissionsLineChartComponent', () => {
  let fixture: ComponentFixture<SubmissionsLineChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubmissionsLineChartComponent],
      providers: [{ provide: LOCALE_ID, useValue: 'en' }],
    }).compileComponents();
    fixture = TestBed.createComponent(SubmissionsLineChartComponent);
  });

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function daysData(count: number): { date: string; count: number }[] {
    return Array.from({ length: count }, (_, i) => ({
      date: new Date(Date.UTC(2026, 0, i + 1)).toISOString().slice(0, 10),
      count: i % 3 === 0 ? 0 : i + 1,
    }));
  }

  it('renders the fixed W=760/H=220 geometry with the gradient, area+line paths, gridlines and dots', () => {
    const data = daysData(30);
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const svg = el().querySelector('svg')!;
    expect(svg.getAttribute('width')).toBe('760');
    expect(svg.getAttribute('height')).toBe('220');

    const gradient = el().querySelector('linearGradient#ideas-line-fill');
    expect(gradient).toBeTruthy();
    expect(gradient!.querySelectorAll('stop').length).toBe(2);

    // area path + line path
    const paths = Array.from(el().querySelectorAll('path'));
    expect(paths.length).toBe(2);
    const linePath = paths.find((p) => p.getAttribute('stroke') === '#20808D');
    expect(linePath).toBeTruthy();
    expect(linePath!.getAttribute('stroke-width')).toBe('1.75');
    expect(linePath!.getAttribute('fill')).toBe('none');

    const areaPath = paths.find((p) => p.getAttribute('fill') === 'url(#ideas-line-fill)');
    expect(areaPath).toBeTruthy();

    // 5 gridlines, one solid baseline + 4 dashed
    const lines = Array.from(el().querySelectorAll('line'));
    expect(lines.length).toBe(5);
    const dashed = lines.filter((l) => l.getAttribute('stroke-dasharray') === '3 3');
    expect(dashed.length).toBe(4);
    const solid = lines.filter((l) => !l.getAttribute('stroke-dasharray'));
    expect(solid.length).toBe(1);

    // dots only for count > 0
    const nonZero = data.filter((d) => d.count > 0).length;
    expect(el().querySelectorAll('circle').length).toBe(nonZero);

    // ~6 x-axis date labels: labelStep = max(1, floor(rows/6))
    const labelStep = Math.max(1, Math.floor(data.length / 6));
    const expectedLabels = data.filter((_, i) => i % labelStep === 0).length;
    const dateLabels = el().querySelectorAll('text.chart-x-label');
    expect(dateLabels.length).toBe(expectedLabels);
  });
});
