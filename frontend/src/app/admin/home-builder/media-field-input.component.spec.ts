import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MediaFieldInputComponent } from './media-field-input.component';
import { HomeBuilderApiService } from './home-builder-api.service';

describe('MediaFieldInputComponent', () => {
  let fixture: ComponentFixture<MediaFieldInputComponent>;
  let api: jasmine.SpyObj<HomeBuilderApiService>;

  function setup(kind: 'image' | 'video' = 'image', value = ''): void {
    api = jasmine.createSpyObj('HomeBuilderApiService', ['uploadMedia']);

    TestBed.configureTestingModule({
      imports: [MediaFieldInputComponent],
      providers: [{ provide: HomeBuilderApiService, useValue: api }],
    });
    fixture = TestBed.createComponent(MediaFieldInputComponent);
    fixture.componentRef.setInput('kind', kind);
    fixture.componentRef.setInput('value', value);
    fixture.detectChanges();
  }

  it('uploads a selected file and emits the returned url', async () => {
    setup('image');
    api.uploadMedia.and.returnValue(Promise.resolve({ id: 'm1', url: '/api/public/home/media/m1' }));
    const emitted: string[] = [];
    fixture.componentInstance.valueChange.subscribe((v) => emitted.push(v));

    const file = new File(['x'], 'photo.png', { type: 'image/png' });
    await fixture.componentInstance.onFileSelected(file);

    expect(api.uploadMedia).toHaveBeenCalledWith(file);
    expect(emitted).toEqual(['/api/public/home/media/m1']);
  });

  it('shows uploading state while the request is in flight', async () => {
    setup('image');
    let resolveUpload!: (v: { id: string; url: string }) => void;
    api.uploadMedia.and.returnValue(new Promise((resolve) => (resolveUpload = resolve)));

    const file = new File(['x'], 'photo.png', { type: 'image/png' });
    const promise = fixture.componentInstance.onFileSelected(file);

    expect(fixture.componentInstance.uploading()).toBe(true);

    resolveUpload({ id: 'm1', url: '/api/public/home/media/m1' });
    await promise;

    expect(fixture.componentInstance.uploading()).toBe(false);
  });

  it('surfaces an error message when the upload fails, without emitting a value', async () => {
    setup('image');
    api.uploadMedia.and.returnValue(Promise.reject({ error: { error: 'Bad file type' } }));
    const emitted: string[] = [];
    fixture.componentInstance.valueChange.subscribe((v) => emitted.push(v));

    const file = new File(['x'], 'bad.exe');
    await fixture.componentInstance.onFileSelected(file);

    expect(fixture.componentInstance.errorMessage()).toBe('Bad file type');
    expect(emitted).toEqual([]);
  });

  it('falls back to a generic error message when the server gives no error envelope', async () => {
    setup('image');
    api.uploadMedia.and.returnValue(Promise.reject(new Error('network down')));

    const file = new File(['x'], 'bad.exe');
    await fixture.componentInstance.onFileSelected(file);

    expect(fixture.componentInstance.errorMessage()).toBeTruthy();
  });

  it('emits an empty string when removed', () => {
    setup('image', '/api/public/home/media/m1');
    const emitted: string[] = [];
    fixture.componentInstance.valueChange.subscribe((v) => emitted.push(v));

    fixture.componentInstance.remove();

    expect(emitted).toEqual(['']);
  });

  it('renders an img preview for image kind', () => {
    setup('image', '/api/public/home/media/m1');
    expect(fixture.nativeElement.querySelector('img')).not.toBeNull();
  });

  it('renders a video preview for video kind', () => {
    setup('video', '/api/public/home/media/m1');
    expect(fixture.nativeElement.querySelector('video')).not.toBeNull();
  });

  it('renders no preview when the value is empty', () => {
    setup('image', '');
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
  });
});
