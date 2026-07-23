import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoadingStateComponent } from './loading-state.component';

describe('LoadingStateComponent', () => {
  let fixture: ComponentFixture<LoadingStateComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [LoadingStateComponent] }).compileComponents();
    fixture = TestBed.createComponent(LoadingStateComponent);
  });

  it('renders the given label in spinner variant', () => {
    fixture.componentRef.setInput('label', 'Fetching ideas…');
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Fetching ideas…');
  });

  it('renders a default label when none is given', () => {
    fixture.detectChanges();
    const text = ((fixture.nativeElement as HTMLElement).textContent ?? '').trim();
    expect(text.length).toBeGreaterThan(0);
  });

  it('renders a centered CSS spinner (not an icon) in the default variant', () => {
    fixture.detectChanges();
    const spinner = (fixture.nativeElement as HTMLElement).querySelector('.animate-spin');
    expect(spinner).toBeTruthy();
    expect((fixture.nativeElement as HTMLElement).querySelector('app-icon')).toBeFalsy();
  });

  it('renders `rows` skeleton bars in skeleton variant', () => {
    fixture.componentRef.setInput('variant', 'skeleton');
    fixture.componentRef.setInput('rows', 5);
    fixture.detectChanges();
    const bars = (fixture.nativeElement as HTMLElement).querySelectorAll('.animate-pulse');
    expect(bars.length).toBe(5);
  });

  it('defaults to 3 skeleton bars when rows is not set', () => {
    fixture.componentRef.setInput('variant', 'skeleton');
    fixture.detectChanges();
    const bars = (fixture.nativeElement as HTMLElement).querySelectorAll('.animate-pulse');
    expect(bars.length).toBe(3);
  });
});
