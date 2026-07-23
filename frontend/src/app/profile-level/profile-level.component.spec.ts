import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MeApiService } from '../core/me-api.service';
import { ProfileLevelComponent } from './profile-level.component';

describe('ProfileLevelComponent', () => {
  let fixture: ComponentFixture<ProfileLevelComponent>;
  let meApi: jasmine.SpyObj<MeApiService>;

  function setup(): void {
    meApi = jasmine.createSpyObj('MeApiService', ['get', 'getBadges']);
    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 's1', email: 's1@x.com', fullNameAr: 'أ', fullNameEn: 'A',
      department: null, title: null, points: 40, level: 2, roles: ['submitter'],
    }));
    meApi.getBadges.and.returnValue(Promise.resolve({
      badges: [
        { code: 'first-idea', nameAr: 'أ', nameEn: 'First Idea', descriptionAr: null, descriptionEn: null, iconUrl: null, earnedAt: '2026-06-01T00:00:00Z' },
        { code: 'prolific', nameAr: 'غ', nameEn: 'Prolific', descriptionAr: null, descriptionEn: null, iconUrl: null, earnedAt: null },
      ],
    }));

    TestBed.configureTestingModule({
      imports: [ProfileLevelComponent],
      providers: [{ provide: MeApiService, useValue: meApi }],
    });
    fixture = TestBed.createComponent(ProfileLevelComponent);
  }

  it('loads points, level, and badges', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.points()).toBe(40);
    expect(fixture.componentInstance.level()).toBe(2);
    expect(fixture.componentInstance.badges().length).toBe(2);
  });

  it('distinguishes earned from locked badges', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.earnedBadges().length).toBe(1);
    expect(fixture.componentInstance.earnedBadges()[0].code).toBe('first-idea');
    expect(fixture.componentInstance.lockedBadges().length).toBe(1);
    expect(fixture.componentInstance.lockedBadges()[0].code).toBe('prolific');
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    const failingApi = jasmine.createSpyObj('MeApiService', ['get', 'getBadges']);
    failingApi.get.and.returnValue(Promise.reject({ error: { error: 'Level unavailable' } }));
    failingApi.getBadges.and.returnValue(Promise.resolve({ badges: [] }));

    TestBed.configureTestingModule({
      imports: [ProfileLevelComponent],
      providers: [{ provide: MeApiService, useValue: failingApi }],
    });
    const failFixture = TestBed.createComponent(ProfileLevelComponent);
    failFixture.detectChanges();
    await failFixture.componentInstance.ngOnInit();
    failFixture.detectChanges();

    expect(failFixture.componentInstance.loadError()).toBe('Level unavailable');
    const retryButton = failFixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    failingApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 's1', email: 's1@x.com', fullNameAr: 'أ', fullNameEn: 'A',
      department: null, title: null, points: 40, level: 2, roles: ['submitter'],
    }));
    retryButton.click();
    await failFixture.whenStable();
    failFixture.detectChanges();

    expect(failFixture.componentInstance.loadError()).toBeNull();
    expect(failFixture.componentInstance.points()).toBe(40);
  });
});
