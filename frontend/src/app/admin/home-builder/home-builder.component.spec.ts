import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { HomeBuilderComponent } from './home-builder.component';
import { HomeBuilderApiService } from './home-builder-api.service';
import { MediaFieldInputComponent } from './media-field-input.component';
import { AdminHomeSection } from './home-builder.model';

describe('HomeBuilderComponent', () => {
  let fixture: ComponentFixture<HomeBuilderComponent>;
  let api: jasmine.SpyObj<HomeBuilderApiService>;

  const sections: AdminHomeSection[] = [
    { id: 's1', idx: 0, type: 'hero', isVisible: true, contentJson: { eyebrow: { ar: 'أ', en: 'E' }, words: [], headline: { ar: '', en: '' }, subheadline: { ar: '', en: '' }, primaryCtaLabel: { ar: '', en: '' }, primaryCtaLink: '/ideas/new', secondaryCtaLabel: { ar: '', en: '' }, secondaryCtaLink: '#about', closedNotice: { ar: '', en: '' }, slogan: [] }, updatedAt: '2026-01-01T00:00:00Z', updatedById: null },
    { id: 's2', idx: 1, type: 'about', isVisible: true, contentJson: { title: { ar: 'عن', en: 'About' }, paragraphs: [{ ar: 'ف١', en: 'P1' }] }, updatedAt: '2026-01-01T00:00:00Z', updatedById: null },
  ];

  function setup(list: AdminHomeSection[] = sections): void {
    api = jasmine.createSpyObj('HomeBuilderApiService', ['listSections', 'saveSections', 'addSection', 'deleteSection']);
    api.listSections.and.returnValue(Promise.resolve(list));

    TestBed.configureTestingModule({
      imports: [HomeBuilderComponent],
      providers: [{ provide: HomeBuilderApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(HomeBuilderComponent);
  }

  async function init(): Promise<void> {
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();
  }

  it('loads sections and renders one card per section', async () => {
    setup();
    await init();

    expect(fixture.componentInstance.rows().length).toBe(2);
    const cards = fixture.nativeElement.querySelectorAll('section');
    expect(cards.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('hero');
    expect(fixture.nativeElement.textContent).toContain('about');
  });

  it('move-down swaps a section with the one after it', async () => {
    setup();
    await init();

    const firstKey = fixture.componentInstance.rows()[0].key;
    fixture.componentInstance.moveDown(firstKey);

    const types = fixture.componentInstance.rows().map((r) => r.type);
    expect(types).toEqual(['about', 'hero']);
  });

  it('move-up swaps a section with the one before it', async () => {
    setup();
    await init();

    const secondKey = fixture.componentInstance.rows()[1].key;
    fixture.componentInstance.moveUp(secondKey);

    const types = fixture.componentInstance.rows().map((r) => r.type);
    expect(types).toEqual(['about', 'hero']);
  });

  it('toggling visibility flips isVisible on the model', async () => {
    setup();
    await init();

    const key = fixture.componentInstance.rows()[0].key;
    expect(fixture.componentInstance.rows()[0].isVisible).toBe(true);

    fixture.componentInstance.toggleVisible(key);

    expect(fixture.componentInstance.rows()[0].isVisible).toBe(false);
  });

  it('editing a Loc field updates the section content', async () => {
    setup();
    await init();

    const key = fixture.componentInstance.rows()[1].key;
    fixture.componentInstance.setLocField(key, 'title', { ar: 'جديد', en: 'New' });

    expect(fixture.componentInstance.rows()[1].content['title']).toEqual({ ar: 'جديد', en: 'New' });
  });

  it('editing a Loc-array item updates that item only', async () => {
    setup();
    await init();

    const key = fixture.componentInstance.rows()[1].key;
    fixture.componentInstance.setLocArrayItem(key, 'paragraphs', 0, { ar: 'محدث', en: 'Updated' });

    expect(fixture.componentInstance.rows()[1].content['paragraphs']).toEqual([{ ar: 'محدث', en: 'Updated' }]);
  });

  it('Save serializes reordered/edited sections and PUTs via saveSections', async () => {
    setup();
    await init();

    const key = fixture.componentInstance.rows()[1].key;
    fixture.componentInstance.moveUp(key);
    fixture.componentInstance.setLocField(fixture.componentInstance.rows()[0].key, 'title', { ar: 'محدث', en: 'Updated' });

    api.saveSections.and.returnValue(Promise.resolve(sections));

    await fixture.componentInstance.save();

    expect(api.saveSections).toHaveBeenCalledTimes(1);
    const savedInputs = api.saveSections.calls.mostRecent().args[0];
    expect(savedInputs.length).toBe(2);
    expect(savedInputs[0].type).toBe('about');
    expect(savedInputs[0].idx).toBe(0);
    expect(savedInputs[1].type).toBe('hero');
    expect(savedInputs[1].idx).toBe(1);
    expect(typeof savedInputs[0].contentJson).toBe('string');
    expect(JSON.parse(savedInputs[0].contentJson).title).toEqual({ ar: 'محدث', en: 'Updated' });
  });

  it('Add appends a new-type card via addSection', async () => {
    setup();
    await init();

    const created: AdminHomeSection = { id: 's3', idx: 2, type: 'cta', isVisible: true, contentJson: { title: { ar: '', en: '' }, subtitle: { ar: '', en: '' }, buttonLabel: { ar: '', en: '' }, buttonLink: '' }, updatedAt: '2026-01-01T00:00:00Z', updatedById: null };
    api.addSection.and.returnValue(Promise.resolve(created));

    fixture.componentInstance.newSectionType.set('cta');
    await fixture.componentInstance.addSection();

    expect(api.addSection).toHaveBeenCalledTimes(1);
    const addedInput = api.addSection.calls.mostRecent().args[0];
    expect(addedInput.type).toBe('cta');
    expect(addedInput.id).toBeNull();
    expect(typeof addedInput.contentJson).toBe('string');

    expect(fixture.componentInstance.rows().length).toBe(3);
    expect(fixture.componentInstance.rows()[2].type).toBe('cta');
  });

  it('Delete removes a card and calls deleteSection for a persisted row', async () => {
    setup();
    await init();

    api.deleteSection.and.returnValue(Promise.resolve());
    const key = fixture.componentInstance.rows()[0].key;

    await fixture.componentInstance.deleteRow(key);

    expect(api.deleteSection).toHaveBeenCalledWith('s1');
    expect(fixture.componentInstance.rows().length).toBe(1);
    expect(fixture.componentInstance.rows()[0].type).toBe('about');
  });

  it('Delete removes a row with no server id yet without calling the API', async () => {
    setup();
    await init();

    // Simulate a row that hasn't been persisted (defensive path — in practice addSection()
    // always returns a real id, but deleteRow() must not call the API for a null id).
    fixture.componentInstance.rows.update((list) => [...list, { key: 'row-unsaved', id: null, type: 'cta', isVisible: true, content: {} }]);

    await fixture.componentInstance.deleteRow('row-unsaved');

    expect(api.deleteSection).not.toHaveBeenCalled();
    expect(fixture.componentInstance.rows().length).toBe(2);
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    api = jasmine.createSpyObj('HomeBuilderApiService', ['listSections', 'saveSections', 'addSection', 'deleteSection']);
    api.listSections.and.returnValue(Promise.reject({ error: { error: 'Sections unavailable' } }));

    TestBed.configureTestingModule({
      imports: [HomeBuilderComponent],
      providers: [{ provide: HomeBuilderApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(HomeBuilderComponent);
    await init();

    expect(fixture.componentInstance.loadError()).toBe('Sections unavailable');

    api.listSections.and.returnValue(Promise.resolve(sections));
    await fixture.componentInstance.reload();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.componentInstance.rows().length).toBe(2);
  });

  describe('media upload fields', () => {
    function mediaSections(): AdminHomeSection[] {
      return [
        {
          id: 'g1',
          idx: 0,
          type: 'about',
          isVisible: true,
          contentJson: { title: { ar: '', en: '' }, paragraphs: [], imageUrl: '/api/public/home/media/existing' },
          updatedAt: '2026-01-01T00:00:00Z',
          updatedById: null,
        },
        {
          id: 'g2',
          idx: 1,
          type: 'gallery',
          isVisible: true,
          contentJson: {
            title: { ar: '', en: '' },
            body: { ar: '', en: '' },
            galleryTitle: { ar: '', en: '' },
            items: [{ caption: { ar: 'ت', en: 'Caption' }, imageUrl: '' }],
            videoTitle: { ar: '', en: '' },
            videoHint: { ar: '', en: '' },
            videoUrl: '',
          },
          updatedAt: '2026-01-01T00:00:00Z',
          updatedById: null,
        },
      ];
    }

    function setupMedia(list: AdminHomeSection[] = mediaSections()): void {
      api = jasmine.createSpyObj('HomeBuilderApiService', [
        'listSections',
        'saveSections',
        'addSection',
        'deleteSection',
        'uploadMedia',
      ]);
      api.listSections.and.returnValue(Promise.resolve(list));

      TestBed.configureTestingModule({
        imports: [HomeBuilderComponent],
        providers: [{ provide: HomeBuilderApiService, useValue: api }],
      });
      fixture = TestBed.createComponent(HomeBuilderComponent);
    }

    function mediaInputs(): MediaFieldInputComponent[] {
      return fixture.debugElement
        .queryAll(By.directive(MediaFieldInputComponent))
        .map((de) => de.componentInstance as MediaFieldInputComponent);
    }

    it('uploading a file for the about image field sets imageUrl to the returned url', async () => {
      setupMedia();
      await init();
      fixture.componentInstance.toggleExpanded(fixture.componentInstance.rows()[0].key);
      fixture.detectChanges();

      api.uploadMedia.and.returnValue(Promise.resolve({ id: 'm1', url: '/api/public/home/media/m1' }));
      const [aboutImage] = mediaInputs();
      const file = new File(['x'], 'photo.png', { type: 'image/png' });
      await aboutImage.onFileSelected(file);
      fixture.detectChanges();

      expect(api.uploadMedia).toHaveBeenCalledWith(file);
      expect(fixture.componentInstance.rows()[0].content['imageUrl']).toBe('/api/public/home/media/m1');
    });

    it('uploading a file for the gallery video field sets videoUrl to the returned url', async () => {
      setupMedia();
      await init();
      fixture.componentInstance.toggleExpanded(fixture.componentInstance.rows()[1].key);
      fixture.detectChanges();

      api.uploadMedia.and.returnValue(Promise.resolve({ id: 'm2', url: '/api/public/home/media/m2' }));
      const videoInput = mediaInputs().find((c) => c.kind() === 'video')!;
      const file = new File(['x'], 'clip.mp4', { type: 'video/mp4' });
      await videoInput.onFileSelected(file);
      fixture.detectChanges();

      expect(fixture.componentInstance.rows()[1].content['videoUrl']).toBe('/api/public/home/media/m2');
    });

    it('uploading a photo for a gallery item sets only that item\'s imageUrl, leaving caption untouched', async () => {
      setupMedia();
      await init();
      fixture.componentInstance.toggleExpanded(fixture.componentInstance.rows()[1].key);
      fixture.detectChanges();

      api.uploadMedia.and.returnValue(Promise.resolve({ id: 'm3', url: '/api/public/home/media/m3' }));
      const itemImageInput = mediaInputs().find((c) => c.kind() === 'image')!;
      const file = new File(['x'], 'item.png', { type: 'image/png' });
      await itemImageInput.onFileSelected(file);
      fixture.detectChanges();

      const items = fixture.componentInstance.rows()[1].content['items'] as Array<{ caption: unknown; imageUrl: string }>;
      expect(items[0].imageUrl).toBe('/api/public/home/media/m3');
      expect(items[0].caption).toEqual({ ar: 'ت', en: 'Caption' });
    });

    it('a failed upload surfaces an error and leaves the field unchanged', async () => {
      setupMedia();
      await init();
      fixture.componentInstance.toggleExpanded(fixture.componentInstance.rows()[1].key);
      fixture.detectChanges();

      api.uploadMedia.and.returnValue(Promise.reject({ error: { error: 'Bad file type' } }));
      const videoInput = mediaInputs().find((c) => c.kind() === 'video')!;
      const file = new File(['x'], 'bad.exe');
      await videoInput.onFileSelected(file);
      fixture.detectChanges();

      expect(videoInput.errorMessage()).toBe('Bad file type');
      expect(fixture.componentInstance.rows()[1].content['videoUrl']).toBe('');
    });

    it('Remove clears the field to an empty string', async () => {
      setupMedia();
      await init();
      fixture.componentInstance.toggleExpanded(fixture.componentInstance.rows()[0].key);
      fixture.detectChanges();

      const [aboutImage] = mediaInputs();
      aboutImage.remove();
      fixture.detectChanges();

      expect(fixture.componentInstance.rows()[0].content['imageUrl']).toBe('');
    });
  });
});
