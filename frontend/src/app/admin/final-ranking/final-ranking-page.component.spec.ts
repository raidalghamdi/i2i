import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { FinalRankingPageComponent } from './final-ranking-page.component';

describe('FinalRankingPageComponent', () => {
  let fixture: ComponentFixture<FinalRankingPageComponent>;

  it('renders the page header and the ranking panel', () => {
    const supervisorApi = jasmine.createSpyObj('SupervisorApiService', ['previewFinalRanking', 'runFinalRanking']);
    TestBed.configureTestingModule({
      imports: [FinalRankingPageComponent],
      providers: [{ provide: SupervisorApiService, useValue: supervisorApi }],
    });
    fixture = TestBed.createComponent(FinalRankingPageComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-final-ranking-panel')).toBeTruthy();
  });
});
