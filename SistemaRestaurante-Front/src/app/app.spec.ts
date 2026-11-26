import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { UserRole } from './models/auth.model';

/** Guarda una sesión válida como la que deja el login. */
export function fakeSession(role: UserRole = 'Admin'): void {
  localStorage.setItem(
    'rm-session',
    JSON.stringify({
      token: 'test-token',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: { id: 1, username: 'test', fullName: 'Tester', role, isActive: true, createdAt: '2026-01-01' },
    }),
  );
}

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();
  });

  async function create(role?: UserRole) {
    if (role) fakeSession(role);
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    return fixture;
  }

  it('should not show the menu without a session', async () => {
    const fixture = await create();
    expect((fixture.nativeElement as HTMLElement).querySelector('.sidebar')).toBeNull();
  });

  it('should render the navigation for a logged-in user', async () => {
    const fixture = await create('Admin');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand')?.textContent).toContain('Restaurante Master');
    expect(compiled.querySelector('.user-name')?.textContent).toContain('Tester');
  });

  it('should show "Personal" only to administrators', async () => {
    const admin = await create('Admin');
    expect(admin.componentInstance.navItems().map((i) => i.label)).toContain('Personal');
    TestBed.resetTestingModule();
    localStorage.clear();

    const staff = await create('Staff');
    expect(staff.componentInstance.navItems().map((i) => i.label)).not.toContain('Personal');
  });

  it('should toggle between day and night themes', async () => {
    const fixture = await create('Staff');
    const app = fixture.componentInstance;
    const initial = app.theme.theme();
    app.theme.toggle();
    expect(app.theme.theme()).not.toBe(initial);
    fixture.detectChanges();
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe(app.theme.theme());
  });
});
