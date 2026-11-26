import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { environment } from '../../environments/environment';
import { authInterceptor } from '../auth/auth.interceptor';
import { LoginResponseDTO } from '../models/auth.model';
import { AuthService } from './auth.service';

const LOGIN: LoginResponseDTO = {
  token: 'abc.def.ghi',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: { id: 7, username: 'camarero', fullName: 'Pablo', role: 'Staff', isActive: true, createdAt: '2026-01-01' },
};

describe('AuthService + authInterceptor', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  let client: HttpClient;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    router = TestBed.inject(Router);
  });

  afterEach(() => http.verify());

  function login(): void {
    auth.login({ username: 'camarero', password: 'x' }).subscribe();
    const req = http.expectOne(`${environment.apiUrl}/Auth/login`);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush(LOGIN);
  }

  it('stores the session after login', () => {
    login();
    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.user()?.role).toBe('Staff');
    expect(auth.canManage()).toBe(false);
    expect(localStorage.getItem('rm-session')).toContain('abc.def.ghi');
  });

  it('adds the bearer token to API calls', () => {
    login();
    client.get(`${environment.apiUrl}/Table`).subscribe();
    const req = http.expectOne(`${environment.apiUrl}/Table`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    req.flush([]);
  });

  it('logs out when the API answers 401', () => {
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    login();
    client.get(`${environment.apiUrl}/Table`).subscribe({ error: () => undefined });
    http.expectOne(`${environment.apiUrl}/Table`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.isLoggedIn()).toBe(false);
    expect(localStorage.getItem('rm-session')).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('ignores an expired stored session', () => {
    localStorage.setItem('rm-session', JSON.stringify({ ...LOGIN, expiresAt: new Date(Date.now() - 1000).toISOString() }));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    const fresh = TestBed.inject(AuthService);
    expect(fresh.isLoggedIn()).toBe(false);
    expect(fresh.token()).toBeNull();
    http = TestBed.inject(HttpTestingController);
  });
});
