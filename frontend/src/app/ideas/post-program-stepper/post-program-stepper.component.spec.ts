import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PostProgramStepperComponent } from './post-program-stepper.component';

describe('PostProgramStepperComponent', () => {
  let fixture: ComponentFixture<PostProgramStepperComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [PostProgramStepperComponent] });
    fixture = TestBed.createComponent(PostProgramStepperComponent);
  });

  it('renders three steps', () => {
    fixture.componentRef.setInput('status', 'approved');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.pp-step').length).toBe(3);
  });

  it('marks nothing current at approved', () => {
    fixture.componentRef.setInput('status', 'approved');
    fixture.detectChanges();
    expect(fixture.componentInstance.steps().every((s) => s.state === 'upcoming')).toBe(true);
  });

  it('marks pilot current and later steps upcoming at in_pilot', () => {
    fixture.componentRef.setInput('status', 'in_pilot');
    fixture.detectChanges();
    const s = fixture.componentInstance.steps();
    expect(s[0].state).toBe('current');
    expect(s[1].state).toBe('upcoming');

    const items: NodeListOf<HTMLElement> = fixture.nativeElement.querySelectorAll('.pp-step');
    expect(items[0].getAttribute('aria-current')).toBe('step');
    expect(items[1].getAttribute('aria-current')).toBeNull();
    expect(items[2].getAttribute('aria-current')).toBeNull();
  });

  it('marks pilot done and scaling current at in_scaling', () => {
    fixture.componentRef.setInput('status', 'in_scaling');
    fixture.detectChanges();
    const s = fixture.componentInstance.steps();
    expect(s[0].state).toBe('done');
    expect(s[1].state).toBe('done');
    expect(s[2].state).toBe('current');
  });
});
