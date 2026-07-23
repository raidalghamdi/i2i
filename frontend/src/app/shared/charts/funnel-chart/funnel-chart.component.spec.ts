import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FunnelChartComponent } from './funnel-chart.component';

describe('FunnelChartComponent', () => {
  let fixture: ComponentFixture<FunnelChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FunnelChartComponent] }).compileComponents();
    fixture = TestBed.createComponent(FunnelChartComponent);
  });

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('renders one CSS-div row per entry with a teal fill bar sized to the max, floored at 6%', () => {
    const data = [
      { label: 'Participation', count: 100 },
      { label: 'Evaluated', count: 60 },
      { label: 'Approved', count: 20 },
      { label: 'Piloted', count: 5 },
      { label: 'Scaled', count: 0 },
    ];
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const rows = el().querySelectorAll('[data-funnel-row]');
    expect(rows.length).toBe(5);

    const fills = Array.from(el().querySelectorAll('[data-funnel-fill]')) as HTMLElement[];
    expect(fills.length).toBe(5);
    expect(fills[0].classList.contains('bg-brand-teal')).toBeTrue();

    // max is 100 -> full 100%
    expect(fills[0].style.width).toBe('100%');
    // zero count still floors at 6%
    expect(fills[4].style.width).toBe('6%');

    const text = el().textContent ?? '';
    expect(text).toContain('Participation');
    expect(text).toContain('Scaled');
  });
});
