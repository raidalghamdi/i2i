import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { HomeBuilderApiService } from './home-builder-api.service';
import { AdminHomeSection, HomeSectionInput } from './home-builder.model';

describe('HomeBuilderApiService', () => {
  let service: HomeBuilderApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(HomeBuilderApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('listSections() gets /api/admin/home/sections and returns the sections with contentJson as an object', async () => {
    const promise = service.listSections();
    const req = httpMock.expectOne('/api/admin/home/sections');
    expect(req.request.method).toBe('GET');
    const sections: AdminHomeSection[] = [
      {
        id: 's1',
        idx: 0,
        type: 'hero',
        isVisible: true,
        contentJson: { eyebrow: { ar: 'أ', en: 'E' } },
        updatedAt: '2026-01-01T00:00:00Z',
        updatedById: null,
      },
    ];
    req.flush(sections);

    expect(await promise).toEqual(sections);
  });

  it('saveSections() PUTs { sections } with each contentJson stringified', async () => {
    const inputs: HomeSectionInput[] = [
      { id: 's1', idx: 0, type: 'hero', isVisible: true, contentJson: JSON.stringify({ eyebrow: { ar: 'أ', en: 'E' } }) },
      { id: null, idx: 1, type: 'cta', isVisible: false, contentJson: JSON.stringify({ title: { ar: '', en: '' } }) },
    ];
    const promise = service.saveSections(inputs);
    const req = httpMock.expectOne('/api/admin/home/sections');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ sections: inputs });
    expect(typeof req.request.body.sections[0].contentJson).toBe('string');

    const returned: AdminHomeSection[] = [
      { id: 's1', idx: 0, type: 'hero', isVisible: true, contentJson: { eyebrow: { ar: 'أ', en: 'E' } }, updatedAt: '2026-01-01T00:00:00Z', updatedById: 'u1' },
      { id: 's2', idx: 1, type: 'cta', isVisible: false, contentJson: { title: { ar: '', en: '' } }, updatedAt: '2026-01-01T00:00:00Z', updatedById: 'u1' },
    ];
    req.flush(returned);

    expect(await promise).toEqual(returned);
  });

  it('addSection() POSTs one HomeSectionInput with contentJson as a string', async () => {
    const input: HomeSectionInput = { id: null, idx: 2, type: 'faq', isVisible: true, contentJson: JSON.stringify({ title: { ar: '', en: '' }, items: [] }) };
    const promise = service.addSection(input);
    const req = httpMock.expectOne('/api/admin/home/sections');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);

    const created: AdminHomeSection = { id: 's3', idx: 2, type: 'faq', isVisible: true, contentJson: { title: { ar: '', en: '' }, items: [] }, updatedAt: '2026-01-01T00:00:00Z', updatedById: 'u1' };
    req.flush(created);

    expect(await promise).toEqual(created);
  });

  it('deleteSection() DELETEs /api/admin/home/sections/{id}', async () => {
    const promise = service.deleteSection('s1');
    const req = httpMock.expectOne('/api/admin/home/sections/s1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });

    await promise;
    expect().nothing();
  });
});
