import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ForgotPasswordComponent } from './pages/forgot-password/forgot-password.component';
import { IngredientsComponent } from './pages/ingredients/ingredients.component';
import { LoginComponent } from './pages/login/login.component';
import { OrdersComponent } from './pages/orders/orders.component';
import { ProductCreateComponent } from './pages/product-create/product-create.component';
import { ProductsComponent } from './pages/products/products.component';
import { RegisterComponent } from './pages/register/register.component';
import { ReportsComponent } from './pages/reports/reports.component';

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
    path: 'dashboard',
    component: DashboardComponent
  },
  {
    path: 'produtos',
    component: ProductsComponent
  },
  {
    path: 'produtos/novo',
    component: ProductCreateComponent
  },
  {
    path: 'ingredientes',
    component: IngredientsComponent
  },
  {
    path: 'pedidos',
    component: OrdersComponent
  },
  {
    path: 'relatorios',
    component: ReportsComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];
