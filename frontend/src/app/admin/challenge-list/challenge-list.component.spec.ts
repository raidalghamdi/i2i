import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ChallengeApiService } from '../challenge-api.service';
import { Challenge } from '../challenge.model';
import { ChallengeListComponent } from './challenge-list.component';

describe('ChallengeListComponent', () => {
  let fixture: ComponentFixture<ChallengeListComponent>;
  let challengeApi: jasmine.SpyObj<ChallengeApiService>;

  function setup(challenges: Challenge[]): void {
    challengeApi = jasmine.createSpyObj('ChallengeApiService', ['list', 'delete']);
    challengeApi.list.and.returnValue(Promise.resolve(challenges));

    TestBed.configureTestingModule({
      imports: [ChallengeListComponent],
      providers: [provideRouter([]), { provide: ChallengeApiService, useValue: challengeApi }],
    });
    fixture = TestBed.createComponent(ChallengeListComponent);
  }

  it('renders one row per challenge', async () => {
    setup([{ id: 'c1', strategicThemeId: 't1', textAr: 'أ', textEn: 'Sample challenge', sortOrder: 0, isActive: true }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Sample challenge');
  });

  it('shows an empty-state message when there are no challenges', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No challenges');
  });

  it('deletes a challenge and refreshes the list', async () => {
    setup([{ id: 'c1', strategicThemeId: 't1', textAr: 'أ', textEn: 'To delete', sortOrder: 0, isActive: true }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    challengeApi.delete.and.returnValue(Promise.resolve());
    challengeApi.list.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onDelete('c1');

    expect(challengeApi.delete).toHaveBeenCalledWith('c1');
    expect(fixture.componentInstance.challenges().length).toBe(0);
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    challengeApi = jasmine.createSpyObj('ChallengeApiService', ['list', 'delete']);
    challengeApi.list.and.returnValue(Promise.reject({ error: { error: 'Challenges unavailable' } }));

    TestBed.configureTestingModule({
      imports: [ChallengeListComponent],
      providers: [provideRouter([]), { provide: ChallengeApiService, useValue: challengeApi }],
    });
    fixture = TestBed.createComponent(ChallengeListComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Challenges unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    challengeApi.list.and.returnValue(Promise.resolve([{ id: 'c1', strategicThemeId: 't1', textAr: 'أ', textEn: 'Recovered', sortOrder: 0, isActive: true }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Recovered');
  });
});
