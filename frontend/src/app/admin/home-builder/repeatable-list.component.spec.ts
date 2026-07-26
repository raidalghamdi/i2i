import { Component, TemplateRef, ViewChild } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RepeatableListComponent } from './repeatable-list.component';

@Component({
  selector: 'app-host',
  imports: [RepeatableListComponent],
  template: `
    <ng-template #row let-item let-i="index">
      <span class="row-text">{{ item }}-{{ i }}</span>
    </ng-template>
    <app-repeatable-list [label]="'Items'" [items]="items" [rowTemplate]="row" (add)="onAdd()" (remove)="onRemove($event)" />
  `,
})
class HostComponent {
  items = ['a', 'b'];
  addCalls = 0;
  removedIndex: number | null = null;
  onAdd(): void {
    this.addCalls++;
  }
  onRemove(index: number): void {
    this.removedIndex = index;
  }
}

describe('RepeatableListComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HostComponent] });
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('renders one row per item via the projected template', () => {
    const rows: HTMLElement[] = Array.from(fixture.nativeElement.querySelectorAll('.row-text'));
    expect(rows.map((r) => r.textContent)).toEqual(['a-0', 'b-1']);
  });

  it('emits add when the add button is clicked', () => {
    const addButton: HTMLButtonElement = fixture.nativeElement.querySelector('.repeatable-list-add');
    addButton.click();
    expect(fixture.componentInstance.addCalls).toBe(1);
  });

  it('emits remove with the row index when a remove button is clicked', () => {
    const removeButtons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('.repeatable-list-remove'));
    removeButtons[1].click();
    expect(fixture.componentInstance.removedIndex).toBe(1);
  });
});
