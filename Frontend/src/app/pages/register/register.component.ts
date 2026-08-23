import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

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
  private readonly toastService = inject(ToastService);

  registerForm = this.fb.nonNullable.group({
    nome: ['', [Validators.required]],
    telefone: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(15)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$/)]],
    confirmPassword: ['', [Validators.required]]
  });

  isSubmitting = false;

  formatPhone(): void {
    const phoneControl = this.registerForm.controls.telefone;
    const digits = phoneControl.value.replace(/\D/g, '').slice(0, 11);
    let formattedPhone = digits;

    if (digits.length > 2 && digits.length <= 10) {
      formattedPhone = `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
    } else if (digits.length > 10) {
      formattedPhone = `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
    }

    if (formattedPhone !== phoneControl.value) {
      phoneControl.setValue(formattedPhone, { emitEvent: false });
    }
  }

  onSubmit(event: Event) {
    event.preventDefault();

    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const { nome, telefone, email, password, confirmPassword } = this.registerForm.getRawValue();

    if (password !== confirmPassword) {
      this.toastService.showError('As senhas não coincidem');
      return;
    }

    this.isSubmitting = true;

    this.authService.register({ nome, telefone, email, senhaHash: password }).subscribe({
      next: () => {
        this.toastService.showSuccess('Cadastro realizado com sucesso!');
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
        this.isSubmitting = false;
        this.toastService.showError(err.error?.error || 'Erro ao realizar cadastro');
      }
    });
  }
}
