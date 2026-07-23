import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { MyIdeasComponent } from './my-ideas.component';
import { IdeasApiService } from '../ideas-api.service';
import { MyIdeaItem } from '../idea.model';

describe('MyIdeasComponent', () => {
  let fixture: ComponentFixture<MyIdeasComponent>;
  let ideasApi: jasmine.SpyObj<IdeasApiService>;

  const items: MyIdeaItem[] = [
    {
      id: 'i1',
      code: 'IDEA-001',
      titleAr: 'عنوان واحد',
      titleEn: 'Solar Rooftop Panels',
      status: 'submitted',
      currentStage: 1,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-05T00:00:00Z',
      feedbackCount: 0,
    },
    {
      id: 'i2',
      code: 'IDEA-002',
      titleAr: 'عنوان اثنين',
      titleEn: 'Water Recycling',
      status: 'in_pilot',
      currentStage: 7,
      createdAt: '2026-01-02T00:00:00Z',
      updatedAt: '2026-01-06T00:00:00Z',
      feedbackCount: 3,
    },
    {
      id: 'i3',
      code: 'IDEA-003',
      titleAr: 'عنوان ثلاثة',
      titleEn: 'Paperless Onboarding',
      status: 'returned',
      currentStage: 2,
      createdAt: '2026-01-03T00:00:00Z',
      updatedAt: '2026-01-07T00:00:00Z',
      feedbackCount: 1,
    },
  ];

  async function setup(queryParams: Record<string, string> = {}): Promise<void> {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getMineDetailed', 'withdraw']);
    ideasApi.getMineDetailed.and.returnValue(Promise.resolve(items));
    ideasApi.withdraw.and.returnValue(Promise.resolve());

    await TestBed.configureTestingModule({
      imports: [MyIdeasComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(queryParams) },
            queryParamMap: of(convertToParamMap(queryParams)),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MyIdeasComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders a card for each idea returned from the API', async () => {
    await setup();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('IDEA-001');
    expect(text).toContain('Solar Rooftop Panels');
    expect(text).toContain('IDEA-002');
    expect(text).toContain('IDEA-003');
  });

  it('shows the Pioneer badge only for the idea at stage 6+', async () => {
    await setup();
    const cards = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('app-pioneer-badge'));
    const withPioneerText = cards.filter((c) => c.textContent?.includes('Pioneer'));
    expect(withPioneerText.length).toBe(1);
  });

  it('shows the feedback count badge only for ideas with feedbackCount > 0', async () => {
    await setup();
    const badges = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('app-feedback-count-badge'));
    const withText = badges.filter((b) => (b.textContent ?? '').trim().length > 0);
    expect(withText.length).toBe(2);
  });

  it('reloads with the mapped statusGroup when a chip is clicked', async () => {
    await setup();
    ideasApi.getMineDetailed.calls.reset();

    const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('[role="tablist"] button'));
    const returnedChip = buttons.find((b) => b.textContent?.includes('Returned')) as HTMLButtonElement;
    returnedChip.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(ideasApi.getMineDetailed).toHaveBeenCalledWith('returned');
  });

  it('seeds statusGroup from the legacy status query param', async () => {
    await setup({ status: 'in_review' });
    expect(ideasApi.getMineDetailed).toHaveBeenCalledWith('in_review');
  });

  it('calls withdraw and reloads when the withdraw button is clicked', async () => {
    await setup();
    ideasApi.getMineDetailed.calls.reset();

    const withdrawButtons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).filter(
      (b) => b.textContent?.includes('Withdraw'),
    );
    expect(withdrawButtons.length).toBeGreaterThan(0);
    (withdrawButtons[0] as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(ideasApi.withdraw).toHaveBeenCalled();
    expect(ideasApi.getMineDetailed).toHaveBeenCalled();
  });

  it('shows a finalize CTA link for ideas awaiting attachments', async () => {
    const finalizeItem: MyIdeaItem = {
      id: 'i4',
      code: 'IDEA-004',
      titleAr: 'عنوان أربعة',
      titleEn: 'Finalize Me',
      status: 'pass_awaiting_attachments',
      currentStage: 5,
      createdAt: '2026-01-04T00:00:00Z',
      updatedAt: '2026-01-08T00:00:00Z',
      feedbackCount: 0,
    };

    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getMineDetailed', 'withdraw']);
    ideasApi.getMineDetailed.and.returnValue(Promise.resolve([finalizeItem]));

    await TestBed.configureTestingModule({
      imports: [MyIdeasComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({}) },
            queryParamMap: of(convertToParamMap({})),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MyIdeasComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const links = Array.from(el.querySelectorAll('a')).filter((a) =>
      a.textContent?.includes('Finalize for committee'),
    );
    expect(links.length).toBe(1);
    expect(links[0].getAttribute('href')).toContain('/ideas/i4');
  });

  it('shows an error alert when withdraw fails and does not reload', async () => {
    await setup();
    ideasApi.withdraw.and.returnValue(Promise.reject(new Error('network error')));
    ideasApi.getMineDetailed.calls.reset();

    const withdrawButtons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button')).filter(
      (b) => b.textContent?.includes('Withdraw'),
    );
    expect(withdrawButtons.length).toBeGreaterThan(0);
    (withdrawButtons[0] as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const alert = el.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    expect(alert?.textContent).toContain('Could not withdraw the idea');
    expect(ideasApi.getMineDetailed).not.toHaveBeenCalled();
  });

  it('shows an empty-state message with a link to /ideas/new when there are no ideas', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getMineDetailed', 'withdraw']);
    ideasApi.getMineDetailed.and.returnValue(Promise.resolve([]));

    await TestBed.configureTestingModule({
      imports: [MyIdeasComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({}) },
            queryParamMap: of(convertToParamMap({})),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MyIdeasComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const hrefs = Array.from(el.querySelectorAll('a')).map((a) => a.getAttribute('href'));
    expect(hrefs.some((h) => h?.includes('/ideas/new'))).toBeTrue();
  });

  it('renders the error state and retries the fetch when the "Try again" button is clicked', async () => {
    ideasApi = jasmine.createSpyObj('IdeasApiService', ['getMineDetailed', 'withdraw']);
    ideasApi.getMineDetailed.and.returnValue(Promise.reject(new Error('network error')));

    await TestBed.configureTestingModule({
      imports: [MyIdeasComponent],
      providers: [
        provideRouter([]),
        { provide: IdeasApiService, useValue: ideasApi },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap({}) },
            queryParamMap: of(convertToParamMap({})),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MyIdeasComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const retryButton = el.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).toBeTruthy();

    ideasApi.getMineDetailed.and.returnValue(Promise.resolve(items));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.error()).toBeNull();
    expect(el.textContent).toContain('IDEA-001');
  });
});
