import { ComponentFixture, TestBed } from '@angular/core/testing';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { RestaurantResponseDTO } from '../../models/restaurant.model';
import { RestaurantListComponent } from './restaurant-list';

const API = `${environment.apiUrl}/Restaurant`;
const RESTAURANTS: RestaurantResponseDTO[] = [
  { id: 2, name: 'Zoco', address: 'C/ 2', phoneNumber: '600000002', email: 'z@x.es', isActive: false, createdAt: '2026-11-01T10:00:00' },
  { id: 1, name: 'Asador', address: 'C/ 1', phoneNumber: '600000001', email: 'a@x.es', isActive: true, createdAt: '2026-10-01T10:00:00' },
];

registerLocaleData(localeEs);

describe('RestaurantListComponent', () => {
  let fixture: ComponentFixture<RestaurantListComponent>;
  let component: RestaurantListComponent;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RestaurantListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    fixture = TestBed.createComponent(RestaurantListComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne(API).flush(RESTAURANTS);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('lists restaurants by name with their status', () => {
    const rows = Array.from(fixture.nativeElement.querySelectorAll('tbody tr')) as HTMLElement[];
    expect(rows.map((r) => r.querySelector('.fw-semibold')?.textContent?.trim())).toEqual(['Asador', 'Zoco']);
    expect(rows[1].textContent).toContain('Suspendido');
    expect(component.activeCount()).toBe(1);
  });

  it('creates a restaurant together with its first admin', () => {
    component.openNew();
    component.form.setValue({ name: ' Nuevo ', address: 'C/ 3', phoneNumber: '600000003', email: 'n@x.es', openingHours: '' });
    component.adminForm.setValue({ adminFullName: 'Ana', adminUsername: 'ana', adminPassword: 'Secreta123' });
    component.save();

    const req = http.expectOne(API);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      name: 'Nuevo', address: 'C/ 3', phoneNumber: '600000003', email: 'n@x.es', openingHours: null,
      adminFullName: 'Ana', adminUsername: 'ana', adminPassword: 'Secreta123',
    });
    req.flush({ ...RESTAURANTS[1], id: 3, name: 'Nuevo' });
    http.expectOne(API).flush(RESTAURANTS);
    expect(component.notice()).toContain('Nuevo');
  });

  it('does not send the form without the admin data', () => {
    component.openNew();
    component.form.setValue({ name: 'Nuevo', address: 'C/ 3', phoneNumber: '600000003', email: 'n@x.es', openingHours: '' });
    component.save();
    http.expectNone(API);
  });

  it('reactivates a suspended restaurant', () => {
    component.toggleActive(RESTAURANTS[0]);
    const req = http.expectOne(`${API}/2/active`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ isActive: true });
    req.flush(null);
    http.expectOne(API).flush(RESTAURANTS);
  });
});
