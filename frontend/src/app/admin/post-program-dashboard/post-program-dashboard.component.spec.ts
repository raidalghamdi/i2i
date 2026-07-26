import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PostProgramDashboardComponent } from './post-program-dashboard.component';

describe('PostProgramDashboardComponent', () => {
  let fixture: ComponentFixture<PostProgramDashboardComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostProgramDashboardComponent, HttpClientTestingModule],
    }).compileComponents();
    fixture = TestBed.createComponent(PostProgramDashboardComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/post-program/ideas').flush([
      { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Idea One', status: 'approved' },
    ]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('lists post-program ideas', () => {
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
    expect(fixture.componentInstance.ideas().length).toBe(1);
  });

  it('disables the Advance button until a comment is entered for that row', () => {
    const advanceButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Advance'),
    );
    expect(advanceButton).toBeTruthy();
    expect(advanceButton!.disabled).toBe(true);

    fixture.componentInstance.setComment('i1', 'Pilot kicked off successfully.');
    fixture.detectChanges();
    expect(advanceButton!.disabled).toBe(false);
  });

  it('does not call the API when onAdvance is invoked with a blank comment', async () => {
    await fixture.componentInstance.onAdvance('i1', 'in_pilot', '   ');
    httpMock.expectNone('/api/admin/ideas/i1/post-program-stage');
    expect(fixture.componentInstance.errorMessage()).toBeNull();
  });

  it('clicking Advance posts the row comment and refreshes the list', async () => {
    fixture.componentInstance.setComment('i1', 'Pilot kicked off successfully.');
    fixture.detectChanges();
    const advanceButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Advance'),
    )!;
    advanceButton.click();

    const post = httpMock.expectOne('/api/admin/ideas/i1/post-program-stage');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({ stage: 'in_pilot', comment: 'Pilot kicked off successfully.' });
    post.flush({ id: 'i1', status: 'in_pilot' });
    await new Promise((r) => setTimeout(r));
    // refresh re-fetches the list
    httpMock.expectOne('/api/admin/post-program/ideas').flush([
      { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Idea One', status: 'in_pilot' },
    ]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    expect(fixture.componentInstance.ideas()[0].status).toBe('in_pilot');
  });

  it('advances an idea and refreshes the list', async () => {
    fixture.componentInstance.onAdvance('i1', 'in_pilot', 'Pilot kicked off successfully.');
    const post = httpMock.expectOne('/api/admin/ideas/i1/post-program-stage');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({ stage: 'in_pilot', comment: 'Pilot kicked off successfully.' });
    post.flush({ id: 'i1', status: 'in_pilot' });
    await new Promise((r) => setTimeout(r));
    // refresh re-fetches the list
    httpMock.expectOne('/api/admin/post-program/ideas').flush([
      { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Idea One', status: 'in_pilot' },
    ]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();
    expect(fixture.componentInstance.ideas()[0].status).toBe('in_pilot');
  });

  it('toggles and lazily loads the transition history for an idea', async () => {
    const historyButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Show history'),
    )!;
    expect(historyButton).toBeTruthy();
    historyButton.click();
    fixture.detectChanges();

    httpMock.expectOne('/api/admin/ideas/i1/post-program-history').flush([
      { fromStage: 'approved', toStage: 'in_pilot', comment: 'Kicked off.', changedAt: '2026-02-01T00:00:00Z', changedById: 'user-1' },
    ]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('approved');
    expect(fixture.nativeElement.textContent).toContain('in_pilot');
    expect(fixture.nativeElement.textContent).toContain('Kicked off.');

    // Toggling again hides it without re-fetching.
    const hideButton = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>).find(
      (b) => b.textContent?.includes('Hide history'),
    )!;
    hideButton.click();
    fixture.detectChanges();
    httpMock.expectNone('/api/admin/ideas/i1/post-program-history');
  });
});

describe('PostProgramDashboardComponent (load states)', () => {
  let fixture: ComponentFixture<PostProgramDashboardComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PostProgramDashboardComponent, HttpClientTestingModule],
    }).compileComponents();
    fixture = TestBed.createComponent(PostProgramDashboardComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/post-program/ideas').flush(
      { error: 'Ideas unavailable' },
      { status: 500, statusText: 'Server Error' },
    );
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Ideas unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    retryButton.click();
    httpMock.expectOne('/api/admin/post-program/ideas').flush([
      { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Idea One', status: 'approved' },
    ]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('IDEA-0001');
  });

  it('shows an empty state when there are no ideas', async () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/admin/post-program/ideas').flush([]);
    await new Promise((r) => setTimeout(r));
    fixture.detectChanges();

    expect(fixture.componentInstance.ideas().length).toBe(0);
    expect(fixture.nativeElement.querySelector('app-empty-state')).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No approved or in-program ideas.');
  });
});
