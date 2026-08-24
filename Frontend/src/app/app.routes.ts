import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './guards/auth.guard';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { LandingComponent } from './pages/landing/landing.component';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { ResetPasswordComponent } from './pages/reset-password/reset-password.component';

export const routes: Routes = [
  {
    path: '',
    component: LandingComponent
  },
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'cadastro',
    component: RegisterComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'esqueci-senha',
    component: ForgotPasswordComponent,
    canActivate: [guestGuard]
  },
  {
    path: 'redefinir-senha',
    component: ResetPasswordComponent,
    canActivate: [guestGuard]
  },
  // As telas internas são carregadas sob demanda: quem abre o login
  // não precisa baixar o workspace inteiro.
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: 'meus-custos',
    loadComponent: () => import('./pages/costs/costs.component').then(m => m.CostsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'meus-insumos',
    loadComponent: () => import('./pages/supplies/supplies.component').then(m => m.SuppliesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'meus-produtos',
    loadComponent: () => import('./pages/products/products.component').then(m => m.ProductsComponent),
    canActivate: [authGuard]
  },
  {
    path: 'meus-colaboradores',
    loadComponent: () => import('./pages/employees/employees.component').then(m => m.EmployeesComponent),
    canActivate: [authGuard]
  },
  {
    path: 'precificacao',
    loadComponent: () => import('./pages/pricing/pricing.component').then(m => m.PricingComponent),
    canActivate: [authGuard]
  },
  {
    path: 'meus-resultados',
    loadComponent: () => import('./pages/results/results.component').then(m => m.ResultsComponent),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
