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
  // As telas internas são carregadas sob demanda: quem abre o login
  // não precisa baixar o workspace inteiro.
  {
    path: 'dashboard',
    loadComponent: () => import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'meus-custos',
    loadComponent: () => import('./pages/costs/costs.component').then(m => m.CostsComponent)
  },
  {
    path: 'meus-insumos',
    loadComponent: () => import('./pages/supplies/supplies.component').then(m => m.SuppliesComponent)
  },
  {
    path: 'meus-produtos',
    loadComponent: () => import('./pages/products/products.component').then(m => m.ProductsComponent)
  },
  {
    path: 'meus-colaboradores',
    loadComponent: () => import('./pages/employees/employees.component').then(m => m.EmployeesComponent)
  },
  {
    path: 'precificacao',
    loadComponent: () => import('./pages/pricing/pricing.component').then(m => m.PricingComponent)
  },
  {
    path: 'meus-resultados',
    loadComponent: () => import('./pages/results/results.component').then(m => m.ResultsComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
