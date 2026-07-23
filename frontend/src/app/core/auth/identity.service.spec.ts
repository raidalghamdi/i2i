import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { IdentityService } from './identity.service';

describe('IdentityService', () => {
  let service: IdentityService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(IdentityService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('populates identity signal on successful load, defaulting activeRole to the first role', async () => {
    const loadPromise = service.load();
    httpMock.expectOne('/api/identity/me').flush({
      samAccountName: 'submitter1',
      email: 'submitter1@gac-demo.sa',
      department: 'Innovation',
      roles: ['submitter', 'evaluator'],
    });
    await loadPromise;

    expect(service.identity()).toEqual({
      samAccountName: 'submitter1',
      email: 'submitter1@gac-demo.sa',
      department: 'Innovation',
      roles: ['submitter', 'evaluator'],
      activeRole: 'submitter',
    });
    expect(service.loadFailed()).toBe(false);
  });

  it('restores activeRole from localStorage when the stored value is still an assigned role', async () => {
    localStorage.setItem('activeRole', 'evaluator');
    const loadPromise = service.load();
    httpMock.expectOne('/api/identity/me').flush({
      samAccountName: 'submitter1',
      email: null,
      department: null,
      roles: ['submitter', 'evaluator'],
    });
    await loadPromise;

    expect(service.identity()?.activeRole).toBe('evaluator');
  });

  it('falls back to the first role when the stored activeRole is no longer assigned', async () => {
    localStorage.setItem('activeRole', 'admin');
    const loadPromise = service.load();
    httpMock.expectOne('/api/identity/me').flush({
      samAccountName: 'submitter1',
      email: null,
      department: null,
      roles: ['submitter', 'evaluator'],
    });
    await loadPromise;

    expect(service.identity()?.activeRole).toBe('submitter');
  });

  it('sets activeRole to null when the user has zero assigned roles', async () => {
    const loadPromise = service.load();
    httpMock.expectOne('/api/identity/me').flush({
      samAccountName: 'newuser1',
      email: null,
      department: null,
      roles: [],
    });
    await loadPromise;

    expect(service.identity()?.activeRole).toBeNull();
  });

  it('sets loadFailed and leaves identity null when the request fails', async () => {
    const loadPromise = service.load();
    httpMock.expectOne('/api/identity/me').flush('Service Unavailable', { status: 503, statusText: 'Service Unavailable' });
    await loadPromise;

    expect(service.identity()).toBeNull();
    expect(service.loadFailed()).toBe(true);
  });

  it('setActiveRole updates the signal and persists to localStorage only for an assigned role', async () => {
    const loadPromise = service.load();
    httpMock.expectOne('/api/identity/me').flush({
      samAccountName: 'submitter1',
      email: null,
      department: null,
      roles: ['submitter', 'evaluator'],
    });
    await loadPromise;

    service.setActiveRole('evaluator');
    expect(service.identity()?.activeRole).toBe('evaluator');
    expect(localStorage.getItem('activeRole')).toBe('evaluator');

    service.setActiveRole('admin');
    expect(service.identity()?.activeRole).toBe('evaluator');
    expect(localStorage.getItem('activeRole')).toBe('evaluator');
  });
});
