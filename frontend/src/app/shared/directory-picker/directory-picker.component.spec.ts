import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DirectoryPickerComponent } from './directory-picker.component';
import { DirectoryApiService, DirectoryPerson } from '../../core/directory-api.service';

/** This workspace is zoneless (no zone.js/testing), so `fakeAsync`/`tick`
 * aren't available here. The component's debounce is real RxJS `debounceTime`,
 * so tests wait out the real 300ms window instead of faking the clock. */
function wait(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

describe('DirectoryPickerComponent', () => {
  let fixture: ComponentFixture<DirectoryPickerComponent>;
  let component: DirectoryPickerComponent;
  let directoryApiSpy: jasmine.SpyObj<DirectoryApiService>;

  const jane: DirectoryPerson = { samAccountName: 'jdoe', displayName: 'Jane Doe', email: 'jdoe@example.com' };
  const john: DirectoryPerson = { samAccountName: 'jsmith', displayName: 'John Smith', email: 'jsmith@example.com' };

  function setup() {
    directoryApiSpy = jasmine.createSpyObj('DirectoryApiService', ['search']);
    TestBed.configureTestingModule({
      imports: [DirectoryPickerComponent],
      providers: [{ provide: DirectoryApiService, useValue: directoryApiSpy }],
    });
    fixture = TestBed.createComponent(DirectoryPickerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function setInputValue(value: string): void {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input[type="text"]');
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('debounces rapid keystrokes into a single search call with the last value', async () => {
    setup();
    directoryApiSpy.search.and.returnValue(of([jane]));

    // Three inputs well within the 300ms window: a correct debounce cancels the
    // first two and only the last ('lay') should ever reach the API. A naive
    // per-keystroke setTimeout with no cancellation would call search 3 times.
    setInputValue('l');
    setInputValue('la');
    setInputValue('lay');
    expect(directoryApiSpy.search).not.toHaveBeenCalled();

    await wait(200);
    expect(directoryApiSpy.search).not.toHaveBeenCalled();

    await wait(150);
    expect(directoryApiSpy.search).toHaveBeenCalledOnceWith('lay');
  });

  it('does not call search for a query shorter than 2 trimmed characters', async () => {
    setup();
    directoryApiSpy.search.and.returnValue(of([jane]));

    setInputValue('a');
    await wait(350);

    expect(directoryApiSpy.search).not.toHaveBeenCalled();
  });

  it('ignores queries that trim to less than 2 characters', async () => {
    setup();
    directoryApiSpy.search.and.returnValue(of([jane]));

    setInputValue('  a ');
    await wait(350);

    expect(directoryApiSpy.search).not.toHaveBeenCalled();
  });

  it('single mode: selecting a result emits selectionChange with that person', async () => {
    setup();
    directoryApiSpy.search.and.returnValue(of([jane, john]));
    const emitted: (DirectoryPerson | DirectoryPerson[])[] = [];
    component.selectionChange.subscribe((v) => emitted.push(v));

    setInputValue('doe');
    await wait(300);
    fixture.detectChanges();

    const resultButton: HTMLButtonElement | null = fixture.nativeElement.querySelector('[data-person-id="jdoe"]');
    expect(resultButton).not.toBeNull();
    resultButton!.click();
    fixture.detectChanges();

    expect(emitted).toEqual([jane]);
  });

  it('multiple mode: selecting two people emits an array and renders 2 chips; removing one updates the emission', async () => {
    fixture = TestBed.configureTestingModule({
      imports: [DirectoryPickerComponent],
      providers: [{ provide: DirectoryApiService, useValue: (directoryApiSpy = jasmine.createSpyObj('DirectoryApiService', ['search'])) }],
    }).createComponent(DirectoryPickerComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('multiple', true);
    fixture.detectChanges();

    directoryApiSpy.search.and.returnValue(of([jane, john]));
    const emitted: (DirectoryPerson | DirectoryPerson[])[] = [];
    component.selectionChange.subscribe((v) => emitted.push(v));

    setInputValue('doe');
    await wait(300);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-person-id="jdoe"]').click();
    fixture.detectChanges();

    directoryApiSpy.search.and.returnValue(of([jane, john]));
    setInputValue('smith');
    await wait(300);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-person-id="jsmith"]').click();
    fixture.detectChanges();

    expect(emitted[emitted.length - 1]).toEqual([jane, john]);

    const chips = fixture.nativeElement.querySelectorAll('[data-chip-id]');
    expect(chips.length).toBe(2);

    const removeButton: HTMLButtonElement = fixture.nativeElement.querySelector('[data-chip-remove="jdoe"]');
    removeButton.click();
    fixture.detectChanges();

    expect(emitted[emitted.length - 1]).toEqual([john]);
    expect(fixture.nativeElement.querySelectorAll('[data-chip-id]').length).toBe(1);
  });

  it('multiple mode: clearSelection() removes chips and excludes prior people from the next emission', async () => {
    fixture = TestBed.configureTestingModule({
      imports: [DirectoryPickerComponent],
      providers: [{ provide: DirectoryApiService, useValue: (directoryApiSpy = jasmine.createSpyObj('DirectoryApiService', ['search'])) }],
    }).createComponent(DirectoryPickerComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('multiple', true);
    fixture.detectChanges();

    directoryApiSpy.search.and.returnValue(of([jane, john]));
    const emitted: (DirectoryPerson | DirectoryPerson[])[] = [];
    component.selectionChange.subscribe((v) => emitted.push(v));

    setInputValue('doe');
    await wait(300);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-person-id="jdoe"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('[data-chip-id]').length).toBe(1);

    component.clearSelection();
    fixture.detectChanges();

    // Chips gone and internal selection reset.
    expect(fixture.nativeElement.querySelectorAll('[data-chip-id]').length).toBe(0);
    expect(component.selected()).toEqual([]);

    // A subsequent pick must emit ONLY the new person, not the previously-cleared one.
    directoryApiSpy.search.and.returnValue(of([jane, john]));
    setInputValue('smith');
    await wait(300);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-person-id="jsmith"]').click();
    fixture.detectChanges();

    expect(emitted[emitted.length - 1]).toEqual([john]);
  });
});
