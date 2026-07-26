import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SectionMultiselectComponent } from './section-multiselect.component';

describe('SectionMultiselectComponent', () => {
  let fixture: ComponentFixture<SectionMultiselectComponent>;

  function setup(selectedSections: string[] = []): void {
    TestBed.configureTestingModule({ imports: [SectionMultiselectComponent] });
    fixture = TestBed.createComponent(SectionMultiselectComponent);
    fixture.componentRef.setInput('selectedSections', selectedSections);
    fixture.detectChanges();
  }

  it('renders all 8 section checkboxes', () => {
    setup();
    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    expect(checkboxes.length).toBe(10);
    const text = fixture.nativeElement.textContent as string;
    for (const label of ['العنوان', 'بيان المشكلة', 'الحل المقترح', 'الفوائد المتوقعة', 'النشاط', 'المحور الاستراتيجي', 'التحدي', 'نوع المشاركة', 'الفريق', 'المرفقات']) {
      expect(text).toContain(label);
    }
  });

  it('reflects the initial selection as checked', () => {
    setup(['team', 'attachments']);
    expect(fixture.componentInstance.isSelected('team')).toBe(true);
    expect(fixture.componentInstance.isSelected('attachments')).toBe(true);
    expect(fixture.componentInstance.isSelected('title')).toBe(false);
  });

  it('emits the updated selection when a checkbox is toggled', () => {
    setup();
    const onChange = jasmine.createSpy('selectionChange');
    fixture.componentInstance.selectionChange.subscribe(onChange);

    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]') as NodeListOf<HTMLInputElement>;
    checkboxes[0].dispatchEvent(new Event('change')); // 'title' is the first rendered checkbox
    expect(onChange).toHaveBeenCalledWith(['title']);
  });
});
