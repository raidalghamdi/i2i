import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { CommitteeApiService } from '../committee-api.service';
import { CommitteeDecisionFormComponent } from './committee-decision-form.component';

describe('CommitteeDecisionFormComponent', () => {
  let fixture: ComponentFixture<CommitteeDecisionFormComponent>;
  let committeeApi: jasmine.SpyObj<CommitteeApiService>;
  let router: jasmine.SpyObj<Router>;

  function setup(): void {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getCriteria', 'submitDecision']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    committeeApi.getCriteria.and.returnValue(Promise.resolve([
      { code: 'originality', nameAr: 'أ', nameEn: 'Originality', weight: 0.30 },
      { code: 'feasibility', nameAr: 'ب', nameEn: 'Feasibility', weight: 0.25 },
      { code: 'impact', nameAr: 'ج', nameEn: 'Impact', weight: 0.30 },
      { code: 'alignment', nameAr: 'د', nameEn: 'Alignment', weight: 0.15 },
    ]));

    TestBed.configureTestingModule({
      imports: [CommitteeDecisionFormComponent],
      providers: [
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(CommitteeDecisionFormComponent);
  }

  // Sets each named control individually via `.get(name)?.setValue(...)` rather than a single
  // `form.patchValue({...})` call. The dynamically-added criterion controls (originality, feasibility,
  // etc.) aren't part of the FormGroup's statically-inferred value type (only `decisionTypeCode`/`comments`
  // are, from the `fb.group({...})` call in the component) — passing an object literal mixing static and
  // dynamic keys directly to `patchValue` would fail TypeScript's excess-property check. `.get(name)?.setValue(...)`
  // has no such static shape constraint.
  function setFormValues(form: typeof fixture.componentInstance.form, values: Record<string, unknown>): void {
    for (const [key, value] of Object.entries(values)) {
      form.get(key)?.setValue(value);
    }
  }

  it('marks the form invalid until all criteria and a decision type are filled', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  it('submits all criteria scores, decision type, and comments, then navigates to the queue', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitDecision.and.returnValue(Promise.resolve({ id: 'decision-1', totalScore: 8, ideaStatus: 'committee' }));

    setFormValues(fixture.componentInstance.form, {
      originality: 8, feasibility: 8, impact: 8, alignment: 8,
      decisionTypeCode: 'approved', comments: 'Great idea.',
    });
    await fixture.componentInstance.onSubmit();

    expect(committeeApi.submitDecision).toHaveBeenCalledWith('idea-1', {
      decisionTypeCode: 'approved',
      criteriaScores: { originality: 8, feasibility: 8, impact: 8, alignment: 8 },
      comments: 'Great idea.',
    });
    expect(router.navigate).toHaveBeenCalledWith(['/committee/queue']);
  });

  it('shows an inline error message when submission fails', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    committeeApi.submitDecision.and.returnValue(Promise.reject({ error: { error: 'You have already decided on this idea.' } }));

    setFormValues(fixture.componentInstance.form, {
      originality: 8, feasibility: 8, impact: 8, alignment: 8,
      decisionTypeCode: 'approved', comments: null,
    });
    await fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('You have already decided on this idea.');
  });

  it('shows the error state and retries the fetch when "Try again" is clicked', async () => {
    committeeApi = jasmine.createSpyObj('CommitteeApiService', ['getCriteria', 'submitDecision']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    committeeApi.getCriteria.and.returnValue(Promise.reject(new Error('boom')));

    TestBed.configureTestingModule({
      imports: [CommitteeDecisionFormComponent],
      providers: [
        { provide: CommitteeApiService, useValue: committeeApi },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'idea-1' } } } },
      ],
    });
    fixture = TestBed.createComponent(CommitteeDecisionFormComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).not.toBeNull();
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    committeeApi.getCriteria.and.returnValue(Promise.resolve([
      { code: 'originality', nameAr: 'أ', nameEn: 'Originality', weight: 0.30 },
    ]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.criteria().length).toBe(1);
  });
});
