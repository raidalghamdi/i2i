import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AvgTimePerStageTableComponent } from './avg-time-per-stage-table.component';

describe('AvgTimePerStageTableComponent', () => {
  let fixture: ComponentFixture<AvgTimePerStageTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AvgTimePerStageTableComponent] }).compileComponents();
    fixture = TestBed.createComponent(AvgTimePerStageTableComponent);
  });

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('filters out stage 0, renders one row per remaining stage with a stage badge, mini-bar and formatted value', () => {
    const data = [
      { stage: 0, avgDays: 12.345 },
      { stage: 1, avgDays: 3.2 },
      { stage: 2, avgDays: 6.789 },
    ];
    fixture.componentRef.setInput('data', data);
    fixture.detectChanges();

    const rows = el().querySelectorAll('tbody tr');
    expect(rows.length).toBe(2); // stage 0 filtered out

    const badges = el().querySelectorAll('[data-stage-badge]');
    expect(badges.length).toBe(2);
    expect(badges[0].textContent?.trim()).toBe('1');
    expect(badges[1].textContent?.trim()).toBe('2');

    const bars = Array.from(el().querySelectorAll('[data-mini-bar-fill]')) as HTMLElement[];
    expect(bars.length).toBe(2);
    // max avgDays among remaining rows = 6.789 -> full 100%
    expect(bars[1].style.width).toBe('100%');
    expect(bars[1].style.background).toBe('rgb(27, 71, 77)'); // #1B474D

    const text = el().textContent ?? '';
    expect(text).toContain('3.2');
    expect(text).toContain('6.8');
  });
});
