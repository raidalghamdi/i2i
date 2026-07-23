import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SupportApiService } from './support-api.service';

describe('SupportApiService', () => {
  let service: SupportApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(SupportApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('POSTs the support request to /api/public/support', async () => {
    const input = {
      name: 'Jane Doe',
      email: 'jane@example.com',
      subject: 'Question about submission',
      message: 'Hello, I have a question.',
    };

    const promise = service.submit(input);

    const req = httpMock.expectOne('/api/public/support');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(input);
    req.flush({ ok: true });

    expect(await promise).toEqual({ ok: true });
  });
});
