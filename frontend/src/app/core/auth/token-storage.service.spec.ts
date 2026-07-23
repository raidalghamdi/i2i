import { TestBed } from '@angular/core/testing';
import { TokenStorageService } from './token-storage.service';

describe('TokenStorageService', () => {
  let service: TokenStorageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenStorageService);
  });

  afterEach(() => localStorage.clear());

  it('has no session initially', () => {
    expect(service.hasSession()).toBe(false);
    expect(service.getAccessToken()).toBeNull();
  });

  it('stores and retrieves a token set', () => {
    service.set({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });

    expect(service.getAccessToken()).toBe('access-1');
    expect(service.getRefreshToken()).toBe('refresh-1');
    expect(service.hasSession()).toBe(true);
  });

  it('reports not-expiring-soon for a token with plenty of time left', () => {
    service.set({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      expiresAt: new Date(Date.now() + 5 * 60_000).toISOString(),
    });

    expect(service.isAccessTokenExpiringSoon()).toBe(false);
  });

  it('reports expiring-soon when within the 30s window', () => {
    service.set({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      expiresAt: new Date(Date.now() + 10_000).toISOString(),
    });

    expect(service.isAccessTokenExpiringSoon()).toBe(true);
  });

  it('reports expiring-soon when there is no stored expiry at all', () => {
    expect(service.isAccessTokenExpiringSoon()).toBe(true);
  });

  it('clears all stored values', () => {
    service.set({
      accessToken: 'access-1',
      refreshToken: 'refresh-1',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });

    service.clear();

    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(service.hasSession()).toBe(false);
  });
});
