import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Idea } from '../../ideas/idea.model';
import { EvaluationReviewDetail } from '../../evaluations/evaluation-review.model';
import { IdeaContextPanelComponent } from './idea-context-panel.component';

describe('IdeaContextPanelComponent', () => {
  let fixture: ComponentFixture<IdeaContextPanelComponent>;

  const baseIdea: Idea = {
    id: 'idea-1', code: 'IDEA-0001', submitterId: 'owner-1', titleAr: 'عنوان الفكرة', titleEn: 'Idea Title',
    problemStatementAr: 'بيان المشكلة', problemStatementEn: 'Problem', proposedSolutionAr: 'الحل المقترح', proposedSolutionEn: 'Solution',
    expectedBenefitsAr: 'الفوائد المتوقعة', expectedBenefitsEn: 'Benefits', strategicThemeId: 'theme-1',
    activityId: 'activity-1', challengeId: null, participationType: 'team', teamName: 'Team Alpha',
    teamMembers: [{ samAccountName: 'ofarouk', name: 'Omar Farouk', email: 'ofarouk@gac-demo.sa' }],
    ipAcknowledged: true, termsAgreed: true,
    status: 'evaluation', currentStage: 2, createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01', approvedAt: null,
    attachments: [{ id: 'att-1', fileName: 'evidence.pdf', contentType: 'application/pdf', fileSizeBytes: 3, uploadedAt: '2026-01-01' }],
    screeningReason: null, editableSections: null,
  };

  const detail: EvaluationReviewDetail = {
    evaluations: [{ reviewerLabel: 'Reviewer 1', score: 8, comment: 'Solid proposal.' }],
    aggregateScore: 8,
    aggregateByCriteria: { innovation: 8 },
    supervisorComment: null,
  };

  function setup(): void {
    TestBed.configureTestingModule({ imports: [IdeaContextPanelComponent] });
    fixture = TestBed.createComponent(IdeaContextPanelComponent);
  }

  it('renders idea content, classification, and attachments', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'supervisor');
    fixture.componentRef.setInput('themeName', 'Digital Track');
    fixture.componentRef.setInput('activityName', 'Hackathon');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('IDEA-0001');
    expect(text).toContain('عنوان الفكرة');
    expect(text).toContain('بيان المشكلة');
    expect(text).toContain('الحل المقترح');
    expect(text).toContain('الفوائد المتوقعة');
    expect(text).toContain('Digital Track');
    expect(text).toContain('Hackathon');
    expect(text).toContain('evidence.pdf');
  });

  it('hides team identity for viewMode "evaluator"', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'evaluator');
    fixture.detectChanges();

    expect(fixture.componentInstance.showTeam()).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('Omar Farouk');
    expect(fixture.nativeElement.textContent).not.toContain('Team Alpha');
  });

  it('shows team identity for viewMode "supervisor"', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'supervisor');
    fixture.detectChanges();

    expect(fixture.componentInstance.showTeam()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Team Alpha');
    expect(fixture.nativeElement.textContent).toContain('Omar Farouk');
  });

  it('shows team identity for viewMode "judge" and "submitter" too', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'judge');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Team Alpha');
  });

  it('shows evaluator ratings only when evaluationDetail is provided and viewMode is supervisor or judge', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'supervisor');
    fixture.componentRef.setInput('evaluationDetail', detail);
    fixture.detectChanges();

    expect(fixture.componentInstance.showEvaluationDetail()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Reviewer 1');
    expect(fixture.nativeElement.textContent).toContain('Solid proposal.');
    expect(fixture.nativeElement.textContent).toContain('8.0');
  });

  it('never shows evaluator ratings for viewMode "evaluator" or "submitter", even if evaluationDetail is provided', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'evaluator');
    fixture.componentRef.setInput('evaluationDetail', detail);
    fixture.detectChanges();

    expect(fixture.componentInstance.showEvaluationDetail()).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('Solid proposal.');
  });

  it('does not show evaluator ratings when evaluationDetail is not provided, even for supervisor', () => {
    setup();
    fixture.componentRef.setInput('idea', baseIdea);
    fixture.componentRef.setInput('viewMode', 'supervisor');
    fixture.detectChanges();

    expect(fixture.componentInstance.showEvaluationDetail()).toBe(false);
    expect(fixture.nativeElement.querySelector('h3')?.textContent).not.toContain('Evaluator Ratings');
  });
});
