import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { IdentityService } from '../core/auth/identity.service';
import { PublicTracksApiService } from '../core/public-tracks-api.service';
import { HomeContentApiService, HomeSection } from '../core/home-content-api.service';
import { LandingComponent } from './landing.component';

/** Representative seeded sections, in the seeded order (idx 0..11). Every text value below
 * is deliberately distinct from the real seed copy so a test can only pass if the value came
 * from this mock's contentJson — not from a residual hardcoded literal in the component. */
function mockSections(): HomeSection[] {
  return [
    {
      idx: 0,
      type: 'hero',
      isVisible: true,
      contentJson: {
        eyebrow: { ar: 'شعار تجريبي', en: 'Mock Eyebrow Copy' },
        words: [
          { ar: 'ابدأ', en: 'MockWordStart' },
          { ar: 'نافس', en: 'MockWordCompete' },
        ],
        headline: { ar: '', en: '' },
        subheadline: { ar: 'نص فرعي تجريبي', en: 'Mock subheadline copy' },
        primaryCtaLabel: { ar: 'قدم', en: 'Mock Submit CTA' },
        primaryCtaLink: '/ideas/new',
        secondaryCtaLabel: { ar: 'تعرف', en: 'Mock Learn CTA' },
        secondaryCtaLink: '#about',
        closedNotice: { ar: 'مغلق', en: 'Mock closed notice' },
        slogan: [
          { ar: 'ابتكر', en: 'MockInnovate' },
          { ar: 'نافس', en: 'MockCompete' },
          { ar: 'أثر', en: 'MockImpact' },
        ],
      },
    },
    {
      idx: 1,
      type: 'about',
      isVisible: true,
      contentJson: {
        title: { ar: 'عن', en: 'Mock About Title' },
        paragraphs: [
          { ar: 'فقرة عن البرنامج', en: 'Mock about body paragraph' },
          { ar: 'مهمتنا', en: 'Mock mission paragraph' },
        ],
      },
    },
    {
      idx: 2,
      type: 'objectives',
      isVisible: true,
      contentJson: {
        title: { ar: 'الأهداف', en: 'Mock Objectives Title' },
        items: [
          { ar: 'هدف واحد', en: 'Mock objective one' },
          { ar: 'هدف اثنان', en: 'Mock objective two' },
        ],
      },
    },
    {
      idx: 3,
      type: 'tracks',
      isVisible: true,
      contentJson: { title: { ar: 'المسارات', en: 'Mock Tracks Title' }, intro: { ar: '', en: '' } },
    },
    {
      idx: 4,
      type: 'details',
      isVisible: true,
      contentJson: {
        title: { ar: 'التفاصيل', en: 'Mock Details Title' },
        rulesTitle: { ar: 'القواعد', en: 'Mock Rules Title' },
        rules: [{ ar: 'قاعدة', en: 'Mock rule one' }],
        formatTitle: { ar: 'الصيغة', en: 'Mock Format Title' },
        format: { ar: 'صيغة', en: 'Mock format body' },
        eligibilityTitle: { ar: 'الأهلية', en: 'Mock Eligibility Title' },
        eligibility: { ar: 'أهلية', en: 'Mock eligibility body' },
      },
    },
    {
      idx: 5,
      type: 'timeline',
      isVisible: true,
      contentJson: {
        title: { ar: 'الجدول الزمني', en: 'Mock Timeline Title' },
        stages: [
          {
            id: 'mock-stage',
            title: { ar: 'مرحلة', en: 'Mock Stage Title' },
            date: { ar: 'تاريخ', en: 'Mock Stage Date' },
            description: { ar: 'وصف', en: 'Mock stage description' },
            tone: 'cyan',
          },
        ],
      },
    },
    {
      idx: 6,
      type: 'criteria',
      isVisible: true,
      contentJson: {
        title: { ar: 'المعايير', en: 'Mock Criteria Title' },
        eyebrow: { ar: 'كيف', en: 'Mock Criteria Eyebrow' },
        lead: { ar: 'مقدمة', en: 'Mock criteria lead' },
        items: [
          { label: { ar: 'ابتكار', en: 'Mock Criterion Label' }, description: { ar: 'وصف', en: 'Mock criterion description' }, weight: 42, color: '#123456', icon: 'sparkles' },
        ],
      },
    },
    {
      idx: 7,
      type: 'prizes',
      isVisible: true,
      contentJson: {
        title: { ar: 'الجوائز', en: 'Mock Prizes Title' },
        items: [{ tier: { ar: 'الأول', en: 'Mock Prize Tier' }, value: { ar: 'قيمة', en: 'Mock Prize Value' } }],
      },
    },
    {
      idx: 8,
      type: 'gallery',
      isVisible: true,
      contentJson: {
        title: { ar: 'النسخة السابقة', en: 'Mock Gallery Title' },
        body: { ar: 'جسم', en: 'Mock gallery body' },
        galleryTitle: { ar: 'من الصور', en: 'Mock Gallery Subtitle' },
        items: [{ ar: 'صورة', en: 'Mock Gallery Caption' }],
        videoTitle: { ar: 'فيديو', en: 'Mock Video Title' },
        videoHint: { ar: 'قريبا', en: 'Mock video hint' },
      },
    },
    {
      idx: 9,
      type: 'partners',
      isVisible: true,
      contentJson: {
        title: { ar: 'الشركاء', en: 'Mock Partners Title' },
        items: [{ ar: 'شريك', en: 'Mock Partner Name' }],
      },
    },
    {
      idx: 10,
      type: 'faq',
      isVisible: true,
      contentJson: {
        title: { ar: 'الأسئلة', en: 'Mock FAQ Title' },
        items: [{ q: { ar: 'سؤال', en: 'Mock FAQ Question' }, a: { ar: 'جواب', en: 'Mock FAQ Answer' } }],
      },
    },
    {
      idx: 11,
      type: 'cta',
      isVisible: true,
      contentJson: {
        title: { ar: 'جاهز', en: 'Mock CTA Title' },
        subtitle: { ar: 'فرعي', en: 'Mock CTA Subtitle' },
        buttonLabel: { ar: 'قدم', en: 'Mock CTA Button' },
        buttonLink: '/ideas/new',
      },
    },
  ];
}

