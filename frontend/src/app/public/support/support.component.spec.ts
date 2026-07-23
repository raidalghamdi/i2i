import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SupportComponent } from './support.component';
import { PublicContentApiService } from '../../core/public-content-api.service';

describe('SupportComponent', () => {
  let fixture: ComponentFixture<SupportComponent>;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    const cmsApi = jasmine.createSpyObj('PublicContentApiService', ['getBySlug']);
    cmsApi.getBySlug.and.returnValue(Promise.resolve(null));

    TestBed.configureTestingModule({
      imports: [SupportComponent, HttpClientTestingModule],
      providers: [{ provide: PublicContentApiService, useValue: cmsApi }],
    });

    fixture = TestBed.createComponent(SupportComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('renders the hero title and the contact details', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Support');
    expect(text).toContain('innovation@gac.gov.sa');
    expect(text).toContain('Sunday–Thursday, 9:00–16:00');
    expect(text).toContain('General Authority for Competition, Riyadh, Kingdom of Saudi Arabia');
  });

  it('does not submit when the form is invalid', () => {
    fixture.componentInstance.onSubmit();

    httpMock.expectNone('/api/public/support');
    expect(fixture.componentInstance.form.controls.email.touched).toBe(true);
  });

  it('submits the form and shows the confirmation message on success', async () => {
    fixture.componentInstance.form.setValue({
      name: 'Jane Doe',
      email: 'jane@example.com',
      subject: 'Question about submission',
      message: 'Hello, I have a question.',
    });

    fixture.componentInstance.onSubmit();

    const req = httpMock.expectOne('/api/public/support');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      name: 'Jane Doe',
      email: 'jane@example.com',
      subject: 'Question about submission',
      message: 'Hello, I have a question.',
    });
    req.flush({ ok: true });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.sent()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain(
      'Thank you — your message has been recorded. The team will reach out shortly.',
    );
    expect(fixture.componentInstance.form.value.name).toBeFalsy();
  });

  it('shows an error message when the submission fails', async () => {
    fixture.componentInstance.form.setValue({
      name: 'Jane Doe',
      email: 'jane@example.com',
      subject: 'Question about submission',
      message: 'Hello, I have a question.',
    });

    fixture.componentInstance.onSubmit();

    const req = httpMock.expectOne('/api/public/support');
    req.flush({ error: 'Message is required.' }, { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.sent()).toBe(false);
    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
  });
});
