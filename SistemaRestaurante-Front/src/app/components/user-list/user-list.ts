import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { RestaurantRole, UserResponseDTO } from '../../models/auth.model';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { apiErrorMessage } from '../../utils/api-error';
import { ROLE_DESCRIPTIONS, ROLE_LABELS, ROLES } from '../../utils/labels';

type Panel =
  | { mode: 'closed' }
  | { mode: 'new' }
  | { mode: 'edit'; user: UserResponseDTO }
  | { mode: 'password'; user: UserResponseDTO };

/**
 * Gestión del personal: altas, cambio de rol, bajas y nueva contraseña (administrador del
 * restaurante). El administrador de la plataforma ve a todo el personal de todos los
 * restaurantes y solo puede ponerles una contraseña nueva (soporte).
 */
@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './user-list.html',
})
export class UserListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  readonly auth = inject(AuthService);

  readonly roles = ROLES;
  readonly roleLabels = ROLE_LABELS;
  readonly roleDescriptions = ROLE_DESCRIPTIONS;

  readonly users = signal<UserResponseDTO[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly panel = signal<Panel>({ mode: 'closed' });

  /** Vista de soporte de la plataforma: solo lectura y cambio de contraseña. */
  readonly platformMode = this.auth.isPlatformAdmin;

  readonly form = this.fb.nonNullable.group({
    username: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9._-]{3,50}$/)]],
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    role: ['Staff' as RestaurantRole, Validators.required],
    isActive: [true],
    password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.userService.getUsers().subscribe({
      next: (data) => {
        // El propio administrador de la plataforma no es personal de ningún restaurante.
        const staff = data.filter((u) => u.role !== 'PlatformAdmin');
        this.users.set(
          this.platformMode()
            ? [...staff].sort((a, b) => (a.restaurantName ?? '').localeCompare(b.restaurantName ?? '', 'es'))
            : staff,
        );
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(apiErrorMessage(err, 'No se pudieron cargar los usuarios.'));
        this.loading.set(false);
      },
    });
  }

  isSelf(user: UserResponseDTO): boolean {
    return user.id === this.auth.user()?.id;
  }

  /** La API guarda las fechas en UTC sin indicar la zona; se marca para mostrarlas en hora local. */
  asUtc(value: string): string {
    return /Z|[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`;
  }

  openNew(): void {
    this.form.reset({ username: '', fullName: '', role: 'Staff', isActive: true, password: '' });
    this.setEnabled({ username: true, password: true, roleAndStatus: true });
    this.open({ mode: 'new' });
  }

  openEdit(user: UserResponseDTO): void {
    this.form.reset({ username: user.username, fullName: user.fullName, role: asRestaurantRole(user), isActive: user.isActive, password: '' });
    this.setEnabled({ username: false, password: false, roleAndStatus: !this.isSelf(user) });
    this.open({ mode: 'edit', user });
  }

  openPassword(user: UserResponseDTO): void {
    this.form.reset({ username: user.username, fullName: user.fullName, role: asRestaurantRole(user), isActive: user.isActive, password: '' });
    this.setEnabled({ username: false, password: true, roleAndStatus: true });
    this.open({ mode: 'password', user });
  }

  close(): void {
    this.panel.set({ mode: 'closed' });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const panel = this.panel();
    const value = this.form.getRawValue();
    let request$: Observable<unknown>;
    let done: string;

    switch (panel.mode) {
      case 'new':
        request$ = this.userService.createUser({
          username: value.username.trim(),
          fullName: value.fullName.trim(),
          role: value.role,
          password: value.password,
        });
        done = `Usuario ${value.username.trim().toLowerCase()} creado.`;
        break;
      case 'edit':
        request$ = this.userService.updateUser(panel.user.id, {
          fullName: value.fullName.trim(),
          role: value.role,
          isActive: value.isActive,
        });
        done = 'Cambios guardados.';
        break;
      case 'password':
        request$ = this.userService.resetPassword(panel.user.id, value.password);
        done = `Contraseña de ${panel.user.username} cambiada. Sus sesiones abiertas se han cerrado.`;
        break;
      default:
        return;
    }

    this.saving.set(true);
    this.error.set(null);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.notice.set(done);
        this.close();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudieron guardar los cambios.'));
      },
    });
  }

  private open(panel: Panel): void {
    this.error.set(null);
    this.notice.set(null);
    this.panel.set(panel);
  }

  private setEnabled(fields: { username: boolean; password: boolean; roleAndStatus: boolean }): void {
    const c = this.form.controls;
    const toggle = (enabled: boolean, ...controls: { enable(): void; disable(): void }[]) =>
      controls.forEach((control) => (enabled ? control.enable() : control.disable()));
    toggle(fields.username, c.username);
    toggle(fields.password, c.password);
    toggle(fields.roleAndStatus, c.role, c.isActive);
  }
}

function asRestaurantRole(user: UserResponseDTO): RestaurantRole {
  return user.role === 'PlatformAdmin' ? 'Staff' : user.role;
}
