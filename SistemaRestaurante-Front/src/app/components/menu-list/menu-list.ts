import { Component, OnInit, ChangeDetectorRef } from '@angular/core'; // 1. Agregamos ChangeDetectorRef
import { CommonModule } from '@angular/common';
import { MenuItemResponseDTO } from '../../models/menu.model';
import { MenuService } from '../../services/menu';

@Component({
  selector: 'app-menu-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './menu-list.html'
})
export class MenuListComponent implements OnInit {
  listaCompleta: MenuItemResponseDTO[] = []; 
  listaMenu: MenuItemResponseDTO[] = [];    
  categoriaSeleccionada: string = 'Todos';

  // 2. Inyectamos cdr en el constructor
  constructor(private menuService: MenuService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.cargarTodoElMenu();
  }

  cargarTodoElMenu(): void {
    this.categoriaSeleccionada = 'Todos';
    this.menuService.getMenuItems().subscribe({
      next: (datos) => {
        this.listaCompleta = datos;
        this.listaMenu = datos;
        
        // 3. Forzamos a Angular a redibujar el HTML con los nuevos datos recibidos
        this.cdr.detectChanges(); 
      },
      error: (err) => console.error('Error al cargar la carta:', err)
    });
  }

  filtrarPorCategoria(categoria: string): void {
    this.categoriaSeleccionada = categoria;
    
    this.listaMenu = this.listaCompleta.filter(plato => {
      // Simplificamos la lectura de la categoría de forma segura
      const catPlato = plato?.category || '';
      return catPlato.toLowerCase().trim() === categoria.toLowerCase().trim();
    });
    
    // También notificamos el cambio al filtrar localmente
    this.cdr.detectChanges();
  }
}