import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { IdeasApiService } from '../ideas/ideas-api.service';
import { MeApiService } from '../core/me-api.service';
import { NotificationsApiService } from '../core/notifications-api.service';
import { IdeaSummary } from '../ideas/idea.model';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;
  let meApi: jasmine.SpyObj<MeApiService>;
  let notificationsApi: jasmine.SpyObj<NotificationsApiService>;

  const ideas: IdeaSummary[] = [
    { id: 'i1', code: 'IDEA-0001', titleAr: 'ا', titleEn: 'Alpha', status: 'submitted', updatedAt: '2026-07-01T00:00:00Z' },
    { id: 'i2', code: 'IDEA-0002', titleAr: 'ب', titleEn: 'Beta', status: 'approved', updatedAt: '2026-07-02T00:00:00Z' },
    { id: 'i3', code: 'IDEA-0003', titleAr: 'ج', titleEn: 'Gamma', status: 'pass_awaiting_attachments', updatedAt: '2026-07-03T00:00:00Z' },
  ];

  function setup(): void {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getMine']);
    meApi = jasmine.createSpyObj('MeApiService', ['get']);
    notificationsApi = jasmine.createSpyObj('NotificationsApiService', ['list']);

    ideasApi.getMine.and.returnValue(Promise.resolve(ideas));
    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 's1', email: 's1@x.com', fullNameAr: 'أ', fullNameEn: 'A',
      department: null, title: null, points: 40, level: 2, roles: ['submitter'],
    }));
    notificationsApi.list.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        { provide: MeApiService, useValue: meApi },
        { provide: NotificationsApiService, useValue: notificationsApi },
      ],
    });
    fixture = TestBed.createComponent(DashboardComponent);
  }

  it('computes KPI counts from idea statuses', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.totalCount()).toBe(3);
    expect(fixture.componentInstance.inReviewCount()).toBe(1);
    expect(fixture.componentInstance.acceptedCount()).toBe(1);
    expect(fixture.componentInstance.level()).toBe(2);
  });

  it('shows the awaiting-finalize banner for ideas in pass_awaiting_attachments', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.awaitingFinalize().length).toBe(1);
    expect(fixture.componentInstance.awaitingFinalize()[0].id).toBe('i3');
  });

  it('shows the 3 most recent ideas as latest activity', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.latest().length).toBe(3);
    expect(fixture.componentInstance.latest()[0].id).toBe('i3');
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    setup();
    ideasApi.getMine.and.returnValue(Promise.reject(new Error('boom')));
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).not.toBeNull();
    const retryButton = (fixture.nativeElement as HTMLElement).querySelector('button');
    expect(retryButton).toBeTruthy();

    ideasApi.getMine.and.returnValue(Promise.resolve(ideas));
    retryButton!.dispatchEvent(new Event('click'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.componentInstance.totalCount()).toBe(3);
  });
});
