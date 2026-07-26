import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LocPairInputComponent } from './loc-pair-input.component';
import { Loc } from './home-builder.model';

describe('LocPairInputComponent', () => {
  let fixture: ComponentFixture<LocPairInputComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [LocPairInputComponent] });
    fixture = TestBed.createComponent(LocPairInputComponent);
    fixture.componentRef.setInput('value', { ar: 'أ', en: 'E' });
    fixture.detectChanges();
  });

  it('renders the AR and EN input values', () => {
    const inputs: HTMLInputElement[] = Array.from(fixture.nativeElement.querySelectorAll('input, textarea'));
    expect(inputs.map((i) => i.value)).toEqual(['أ', 'E']);
  });

  it('emits an updated Loc when the AR field changes, keeping EN intact', () => {
    const emitted: Loc[] = [];
    fixture.componentInstance.valueChange.subscribe((v: Loc) => emitted.push(v));

    const arField: HTMLInputElement = fixture.nativeElement.querySelectorAll('input, textarea')[0];
    arField.value = 'جديد';
    arField.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(emitted).toEqual([{ ar: 'جديد', en: 'E' }]);
  });

  it('emits an updated Loc when the EN field changes, keeping AR intact', () => {
    const emitted: Loc[] = [];
    fixture.componentInstance.valueChange.subscribe((v: Loc) => emitted.push(v));

    const enField: HTMLInputElement = fixture.nativeElement.querySelectorAll('input, textarea')[1];
    enField.value = 'New';
    enField.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(emitted).toEqual([{ ar: 'أ', en: 'New' }]);
  });
});
