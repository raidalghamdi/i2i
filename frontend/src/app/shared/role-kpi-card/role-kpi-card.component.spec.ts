import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RoleKpiCardComponent } from './role-kpi-card.component';

describe('RoleKpiCardComponent', () => {
  let fixture: ComponentFixture<RoleKpiCardComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [RoleKpiCardComponent], providers: [provideRouter([])] }).compileComponents();
    fixture = TestBed.createComponent(RoleKpiCardComponent);
  });
  it('renders label, value and hint', () => {
    fixture.componentRef.setInput('label', 'My Ideas');
    fixture.componentRef.setInput('value', 7);
    fixture.componentRef.setInput('hint', '3 pts');
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('My Ideas');
    expect(text).toContain('7');
    expect(text).toContain('3 pts');
  });
});
