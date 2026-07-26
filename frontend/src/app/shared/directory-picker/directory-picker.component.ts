import { Component, EventEmitter, OnDestroy, OnInit, Output, inject, input, signal } from '@angular/core';
import { EMPTY, Subject, Subscription } from 'rxjs';
import { debounceTime, switchMap } from 'rxjs/operators';
import { DirectoryApiService, DirectoryPerson } from '../../core/directory-api.service';
import { IconComponent } from '../icon/icon.component';

/** Reusable AD-directory person picker: a debounced search box backed by
 * `DirectoryApiService.search`, rendering matches as a results dropdown.
 * In single-select mode (default) picking a result immediately emits it.
 * In `multiple` mode picks accumulate as removable chips and each change
 * (add or remove) emits the full current selection array. */
@Component({
  selector: 'app-directory-picker',
  imports: [IconComponent],
  templateUrl: './directory-picker.component.html',
})
export class DirectoryPickerComponent implements OnInit, OnDestroy {
  private readonly directoryApi = inject(DirectoryApiService);

  readonly multiple = input(false);
  readonly placeholder = input<string>();
  /** Pre-selected people to seed the picker with (e.g. an existing team roster when editing). */
  readonly initialSelection = input<DirectoryPerson[]>([]);

  @Output() readonly selectionChange = new EventEmitter<DirectoryPerson | DirectoryPerson[]>();

  protected readonly defaultPlaceholder = $localize`:@@directoryPickerPlaceholder:Search by name or email…`;

  readonly query = signal('');
  readonly results = signal<DirectoryPerson[]>([]);
  readonly loading = signal(false);
  readonly selected = signal<DirectoryPerson[]>([]);

  private readonly queryInput$ = new Subject<string>();
  private readonly searchSub: Subscription;

  constructor() {
    this.searchSub = this.queryInput$
      .pipe(
        debounceTime(300),
        switchMap((q) => {
          if (q.trim().length < 2) {
            this.loading.set(false);
            return EMPTY;
          }
          this.loading.set(true);
          return this.directoryApi.search(q.trim());
        }),
      )
      .subscribe((people) => {
        this.results.set(people);
        this.loading.set(false);
      });
  }

  ngOnInit(): void {
    const initial = this.initialSelection();
    if (initial.length > 0) {
      this.selected.set([...initial]);
    }
  }

  ngOnDestroy(): void {
    this.searchSub.unsubscribe();
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    if (value.trim().length < 2) {
      this.results.set([]);
    }
    this.queryInput$.next(value);
  }

  select(person: DirectoryPerson): void {
    if (this.multiple()) {
      if (this.selected().some((p) => p.samAccountName === person.samAccountName)) {
        return;
      }
      this.selected.update((current) => [...current, person]);
      this.selectionChange.emit(this.selected());
      this.query.set('');
      this.results.set([]);
    } else {
      this.selected.set([person]);
      this.selectionChange.emit(person);
      this.query.set('');
      this.results.set([]);
    }
  }

  remove(person: DirectoryPerson): void {
    this.selected.update((current) => current.filter((p) => p.samAccountName !== person.samAccountName));
    this.selectionChange.emit(this.selected());
  }

  /** Clears the current selection and any in-flight query/results so the picker
   * can be reused within the same mounted page (e.g. after a successful import).
   * Resets state only; does not emit `selectionChange`. */
  clearSelection(): void {
    this.selected.set([]);
    this.query.set('');
    this.results.set([]);
  }
}
