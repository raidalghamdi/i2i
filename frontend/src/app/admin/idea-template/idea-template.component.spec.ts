import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AdminApiService } from '../admin-api.service';
import { IdeaTemplateInfo } from '../admin.model';
import { IdeaTemplateComponent } from './idea-template.component';

describe('IdeaTemplateComponent', () => {
  let fixture: ComponentFixture<IdeaTemplateComponent>;
  let api: jasmine.SpyObj<AdminApiService>;

  const currentTemplate: IdeaTemplateInfo = {
    fileName: 'idea-description-template.docx',
    contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    sizeBytes: 1058,
    uploadedAt: '2026-07-19T00:00:00Z',
  };

  function setup(current: IdeaTemplateInfo | null = currentTemplate): void {
    api = jasmine.createSpyObj('AdminApiService', ['getCurrentIdeaTemplate', 'uploadIdeaTemplate']);
    api.getCurrentIdeaTemplate.and.returnValue(Promise.resolve(current));

    TestBed.configureTestingModule({
      imports: [IdeaTemplateComponent],
      providers: [{ provide: AdminApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(IdeaTemplateComponent);
  }

  it('loads and renders the current template info', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(api.getCurrentIdeaTemplate).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('idea-description-template.docx');
  });

  it('shows an empty state when no template has been uploaded yet', async () => {
    setup(null);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.current()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No template uploaded yet');
  });

  it('uploads a chosen file and shows the updated current template', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    const uploaded: IdeaTemplateInfo = {
      fileName: 'new-template.docx',
      contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      sizeBytes: 2048,
      uploadedAt: '2026-07-25T00:00:00Z',
    };
    api.uploadIdeaTemplate.and.returnValue(Promise.resolve(uploaded));

    const file = new File(['x'], 'new-template.docx', {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    });
    await fixture.componentInstance.onFileSelected(file);
    fixture.detectChanges();

    expect(api.uploadIdeaTemplate).toHaveBeenCalledWith(file);
    expect(fixture.componentInstance.current()).toEqual(uploaded);
    expect(fixture.componentInstance.successMessage()).toBeTruthy();
  });

  it('shows an error message when the upload fails', async () => {
    setup();
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();

    api.uploadIdeaTemplate.and.returnValue(Promise.reject({ error: { error: 'Upload failed' } }));

    const file = new File(['x'], 'bad.docx');
    await fixture.componentInstance.onFileSelected(file);
    fixture.detectChanges();

    expect(fixture.componentInstance.errorMessage()).toBe('Upload failed');
  });
});
