import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  registerForm = this.fb.nonNullable.group({
    nome: ['', [Validators.required]],
    telefone: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });

  errorMessage = '';

  onSubmit(event: Event) {
    event.preventDefault();

    if (this.registerForm.invalid) {
      return;
    }

    const { nome, telefone, email, password, confirmPassword } = this.registerForm.getRawValue();

    if (password !== confirmPassword) {
      this.errorMessage = 'As senhas não coincidem';
      return;
    }

    this.authService.register({ nome, telefone, email, senhaHash: password }).subscribe({
      next: () => {
        // Automatically login or navigate to login
        this.authService.login({ email, senhaHash: password }).subscribe({
          next: () => {
            void this.router.navigate(['/dashboard']);
          },
          error: () => {
            void this.router.navigate(['/login']);
          }
        });
      },
      error: (err) => {
        this.errorMessage = err.error?.error || 'Erro ao realizar cadastro';
      }
    });
  }
}
