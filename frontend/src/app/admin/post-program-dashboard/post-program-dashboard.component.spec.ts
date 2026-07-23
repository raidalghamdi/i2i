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

  it('advances an idea and refreshes the list', async () => {
    fixture.componentInstance.onAdvance('i1', 'in_pilot');
    const post = httpMock.expectOne('/api/admin/ideas/i1/post-program-stage');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toEqual({ stage: 'in_pilot' });
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
