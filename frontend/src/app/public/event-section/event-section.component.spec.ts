import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { EventSectionComponent } from './event-section.component';

describe('EventSectionComponent', () => {
  let fixture: ComponentFixture<EventSectionComponent>;

  function setup(section: string): void {
    TestBed.configureTestingModule({
      imports: [EventSectionComponent, HttpClientTestingModule],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => section } } },
        },
      ],
    });
    fixture = TestBed.createComponent(EventSectionComponent);
  }

  it('renders the workshops section title and a workshop item', () => {
    setup('workshops');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Workshops');
    expect(text).toContain('Idea Framing & Problem Definition');
    expect(text).toContain('2026-02-10');
  });

  it('renders the main section title without a workshop list', () => {
    setup('main');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Main Competition');
    expect(text).not.toContain('Idea Framing & Problem Definition');
  });

  it('shows a not-found message for an unknown section', () => {
    setup('bogus');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Section not found');
    expect(text).toContain('Event not found.');
    expect(text).toContain('Back to Events');
  });
});
