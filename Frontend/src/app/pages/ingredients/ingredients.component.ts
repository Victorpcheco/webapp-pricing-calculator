import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';

interface IngredientMetric {
  label: string;
  value: string;
  detail: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
}

interface Ingredient {
  name: string;
  category: string;
  stock: string;
  coverage: number;
  unitCost: number;
  supplier: string;
  status: 'Ok' | 'Baixo' | 'Crítico';
  tone: 'good' | 'warning' | 'danger';
}

interface PurchaseItem {
  name: string;
  quantity: string;
  estimate: number;
}

interface CostDriver {
  name: string;
  percent: number;
  value: string;
  tone: 'green' | 'amber' | 'rose';
}

@Component({
  selector: 'app-ingredients',
  imports: [CommonModule, FormsModule, AppShellComponent],
  templateUrl: './ingredients.component.html',
  styleUrl: './ingredients.component.scss'
})
export class IngredientsComponent {
  readonly stockFilters = ['Todos', 'Baixo', 'Crítico'];
  selectedFilter = 'Todos';
  searchTerm = '';

  readonly metrics: IngredientMetric[] = [
    { label: 'Itens ativos', value: '68', detail: '12 fornecedores', tone: 'blue' },
    { label: 'Custo do mês', value: 'R$ 2.940', detail: '42% do faturamento', tone: 'amber' },
    { label: 'Estoque saudável', value: '81%', detail: '+9 pts na semana', tone: 'green' },
    { label: 'Compra urgente', value: '3', detail: 'Cobertura menor que 4 dias', tone: 'rose' }
  ];

  readonly ingredients: Ingredient[] = [
    {
      name: 'Chocolate meio amargo',
      category: 'Chocolate',
      stock: '2,4 kg',
      coverage: 3,
      unitCost: 42.9,
      supplier: 'Cacau Minas',
      status: 'Crítico',
      tone: 'danger'
    },
    {
      name: 'Leite condensado',
      category: 'Laticínios',
      stock: '18 latas',
      coverage: 6,
      unitCost: 6.8,
      supplier: 'Atacado Boa Compra',
      status: 'Baixo',
      tone: 'warning'
    },
    {
      name: 'Farinha de trigo',
      category: 'Secos',
      stock: '14 kg',
      coverage: 15,
      unitCost: 4.6,
      supplier: 'Mercado Central',
      status: 'Ok',
      tone: 'good'
    },
    {
      name: 'Granulado belga',
      category: 'Confeitos',
      stock: '4,8 kg',
      coverage: 12,
      unitCost: 31.4,
      supplier: 'Doce Pro',
      status: 'Ok',
      tone: 'good'
    },
    {
      name: 'Óleo vegetal',
      category: 'Secos',
      stock: '3 litros',
      coverage: 4,
      unitCost: 8.2,
      supplier: 'Atacado Boa Compra',
      status: 'Baixo',
      tone: 'warning'
    }
  ];

  readonly purchaseList: PurchaseItem[] = [
    { name: 'Chocolate meio amargo', quantity: '8 kg', estimate: 343.2 },
    { name: 'Leite condensado', quantity: '36 latas', estimate: 244.8 },
    { name: 'Óleo vegetal', quantity: '12 litros', estimate: 98.4 }
  ];

  readonly costDrivers: CostDriver[] = [
    { name: 'Chocolate', percent: 38, value: 'R$ 1.117', tone: 'rose' },
    { name: 'Laticínios', percent: 24, value: 'R$ 706', tone: 'amber' },
    { name: 'Secos', percent: 18, value: 'R$ 529', tone: 'green' }
  ];

  get filteredIngredients(): Ingredient[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.ingredients.filter((ingredient) => {
      const matchesFilter = this.selectedFilter === 'Todos' || ingredient.status === this.selectedFilter;
      const matchesTerm = !term || ingredient.name.toLowerCase().includes(term);
      return matchesFilter && matchesTerm;
    });
  }

  get purchaseTotal(): number {
    return this.purchaseList.reduce((total, item) => total + item.estimate, 0);
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }

  trackByLabel(_index: number, item: { label: string }): string {
    return item.label;
  }

  trackByName(_index: number, item: { name: string }): string {
    return item.name;
  }

  trackByFilter(_index: number, filter: string): string {
    return filter;
  }
}
