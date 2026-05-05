import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';

interface ProductMetric {
  label: string;
  value: string;
  detail: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
}

interface Product {
  name: string;
  category: string;
  cost: number;
  price: number;
  margin: number;
  sales: number;
  status: string;
  tone: 'good' | 'warning' | 'danger';
  lastReview: string;
}

interface ReviewItem {
  name: string;
  reason: string;
  impact: string;
  tone: 'good' | 'warning' | 'danger';
}

@Component({
  selector: 'app-products',
  imports: [CommonModule, FormsModule, RouterLink, AppShellComponent],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent {
  readonly categories = ['Todos', 'Bolos', 'Docinhos', 'Brownies', 'Salgados'];
  selectedCategory = 'Todos';
  searchTerm = '';

  readonly metrics: ProductMetric[] = [
    { label: 'Produtos ativos', value: '42', detail: '8 categorias no cardápio', tone: 'blue' },
    { label: 'Margem média', value: '52%', detail: '+4 pts no mês', tone: 'green' },
    { label: 'Para revisar', value: '6', detail: 'Custos mudaram', tone: 'amber' },
    { label: 'Abaixo da meta', value: '3', detail: 'Margem menor que 35%', tone: 'rose' }
  ];

  readonly products: Product[] = [
    {
      name: 'Bolo no pote chocolate',
      category: 'Bolos',
      cost: 5.8,
      price: 14,
      margin: 58.6,
      sales: 86,
      status: 'Saudável',
      tone: 'good',
      lastReview: 'Hoje'
    },
    {
      name: 'Brigadeiro gourmet',
      category: 'Docinhos',
      cost: 1.45,
      price: 3.5,
      margin: 58.5,
      sales: 220,
      status: 'Saudável',
      tone: 'good',
      lastReview: 'Ontem'
    },
    {
      name: 'Brownie recheado',
      category: 'Brownies',
      cost: 4.9,
      price: 9.5,
      margin: 48.4,
      sales: 74,
      status: 'Observar',
      tone: 'warning',
      lastReview: '3 dias'
    },
    {
      name: 'Cento de salgados',
      category: 'Salgados',
      cost: 64,
      price: 96,
      margin: 33.3,
      sales: 18,
      status: 'Recalcular',
      tone: 'danger',
      lastReview: '12 dias'
    },
    {
      name: 'Mini cheesecake',
      category: 'Docinhos',
      cost: 3.2,
      price: 8,
      margin: 60,
      sales: 64,
      status: 'Saudável',
      tone: 'good',
      lastReview: '1 semana'
    }
  ];

  readonly reviewItems: ReviewItem[] = [
    {
      name: 'Cento de salgados',
      reason: 'Óleo e farinha subiram',
      impact: '+R$ 18,00 sugeridos',
      tone: 'danger'
    },
    {
      name: 'Brownie recheado',
      reason: 'Chocolate acima da média',
      impact: '+R$ 1,40 por unidade',
      tone: 'warning'
    },
    {
      name: 'Mini cheesecake',
      reason: 'Boa margem atual',
      impact: 'Manter preço',
      tone: 'good'
    }
  ];

  get filteredProducts(): Product[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.products.filter((product) => {
      const matchesCategory = this.selectedCategory === 'Todos' || product.category === this.selectedCategory;
      const matchesTerm = !term || product.name.toLowerCase().includes(term);
      return matchesCategory && matchesTerm;
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }

  formatPercent(value: number): string {
    return `${Math.round(value)}%`;
  }

  trackByLabel(_index: number, item: { label: string }): string {
    return item.label;
  }

  trackByName(_index: number, item: { name: string }): string {
    return item.name;
  }

  trackByCategory(_index: number, category: string): string {
    return category;
  }
}
