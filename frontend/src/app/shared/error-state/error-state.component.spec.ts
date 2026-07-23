import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ErrorStateComponent } from './error-state.component';

describe('ErrorStateComponent', () => {
  let fixture: ComponentFixture<ErrorStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ErrorStateComponent] }).compileComponents();
    fixture = TestBed.createComponent(ErrorStateComponent);
  });

  it('renders the given message', () => {
    fixture.componentRef.setInput('message', 'Failed to load ideas.');
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Failed to load ideas.');
  });

  it('renders a default message when none is given', () => {
    fixture.detectChanges();
    const text = ((fixture.nativeElement as HTMLElement).textContent ?? '').trim();
    expect(text.length).toBeGreaterThan(0);
  });

  it('emits retry when the button is clicked', () => {
    fixture.detectChanges();
    let emitted = false;
    fixture.componentInstance.retry.subscribe(() => (emitted = true));
    const button = (fixture.nativeElement as HTMLElement).querySelector('button');
    button?.dispatchEvent(new Event('click'));
    expect(emitted).toBe(true);
  });
});
