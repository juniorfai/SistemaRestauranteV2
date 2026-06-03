import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MenuItemResponseDTO } from '../../models/menu.model';
import { MenuService } from '../../services/menu';
;

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

  constructor(private menuService: MenuService) {}

  ngOnInit(): void {
    this.cargarTodoElMenu();
  }

  cargarTodoElMenu(): void {
    this.categoriaSeleccionada = 'Todos';
    this.menuService.getMenuItems().subscribe({
      next: (datos) => {
        this.listaCompleta = datos;
        this.listaMenu = datos;
      },
      error: (err) => console.error('Error al cargar la carta:', err)
    });
  }

  filtrarPorCategoria(categoria: string): void {
    this.categoriaSeleccionada = categoria;
    
    // Filtramos localmente usando el respaldo para que la navegación sea instantánea
    this.listaMenu = this.listaCompleta.filter(plato => {
      const catPlato = plato.category || plato['category'] || plato['category'] || '';
      return catPlato.toLowerCase().trim() === categoria.toLowerCase().trim();
    });
  }
}