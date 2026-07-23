import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { AnalyticsApiService } from '../../admin/analytics-api.service';
import { PillarDetail } from '../../admin/analytics.model';
import { PillarDetailComponent } from './pillar-detail.component';

const sampleDetail: PillarDetail = {
  themeId: 'theme-1',
  nameAr: 'الابتكار الرقمي',
  nameEn: 'Digital Innovation',
  descriptionAr: 'وصف بالعربية',
  descriptionEn: 'English description',
  ownerName: 'Jane Owner',
  kpis: {
    ideas: 34,
    budgetSpent: 120000,
    budgetAllocated: 200000,
    pilotsActive: 4,
    implementationsDone: 6,
  },
  timeline: [
    { month: '2026-01-01', count: 3 },
    { month: '2026-02-01', count: 5 },
  ],
  ideas: [
    { id: 'i-1', code: 'IDA-001', titleAr: 'فكرة أولى', titleEn: 'Idea One', status: 'approved', currentStage: 'pilot' },
  ],
};

function setup(getPillarResult: PillarDetail | null): {
  fixture: ComponentFixture<PillarDetailComponent>;
  api: { getPillar: jasmine.Spy };
} {
  const api = { getPillar: jasmine.createSpy('getPillar').and.returnValue(Promise.resolve(getPillarResult)) };
  TestBed.configureTestingModule({
    imports: [PillarDetailComponent],
    providers: [
      { provide: AnalyticsApiService, useValue: api },
      { provide: LOCALE_ID, useValue: 'en' },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'theme-1' } } } },
    ],
  });
  const fixture = TestBed.createComponent(PillarDetailComponent);
  return { fixture, api };
}

describe('PillarDetailComponent', () => {
  it('reads themeId from the route and renders the theme KPIs, timeline chart, and ideas table', async () => {
    const { fixture, api } = setup(sampleDetail);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.getPillar).toHaveBeenCalledWith('theme-1');
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Digital Innovation');
    expect(text).toContain('Jane Owner');
    expect(text).toContain('English description');
    expect(text).toContain('34');
    expect(text).toContain('120,000');
    expect(text).toContain('200,000');
    expect(text).toContain('4');
    expect(text).toContain('6');
    expect(fixture.nativeElement.querySelector('app-submissions-line-chart')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('app-status-badge')).toBeTruthy();
    expect(text).toContain('IDA-001');
    expect(text).toContain('Idea One');
  });

  it('shows a not-found state when the pillar does not exist', async () => {
    const { fixture } = setup(null);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Pillar not found');
    expect(fixture.nativeElement.querySelector('app-submissions-line-chart')).toBeFalsy();
  });

  it('shows a loading state before the pillar detail resolves', () => {
    const api = { getPillar: jasmine.createSpy('getPillar').and.returnValue(new Promise(() => {})) };
    TestBed.configureTestingModule({
      imports: [PillarDetailComponent],
      providers: [
        { provide: AnalyticsApiService, useValue: api },
        { provide: LOCALE_ID, useValue: 'en' },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'theme-1' } } } },
      ],
    });
    const fixture = TestBed.createComponent(PillarDetailComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('app-loading-state')).toBeTruthy();
  });
});
