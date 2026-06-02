import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  resetPasswordForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    token: ['', [Validators.required]],
    novaSenha: ['', [Validators.required, Validators.minLength(6)]]
  });

  errorMessage = '';
  successMessage = '';
  isSubmitting = false;

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['email']) {
        this.resetPasswordForm.patchValue({ email: params['email'] });
      }
    });
  }

  onSubmit(event: Event) {
    event.preventDefault();

    if (this.resetPasswordForm.invalid) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    const command = this.resetPasswordForm.getRawValue();

    this.authService.resetPassword(command).subscribe({
      next: () => {
        this.successMessage = 'Senha redefinida com sucesso! Você será redirecionado para o login.';
        setTimeout(() => {
          void this.router.navigate(['/']);
        }, 3000);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.error || 'Erro ao redefinir a senha';
      }
    });
  }
}