describe('LandingComponent', () => {
  let fixture: ComponentFixture<LandingComponent>;
  let publicTracksApi: jasmine.SpyObj<PublicTracksApiService>;
  let homeContentApi: jasmine.SpyObj<HomeContentApiService>;

  async function setup(activeRole: string | null, sections: HomeSection[] | 'reject' = mockSections()): Promise<void> {
    publicTracksApi = jasmine.createSpyObj('PublicTracksApiService', ['list']);
    publicTracksApi.list.and.returnValue(
      Promise.resolve([{ id: 'theme-1', nameAr: 'ا', nameEn: 'Theme One', descriptionAr: '', descriptionEn: '', priority: 1 }]),
    );

    homeContentApi = jasmine.createSpyObj('HomeContentApiService', ['getHome']);
    if (sections === 'reject') {
      homeContentApi.getHome.and.returnValue(Promise.reject(new Error('network down')));
    } else {
      homeContentApi.getHome.and.returnValue(Promise.resolve(sections));
    }

    TestBed.configureTestingModule({
      imports: [LandingComponent],
      providers: [
        provideRouter([]),
        { provide: PublicTracksApiService, useValue: publicTracksApi },
        { provide: HomeContentApiService, useValue: homeContentApi },
        {
          provide: IdentityService,
          useValue: {
            identity: signal({
              samAccountName: 'submitter1',
              email: null,
              department: null,
              roles: activeRole ? [activeRole] : [],
              activeRole,
            }),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(LandingComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('shows the "Submit your idea" CTA for a user with an assigned role', async () => {
    await setup('submitter');
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/ideas/new')).toBe(true);
  });

  it('hides the "Submit your idea" CTA when there is no active role', async () => {
    await setup(null);
    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    expect(links.some((a) => a.getAttribute('href') === '/ideas/new')).toBe(false);
  });

  it('does not render the "Platform at a glance" stats section', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Platform at a glance');
    expect(text).not.toContain('المنصة في لمحة');
    expect(text).not.toContain('Total ideas');
    expect(text).not.toContain('Approved');
    expect(text).not.toContain('Evaluators');
  });

  it('fetches and renders the public tracks regardless of role', async () => {
    await setup(null);
    expect(publicTracksApi.list).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Theme One');
  });

  it('renders the hero eyebrow and rotating words from the fetched home content', async () => {
    await setup('submitter');
    expect(homeContentApi.getHome).toHaveBeenCalled();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock Eyebrow Copy');
    expect(text).toContain('MockWordStart');
  });

  it('renders the about paragraphs from the fetched home content', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock About Title');
    expect(text).toContain('Mock about body paragraph');
    expect(text).toContain('Mock mission paragraph');
  });

  it('renders the objectives items from the fetched home content', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock Objectives Title');
    expect(text).toContain('Mock objective one');
    expect(text).toContain('Mock objective two');
  });

  it('renders the criteria labels and weights from the fetched home content', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock Criteria Title');
    expect(text).toContain('Mock Criterion Label');
    expect(text).toContain('42%');
  });

  it('renders the prize tiers and values from the fetched home content', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock Prizes Title');
    expect(text).toContain('Mock Prize Tier');
    expect(text).toContain('Mock Prize Value');
  });

  it('renders the FAQ questions from the fetched home content', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock FAQ Title');
    expect(text).toContain('Mock FAQ Question');
  });

  it('renders the final CTA button from the fetched home content', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock CTA Title');
    expect(text).toContain('Mock CTA Button');
  });

  it('renders sections in the order returned by the API', async () => {
    await setup('submitter');
    const text = fixture.nativeElement.textContent as string;
    expect(text.indexOf('Mock Eyebrow Copy')).toBeLessThan(text.indexOf('Mock About Title'));
    expect(text.indexOf('Mock About Title')).toBeLessThan(text.indexOf('Mock Objectives Title'));
    expect(text.indexOf('Mock Objectives Title')).toBeLessThan(text.indexOf('Mock Criteria Title'));
    expect(text.indexOf('Mock Criteria Title')).toBeLessThan(text.indexOf('Mock Prizes Title'));
    expect(text.indexOf('Mock Prizes Title')).toBeLessThan(text.indexOf('Mock FAQ Title'));
    expect(text.indexOf('Mock FAQ Title')).toBeLessThan(text.indexOf('Mock CTA Title'));
  });

  it('renders an admin-added richtext section (title + sanitized HTML body)', async () => {
    const sections = mockSections();
    sections.push({
      idx: 12,
      type: 'richtext',
      isVisible: true,
      contentJson: {
        title: { ar: 'قسم', en: 'Mock Richtext Title' },
        html: { ar: '<p>عربي</p>', en: '<p>Mock <strong>richtext</strong> body</p>' },
      },
    });
    await setup('submitter', sections);
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Mock Richtext Title');
    // innerHTML content renders (strong tag preserved, text present)
    expect(text).toContain('Mock');
    expect(text).toContain('richtext');
    expect(text).toContain('body');
    expect(fixture.nativeElement.querySelector('strong')).not.toBeNull();
  });

  it('renders nothing (no error) for an unknown/future section type', async () => {
    const sections = mockSections();
    sections.push({
      idx: 13,
      type: 'some-future-type',
      isVisible: true,
      contentJson: { title: { ar: 'مجهول', en: 'Unknown Section Title' } },
    });
    await setup('submitter', sections);
    const text = fixture.nativeElement.textContent as string;
    // unknown type degrades gracefully to nothing, and known sections still render
    expect(text).not.toContain('Unknown Section Title');
    expect(text).toContain('Mock CTA Title');
  });

  it('renders no dynamic sections when the home content fetch fails, without throwing', async () => {
    await setup('submitter', 'reject');
    const text = fixture.nativeElement.textContent as string;
    expect(text).not.toContain('Mock Eyebrow Copy');
    expect(text).not.toContain('Mock CTA Title');
    // The landing's own persistent chrome (sticky CTA) still renders on the failure path.
    expect(fixture.nativeElement.querySelector('app-sticky-cta')).not.toBeNull();
  });

  it('does NOT render its own footer (the public shell provides the single site footer)', async () => {
    await setup('submitter');
    // Guards against the duplicate-footer regression: the footer belongs to the public shell.
    expect(fixture.nativeElement.querySelector('app-site-footer')).toBeNull();
    expect(fixture.nativeElement.querySelector('footer')).toBeNull();
  });

  describe('uploaded media rendering', () => {
    it('renders an <img> for the about section when imageUrl is present', async () => {
      const sections = mockSections();
      const aboutSection = sections.find((s) => s.type === 'about')!;
      aboutSection.contentJson.imageUrl = '/api/public/home/media/about-1';
      await setup('submitter', sections);

      const aboutSectionEl = fixture.nativeElement.querySelector('#about');
      const img = aboutSectionEl.querySelector('img');
      expect(img).not.toBeNull();
      expect(img.getAttribute('src')).toBe('/api/public/home/media/about-1');
      expect(aboutSectionEl.querySelector('app-icon')).toBeNull();
    });

    it('renders the placeholder icon for the about section when imageUrl is absent', async () => {
      await setup('submitter');
      const aboutSectionEl = fixture.nativeElement.querySelector('#about');
      expect(aboutSectionEl.querySelector('img')).toBeNull();
      expect(aboutSectionEl.querySelector('app-icon')).not.toBeNull();
    });

    it('renders an <img> per gallery item when imageUrl is present', async () => {
      const sections = mockSections();
      const gallerySection = sections.find((s) => s.type === 'gallery')!;
      gallerySection.contentJson.items = [
        { caption: { ar: 'صورة أولى', en: 'Mock First Photo' }, imageUrl: '/api/public/home/media/gallery-1' },
        { caption: { ar: 'صورة ثانية', en: 'Mock Second Photo' }, imageUrl: '' },
      ];
      await setup('submitter', sections);

      const previousSectionEl = fixture.nativeElement.querySelector('#previous');
      const imgs = previousSectionEl.querySelectorAll('figure img');
      expect(imgs.length).toBe(1);
      expect(imgs[0].getAttribute('src')).toBe('/api/public/home/media/gallery-1');
      expect(previousSectionEl.textContent).toContain('Mock First Photo');
      expect(previousSectionEl.textContent).toContain('Mock Second Photo');
    });

    it('renders a <video> for the gallery section when videoUrl is present', async () => {
      const sections = mockSections();
      const gallerySection = sections.find((s) => s.type === 'gallery')!;
      gallerySection.contentJson.videoUrl = '/api/public/home/media/gallery-video';
      await setup('submitter', sections);

      const previousSectionEl = fixture.nativeElement.querySelector('#previous');
      const video = previousSectionEl.querySelector('video');
      expect(video).not.toBeNull();
      expect(video.getAttribute('src')).toBe('/api/public/home/media/gallery-video');
      expect(previousSectionEl.textContent).not.toContain('Mock video hint');
    });

    it('renders the video placeholder for the gallery section when videoUrl is absent', async () => {
      await setup('submitter');
      const previousSectionEl = fixture.nativeElement.querySelector('#previous');
      expect(previousSectionEl.querySelector('video')).toBeNull();
      expect(previousSectionEl.textContent).toContain('Mock video hint');
    });

    it('renders old-shape gallery items (plain Loc, no caption/imageUrl) without throwing', async () => {
      const sections = mockSections();
      const gallerySection = sections.find((s) => s.type === 'gallery')!;
      // Pre-existing seeded rows may still store items as plain Loc pairs, not {caption, imageUrl}.
      gallerySection.contentJson.items = [{ ar: 'صورة قديمة', en: 'Mock Old Shape Caption' }];

      await expectAsync(setup('submitter', sections)).toBeResolved();

      const previousSectionEl = fixture.nativeElement.querySelector('#previous');
      expect(previousSectionEl.textContent).toContain('Mock Old Shape Caption');
      expect(previousSectionEl.querySelector('figure img')).toBeNull();
      expect(previousSectionEl.querySelector('figure app-icon')).not.toBeNull();
    });
  });
});
