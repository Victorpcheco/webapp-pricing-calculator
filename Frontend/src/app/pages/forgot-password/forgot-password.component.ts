import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';
import { SpotlightDirective } from '../../shared/directives/spotlight.directive';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, SpotlightDirective],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  forgotPasswordForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  isSubmitting = false;

  onSubmit(event: Event) {
    event.preventDefault();

    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    const email = this.forgotPasswordForm.getRawValue().email;

    this.authService.requestPasswordReset({ email }).subscribe({
      next: () => {
        this.toastService.showSuccess('Instruções enviadas para o seu e-mail!');
        void this.router.navigate(['/redefinir-senha'], {
          queryParams: { email }
        });
      },
      error: (err) => {
        this.isSubmitting = false;
        this.toastService.showError(err.error?.error || 'Erro ao solicitar recuperação de senha');
      }
    });
  }
}
