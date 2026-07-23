import { TestBed } from '@angular/core/testing';
import { LocaleService } from './locale.service';

describe('LocaleService', () => {
  let service: LocaleService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LocaleService);
  });

  it('returns a non-empty string href for the alternate locale', () => {
    const href = service.alternateLocaleHref();
    expect(typeof href).toBe('string');
    expect(href.length).toBeGreaterThan(0);
  });
});
