import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand')?.textContent).toContain('Restaurante Master');
  });

  it('should toggle between day and night themes', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    const initial = app.theme.theme();
    app.theme.toggle();
    expect(app.theme.theme()).not.toBe(initial);
    fixture.detectChanges();
    expect(document.documentElement.getAttribute('data-bs-theme')).toBe(app.theme.theme());
  });
});
