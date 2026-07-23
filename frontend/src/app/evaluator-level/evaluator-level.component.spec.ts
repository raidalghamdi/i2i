import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MeApiService } from '../core/me-api.service';
import { EvaluatorLevelComponent } from './evaluator-level.component';

describe('EvaluatorLevelComponent', () => {
  let fixture: ComponentFixture<EvaluatorLevelComponent>;
  let meApi: jasmine.SpyObj<MeApiService>;

  function setup(points: number): void {
    meApi = jasmine.createSpyObj('MeApiService', ['get']);
    meApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 'e1', email: 'e1@x.com', fullNameAr: 'م', fullNameEn: 'E',
      department: null, title: null, points, level: 1, roles: ['evaluator'],
    }));

    TestBed.configureTestingModule({
      imports: [EvaluatorLevelComponent],
      providers: [{ provide: MeApiService, useValue: meApi }],
    });
    fixture = TestBed.createComponent(EvaluatorLevelComponent);
  }

  it('resolves the current level and the next level from points', async () => {
    setup(120);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.currentLevel().thresholdPoints).toBeLessThanOrEqual(120);
    expect(fixture.componentInstance.nextLevel()?.thresholdPoints).toBeGreaterThan(120);
  });

  it('computes points remaining to the next level', async () => {
    setup(120);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const expected = fixture.componentInstance.nextLevel()!.thresholdPoints - 120;
    expect(fixture.componentInstance.pointsToNext()).toBe(expected);
  });

  it('reports no next level when at the maximum threshold', async () => {
    setup(999999);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.nextLevel()).toBeNull();
    expect(fixture.componentInstance.pointsToNext()).toBe(0);
  });

  it('marks every level up to and including the current one as reached', async () => {
    setup(120);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const currentIndex = fixture.componentInstance.currentLevel().index;
    for (const level of fixture.componentInstance.levels()) {
      expect(fixture.componentInstance.isReached(level)).toBe(level.index <= currentIndex);
    }
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    const failingApi = jasmine.createSpyObj('MeApiService', ['get']);
    failingApi.get.and.returnValue(Promise.reject({ error: { error: 'Level unavailable' } }));

    TestBed.configureTestingModule({
      imports: [EvaluatorLevelComponent],
      providers: [{ provide: MeApiService, useValue: failingApi }],
    });
    const failFixture = TestBed.createComponent(EvaluatorLevelComponent);
    failFixture.detectChanges();
    await failFixture.whenStable();
    failFixture.detectChanges();

    expect(failFixture.componentInstance.loadError()).toBe('Level unavailable');
    const retryButton = failFixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    failingApi.get.and.returnValue(Promise.resolve({
      id: 'u1', samAccountName: 'e1', email: 'e1@x.com', fullNameAr: 'م', fullNameEn: 'E',
      department: null, title: null, points: 120, level: 1, roles: ['evaluator'],
    }));
    retryButton.click();
    await failFixture.whenStable();
    failFixture.detectChanges();

    expect(failFixture.componentInstance.loadError()).toBeNull();
    expect(failFixture.componentInstance.currentLevel().thresholdPoints).toBeLessThanOrEqual(120);
  });
});
