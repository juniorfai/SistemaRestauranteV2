import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ThemeService } from '../../services/theme.service';
import { apiErrorMessage } from '../../utils/api-error';
import { ROLE_LABELS } from '../../utils/labels';

/** Valida que "repetir contraseña" coincida con la nueva. */
export function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const value = group.value as { newPassword?: string; confirmPassword?: string };
  return value.newPassword === value.confirmPassword ? null : { mismatch: true };
}

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './account.html',
})
export class AccountComponent {
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  readonly theme = inject(ThemeService);
  readonly roleLabels = ROLE_LABELS;

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword } = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.auth.changePassword({ currentPassword, newPassword }).subscribe({
      // La API invalida el token actual al cambiar la contraseña: hay que volver a entrar.
      next: () => this.auth.logout(),
      error: (err) => {
        this.saving.set(false);
        this.error.set(apiErrorMessage(err, 'No se pudo cambiar la contraseña.'));
      },
    });
  }
}
