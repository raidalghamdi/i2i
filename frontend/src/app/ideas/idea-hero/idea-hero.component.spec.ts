import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IdeaJourney } from '../idea.model';
import { IdeaHeroComponent } from './idea-hero.component';

describe('IdeaHeroComponent', () => {
  let fixture: ComponentFixture<IdeaHeroComponent>;

  const journey: IdeaJourney = {
    currentStage: 3, stopped: false, evaluationScore: null,
    stages: Array.from({ length: 8 }, (_, index) => ({
      index, state: index < 3 ? 'completed' : index === 3 ? 'current' : 'upcoming',
      label: { ar: 'x', en: 'Stage' }, completedAt: null,
    })),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [IdeaHeroComponent] });
    fixture = TestBed.createComponent(IdeaHeroComponent);
  });

  it('shows the code, title, chips and embeds the timeline', () => {
    fixture.componentRef.setInput('code', 'IDEA-0001');
    fixture.componentRef.setInput('title', 'My Idea');
    fixture.componentRef.setInput('status', 'committee');
    fixture.componentRef.setInput('journey', journey);
    fixture.componentRef.setInput('trackName', 'Digital Track');
    fixture.componentRef.setInput('activityName', 'Hackathon');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('IDEA-0001');
    expect(text).toContain('My Idea');
    expect(text).toContain('Digital Track');
    expect(text).toContain('Hackathon');
    expect(fixture.nativeElement.querySelector('app-idea-journey-timeline')).toBeTruthy();
  });
});
