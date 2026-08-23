import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);

  loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  isSubmitting = false;

  onSubmit(event: Event) {
    event.preventDefault();

    if (this.loginForm.invalid || this.isSubmitting) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const { email, password } = this.loginForm.getRawValue();

    this.isSubmitting = true;

    this.authService.login({ email, senhaHash: password }).subscribe({
      next: () => {
        this.toastService.showSuccess('Login realizado com sucesso!');
        void this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.toastService.showError(
          err.error?.Error || err.error?.error || 'Erro ao realizar login'
        );
      }
    });
  }
}
