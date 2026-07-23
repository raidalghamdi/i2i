import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CmsApiService } from '../cms-api.service';
import { CmsBlock } from '../cms.model';
import { CmsBlockListComponent } from './cms-block-list.component';

describe('CmsBlockListComponent', () => {
  let fixture: ComponentFixture<CmsBlockListComponent>;
  let cmsApi: jasmine.SpyObj<CmsApiService>;

  function setup(blocks: CmsBlock[]): void {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['listBlocks', 'deleteBlock']);
    cmsApi.listBlocks.and.returnValue(Promise.resolve(blocks));

    TestBed.configureTestingModule({
      imports: [CmsBlockListComponent],
      providers: [provideRouter([]), { provide: CmsApiService, useValue: cmsApi }],
    });
    fixture = TestBed.createComponent(CmsBlockListComponent);
  }

  it('renders one row per block', async () => {
    setup([{ id: 'b1', key: 'welcome-banner', contentAr: 'أ', contentEn: 'Welcome', isPublished: true, updatedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('welcome-banner');
  });

  it('shows an empty-state message when there are no blocks', async () => {
    setup([]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No content blocks');
  });

  it('deletes a block and refreshes the list', async () => {
    setup([{ id: 'b1', key: 'to-delete', contentAr: 'أ', contentEn: 'D', isPublished: true, updatedAt: '2026-01-01' }]);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    cmsApi.deleteBlock.and.returnValue(Promise.resolve());
    cmsApi.listBlocks.and.returnValue(Promise.resolve([]));

    await fixture.componentInstance.onDelete('b1');

    expect(cmsApi.deleteBlock).toHaveBeenCalledWith('b1');
    expect(fixture.componentInstance.blocks().length).toBe(0);
  });

  it('shows an error state with retry when the load fails, and recovers on retry', async () => {
    cmsApi = jasmine.createSpyObj('CmsApiService', ['listBlocks', 'deleteBlock']);
    cmsApi.listBlocks.and.returnValue(Promise.reject({ error: { error: 'Blocks unavailable' } }));

    TestBed.configureTestingModule({
      imports: [CmsBlockListComponent],
      providers: [provideRouter([]), { provide: CmsApiService, useValue: cmsApi }],
    });
    fixture = TestBed.createComponent(CmsBlockListComponent);
    fixture.detectChanges();
    await fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBe('Blocks unavailable');
    const retryButton = fixture.nativeElement.querySelector('app-error-state button') as HTMLButtonElement;
    expect(retryButton).not.toBeNull();

    cmsApi.listBlocks.and.returnValue(Promise.resolve([{ id: 'b1', key: 'recovered', contentAr: 'أ', contentEn: 'Recovered', isPublished: true, updatedAt: '2026-01-01' }]));
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('recovered');
  });
});
