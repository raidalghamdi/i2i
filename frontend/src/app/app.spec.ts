import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  let fixture: ComponentFixture<App>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    });
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();
  });

  it('creates the root component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders only a router outlet', () => {
    expect(fixture.nativeElement.querySelector('router-outlet')).not.toBeNull();
  });
});
