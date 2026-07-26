import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CmsDashboardComponent } from './cms-dashboard.component';

describe('CmsDashboardComponent', () => {
  let fixture: ComponentFixture<CmsDashboardComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CmsDashboardComponent],
      providers: [provideRouter([])],
    });
    fixture = TestBed.createComponent(CmsDashboardComponent);
  });

  it('renders links to all three content-type list screens', () => {
    fixture.detectChanges();

    const links = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    const hrefs = links.map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/admin/cms/blocks');
    expect(hrefs).toContain('/admin/cms/content');
    expect(hrefs).toContain('/admin/cms/strings');
  });
});
