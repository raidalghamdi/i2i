import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RosterApiService } from '../roster-api.service';
import { RosterHubRow } from '../roster.model';
import { RosterHubComponent } from './roster-hub.component';

describe('RosterHubComponent', () => {
  let fixture: ComponentFixture<RosterHubComponent>;
  let api: jasmine.SpyObj<RosterApiService>;

  const rows: RosterHubRow[] = [
    {
      roleCode: 'evaluator',
      roleNameAr: 'مقيّم',
      roleNameEn: 'Evaluator',
      activeCount: 5,
      pendingCount: 2,
      expiredCount: 1,
      withdrawnCount: 3,
    },
    {
      roleCode: 'supervisor',
      roleNameAr: 'مشرف',
      roleNameEn: 'Supervisor',
      activeCount: 4,
      pendingCount: 0,
      expiredCount: 0,
      withdrawnCount: 1,
    },
  ];

  function setup(): void {
    api = jasmine.createSpyObj('RosterApiService', ['getHub']);
    api.getHub.and.returnValue(Promise.resolve(rows));

    TestBed.configureTestingModule({
      imports: [RosterHubComponent],
      providers: [provideRouter([]), { provide: RosterApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(RosterHubComponent);
  }

  it('loads and renders one card per role with the active/pending/expired/withdrawn counts', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Evaluator');
    expect(text).toContain('Supervisor');
    expect(text).toContain('5');
    expect(text).toContain('2');
    expect(text).toContain('1');
    expect(text).toContain('3');
    expect(text).toContain('4');
  });

  it('links each card to its per-role detail page', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/admin/roster/evaluator')).toBe(true);
    expect(links.some((a) => a.getAttribute('href') === '/admin/roster/supervisor')).toBe(true);
  });

  it('shows an error state with retry when the hub fails to load, and recovers on retry', async () => {
    api = jasmine.createSpyObj('RosterApiService', ['getHub']);
    api.getHub.and.returnValue(Promise.reject({ error: { error: 'Roster hub unavailable' } }));

    TestBed.configureTestingModule({
      imports: [RosterHubComponent],
      providers: [provideRouter([]), { provide: RosterApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(RosterHubComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBe('Roster hub unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    api.getHub.and.returnValue(Promise.resolve(rows));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Evaluator');
  });

  it('shows nothing but does not error when there are no roles', async () => {
    api = jasmine.createSpyObj('RosterApiService', ['getHub']);
    api.getHub.and.returnValue(Promise.resolve([]));

    TestBed.configureTestingModule({
      imports: [RosterHubComponent],
      providers: [provideRouter([]), { provide: RosterApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(RosterHubComponent);

    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('a').length).toBe(0);
  });
});
