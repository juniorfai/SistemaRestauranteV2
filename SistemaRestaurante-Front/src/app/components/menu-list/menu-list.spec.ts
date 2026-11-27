import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { MenuItemResponseDTO } from '../../models/menu.model';
import { MenuListComponent } from './menu-list';

const ITEMS: MenuItemResponseDTO[] = [
  { id: 1, name: 'Bruschetta', price: 6.99, category: 'Appetizer', categoryName: 'Appetizer', isAvailable: true, restaurantId: 1, stationId: 1, stationName: 'Cocina', allergens: [] },
  { id: 2, name: 'Brownie', price: 6.99, category: 'Dessert', categoryName: 'Dessert', isAvailable: true, restaurantId: 1, stationId: 1, stationName: 'Cocina', allergens: [] },
];

describe('MenuListComponent', () => {
  let component: MenuListComponent;
  let fixture: ComponentFixture<MenuListComponent>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MenuListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(MenuListComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne((req) => req.url.endsWith('/MenuItem')).flush(ITEMS);
  });

  it('should list all items', () => {
    expect(component.visibleItems().length).toBe(2);
  });

  it('should filter by category', () => {
    component.selectedCategory.set('Dessert');
    expect(component.visibleItems().map((i) => i.name)).toEqual(['Brownie']);
  });
});
