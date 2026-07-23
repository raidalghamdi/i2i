import { ComponentFixture, TestBed } from '@angular/core/testing';
import { JourneyStage } from '../idea.model';
import { IdeaJourneyTimelineComponent } from './idea-journey-timeline.component';

describe('IdeaJourneyTimelineComponent', () => {
  let fixture: ComponentFixture<IdeaJourneyTimelineComponent>;

  function stagesFixture(): JourneyStage[] {
    const labels = [
      'Idea Submission', 'Initial Screening', 'Technical Evaluation', 'Committee Review',
      'Approval', 'Pilot Implementation', 'Measurement & Impact', 'Scale & Adoption',
    ];
    return labels.map((en, index) => ({
      index,
      state: index < 3 ? 'completed' : index === 3 ? 'current' : 'upcoming',
      label: { ar: en, en },
      completedAt: null,
    }));
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [IdeaJourneyTimelineComponent] });
    fixture = TestBed.createComponent(IdeaJourneyTimelineComponent);
  });

  it('renders eight steps', () => {
    fixture.componentRef.setInput('stages', stagesFixture());
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.ij-step').length).toBe(8);
  });

  it('marks completed, current and upcoming states with the right classes', () => {
    fixture.componentRef.setInput('stages', stagesFixture());
    fixture.detectChanges();
    const steps = fixture.nativeElement.querySelectorAll('.ij-step');
    expect(steps[0].classList).toContain('completed');
    expect(steps[3].classList).toContain('current');
    expect(steps[7].classList).toContain('upcoming');
  });

  it('renders a stop pill on a stopped stage', () => {
    const stages = stagesFixture();
    stages[2] = { ...stages[2], state: 'stopped' };
    fixture.componentRef.setInput('stages', stages);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.ij-stop-pill')).toBeTruthy();
  });
});
