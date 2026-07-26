import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AdminApiService } from './admin-api.service';

describe('AdminApiService', () => {
  let service: AdminApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AdminApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('listUsers() gets /api/admin/users', async () => {
    const promise = service.listUsers();
    const req = httpMock.expectOne('/api/admin/users');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith', department: 'Innovation', title: 'Analyst', isActive: true, roles: [] }]);

    expect(await promise).toEqual([{ id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith', department: 'Innovation', title: 'Analyst', isActive: true, roles: [] }]);
  });

  it('getUser() gets /api/admin/users/{id}', async () => {
    const promise = service.getUser('user-1');
    const req = httpMock.expectOne('/api/admin/users/user-1');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith', department: null, title: null, isActive: true, roles: [] });

    expect(await promise).toEqual({ id: 'user-1', samAccountName: 'jsmith', email: 'jsmith@gac-demo.sa', fullNameAr: 'ا', fullNameEn: 'J Smith', department: null, title: null, isActive: true, roles: [] });
  });

  it('grantRole() posts to /api/admin/role-grants', async () => {
    const promise = service.grantRole({ samAccountName: 'jsmith', roleCode: 'evaluator' });
    const req = httpMock.expectOne('/api/admin/role-grants');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ samAccountName: 'jsmith', roleCode: 'evaluator' });
    req.flush({ status: 'granted', userId: 'user-1', pendingGrantId: null });

    expect(await promise).toEqual({ status: 'granted', userId: 'user-1', pendingGrantId: null });
  });

  it('grantRoleToGroup() posts to /api/admin/role-grants/group', async () => {
    const promise = service.grantRoleToGroup({ groupName: 'GAC-Evaluators', roleCode: 'evaluator' });
    const req = httpMock.expectOne('/api/admin/role-grants/group');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ groupName: 'GAC-Evaluators', roleCode: 'evaluator' });
    req.flush({ grantedCount: 2, pendingCount: 1, skippedCount: 0, errors: [] });

    expect(await promise).toEqual({ grantedCount: 2, pendingCount: 1, skippedCount: 0, errors: [] });
  });

  it('revokeRole() deletes /api/admin/users/{userId}/roles/{roleId}', async () => {
    const promise = service.revokeRole('user-1', 'role-1');
    const req = httpMock.expectOne('/api/admin/users/user-1/roles/role-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    await promise;
  });

  it('setActive() posts to /api/admin/users/{id}/active', async () => {
    const promise = service.setActive('user-1', false);
    const req = httpMock.expectOne('/api/admin/users/user-1/active');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ isActive: false });
    req.flush(null);

    await promise;
  });

  it('listPendingGrants() gets /api/admin/pending-role-grants', async () => {
    const promise = service.listPendingGrants();
    const req = httpMock.expectOne('/api/admin/pending-role-grants');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'grant-1', samAccountName: 'futureuser1', roleCode: 'evaluator', roleNameEn: 'Evaluator', grantedByName: 'Admin One', grantedAt: '2026-01-01' }]);

    expect(await promise).toEqual([{ id: 'grant-1', samAccountName: 'futureuser1', roleCode: 'evaluator', roleNameEn: 'Evaluator', grantedByName: 'Admin One', grantedAt: '2026-01-01' }]);
  });

  it('cancelPendingGrant() deletes /api/admin/pending-role-grants/{id}', async () => {
    const promise = service.cancelPendingGrant('grant-1');
    const req = httpMock.expectOne('/api/admin/pending-role-grants/grant-1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    await promise;
  });

  it('listRoles() gets /api/roles', async () => {
    const promise = service.listRoles();
    const req = httpMock.expectOne('/api/roles');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'role-1', code: 'evaluator', nameEn: 'Evaluator' }]);

    expect(await promise).toEqual([{ id: 'role-1', code: 'evaluator', nameEn: 'Evaluator' }]);
  });

  it('getCurrentIdeaTemplate() gets /api/admin/idea-template', async () => {
    const promise = service.getCurrentIdeaTemplate();
    const req = httpMock.expectOne('/api/admin/idea-template');
    expect(req.request.method).toBe('GET');
    req.flush({ fileName: 'template.docx', contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', sizeBytes: 1058, uploadedAt: '2026-07-19T00:00:00Z' });

    expect(await promise).toEqual({ fileName: 'template.docx', contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', sizeBytes: 1058, uploadedAt: '2026-07-19T00:00:00Z' });
  });

  it('getCurrentIdeaTemplate() resolves null when none has been uploaded yet (404)', async () => {
    const promise = service.getCurrentIdeaTemplate();
    const req = httpMock.expectOne('/api/admin/idea-template');
    req.flush(null, { status: 404, statusText: 'Not Found' });

    expect(await promise).toBeNull();
  });

  it('uploadIdeaTemplate() posts multipart to /api/admin/idea-template', async () => {
    const file = new File(['x'], 'template.docx', { type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' });
    const promise = service.uploadIdeaTemplate(file);
    const req = httpMock.expectOne('/api/admin/idea-template');
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);
    req.flush({ fileName: 'template.docx', contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', sizeBytes: 4, uploadedAt: '2026-07-25T00:00:00Z' });

    expect(await promise).toEqual({ fileName: 'template.docx', contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', sizeBytes: 4, uploadedAt: '2026-07-25T00:00:00Z' });
  });

  it('importFromAd() posts to /api/admin/directory/import and maps per-SAM results', async () => {
    const promise = service.importFromAd(['lhassan'], 'evaluator');
    const req = httpMock.expectOne('/api/admin/directory/import');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ samAccountNames: ['lhassan'], roleCode: 'evaluator' });
    req.flush([{ samAccountName: 'lhassan', status: 'GrantedImmediately' }]);

    expect(await promise).toEqual([{ samAccountName: 'lhassan', status: 'GrantedImmediately' }]);
  });
});
