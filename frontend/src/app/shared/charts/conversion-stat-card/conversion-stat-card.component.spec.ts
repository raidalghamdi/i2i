import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConversionStatCardComponent } from './conversion-stat-card.component';

describe('ConversionStatCardComponent', () => {
  let fixture: ComponentFixture<ConversionStatCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ConversionStatCardComponent] }).compileComponents();
    fixture = TestBed.createComponent(ConversionStatCardComponent);
  });

  function el(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('renders the big rate %, the gradient meter clamped to [0,100], and the two stat boxes', () => {
    fixture.componentRef.setInput('rate', 42.5);
    fixture.componentRef.setInput('submitted', 200);
    fixture.componentRef.setInput('pilot', 85);
    fixture.detectChanges();

    const text = el().textContent ?? '';
    expect(text).toContain('42.5');
    expect(text).toContain('%');
    expect(text).toContain('200');
    expect(text).toContain('85');

    const meterFill = el().querySelector('[data-meter-fill]') as HTMLElement;
    expect(meterFill).toBeTruthy();
    expect(meterFill.style.width).toBe('42.5%');
    expect(meterFill.style.background).toContain('linear-gradient');

    const statBoxes = el().querySelectorAll('[data-stat-box]');
    expect(statBoxes.length).toBe(2);
  });

  it('clamps the meter width to 100 when rate exceeds 100', () => {
    fixture.componentRef.setInput('rate', 150);
    fixture.componentRef.setInput('submitted', 10);
    fixture.componentRef.setInput('pilot', 15);
    fixture.detectChanges();

    const meterFill = el().querySelector('[data-meter-fill]') as HTMLElement;
    expect(meterFill.style.width).toBe('100%');
  });
});
