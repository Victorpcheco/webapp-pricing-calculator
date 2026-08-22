import { Routes } from '@angular/router';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { ResetPasswordComponent } from './pages/reset-password/reset-password.component';

export const routes: Routes = [
  {
    path: '',
    component: LoginComponent
  },
  {
    path: 'cadastro',
    component: RegisterComponent
  },
  {
    path: 'esqueci-senha',
    component: ForgotPasswordComponent
  },
  {
    path: 'redefinir-senha',
    component: ResetPasswordComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];
