import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';

interface KpiCard {
  label: string;
  value: string;
  detail: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
}

interface ProductRow {
  name: string;
  category: string;
  cost: number;
  price: number;
  margin: number;
  volume: number;
  status: string;
  tone: 'good' | 'warning' | 'danger';
}

interface CostItem {
  label: string;
  value: string;
  percent: number;
  tone: 'blue' | 'green' | 'amber' | 'rose';
}

interface StockAlert {
  name: string;
  amount: string;
  coverage: string;
  level: 'ok' | 'low' | 'critical';
}

interface ProductionItem {
  product: string;
  quantity: number;
  due: string;
  status: string;
}

interface SalesChannel {
  name: string;
  value: string;
  percent: number;
}

interface PriceCalculator {
  recipeCost: number;
  portions: number;
  packageCost: number;
  fees: number;
  desiredMargin: number;
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, FormsModule, RouterLink, AppShellComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  readonly businessName = 'Doces da Maria';
  readonly today = new Intl.DateTimeFormat('pt-BR', {
    day: '2-digit',
    month: 'long',
    weekday: 'long'
  }).format(new Date());

  readonly periodFilters = ['Hoje', '7 dias', '30 dias'];
  selectedPeriod = '7 dias';

  calculator: PriceCalculator = {
    recipeCost: 86.4,
    portions: 24,
    packageCost: 0.75,
    fees: 1.2,
    desiredMargin: 62
  };

  readonly kpis: KpiCard[] = [
    {
      label: 'Receita prevista',
      value: 'R$ 8.420',
      detail: '+18% vs. semana passada',
      tone: 'blue'
    },
    {
      label: 'Lucro bruto',
      value: 'R$ 3.180',
      detail: '37,8% de margem no período',
      tone: 'green'
    },
    {
      label: 'Produtos revisados',
      value: '24',
      detail: '6 precisam de novo preço',
      tone: 'amber'
    },
    {
      label: 'Risco de ruptura',
      value: '3',
      detail: 'Ingredientes para repor',
      tone: 'rose'
    }
  ];

  readonly products: ProductRow[] = [
    {
      name: 'Bolo no pote chocolate',
      category: 'Bolos',
      cost: 5.8,
      price: 14,
      margin: 58.6,
      volume: 86,
      status: 'Saudável',
      tone: 'good'
    },
    {
      name: 'Brigadeiro gourmet',
      category: 'Docinhos',
      cost: 1.45,
      price: 3.5,
      margin: 58.5,
      volume: 220,
      status: 'Saudável',
      tone: 'good'
    },
    {
      name: 'Brownie recheado',
      category: 'Brownies',
      cost: 4.9,
      price: 9.5,
      margin: 48.4,
      volume: 74,
      status: 'Observar',
      tone: 'warning'
    },
    {
      name: 'Cento de salgados',
      category: 'Salgados',
      cost: 64,
      price: 96,
      margin: 33.3,
      volume: 18,
      status: 'Recalcular',
      tone: 'danger'
    }
  ];

  readonly costBreakdown: CostItem[] = [
    { label: 'Ingredientes', value: 'R$ 2.940', percent: 42, tone: 'blue' },
    { label: 'Embalagens', value: 'R$ 860', percent: 18, tone: 'amber' },
    { label: 'Taxas e entrega', value: 'R$ 740', percent: 16, tone: 'rose' },
    { label: 'Energia e gás', value: 'R$ 420', percent: 9, tone: 'green' }
  ];

  readonly stockAlerts: StockAlert[] = [
    {
      name: 'Chocolate meio amargo',
      amount: '2,4 kg',
      coverage: '3 dias',
      level: 'critical'
    },
    {
      name: 'Leite condensado',
      amount: '18 latas',
      coverage: '6 dias',
      level: 'low'
    },
    {
      name: 'Granulado belga',
      amount: '4,8 kg',
      coverage: '12 dias',
      level: 'ok'
    }
  ];

  readonly productionQueue: ProductionItem[] = [
    {
      product: 'Bolo no pote chocolate',
      quantity: 32,
      due: 'Hoje, 16h',
      status: 'Separar etiquetas'
    },
    {
      product: 'Brigadeiro gourmet',
      quantity: 120,
      due: 'Amanhã, 9h',
      status: 'Comprar forminhas'
    },
    {
      product: 'Brownie recheado',
      quantity: 48,
      due: 'Amanhã, 14h',
      status: 'Produzir recheio'
    }
  ];

  readonly salesChannels: SalesChannel[] = [
    { name: 'Instagram', value: 'R$ 3.280', percent: 39 },
    { name: 'WhatsApp', value: 'R$ 2.740', percent: 33 },
    { name: 'Feiras', value: 'R$ 1.520', percent: 18 },
    { name: 'Apps', value: 'R$ 880', percent: 10 }
  ];

  selectPeriod(period: string): void {
    this.selectedPeriod = period;
  }

  get recipeCostPerUnit(): number {
    return this.divide(this.toNumber(this.calculator.recipeCost), this.toNumber(this.calculator.portions));
  }

  get totalUnitCost(): number {
    return this.recipeCostPerUnit + this.toNumber(this.calculator.packageCost) + this.toNumber(this.calculator.fees);
  }

  get suggestedPrice(): number {
    const margin = Math.min(Math.max(this.toNumber(this.calculator.desiredMargin), 1), 90) / 100;
    return this.divide(this.totalUnitCost, 1 - margin);
  }

  get profitPerUnit(): number {
    return Math.max(this.suggestedPrice - this.totalUnitCost, 0);
  }

  get realMargin(): number {
    return this.divide(this.profitPerUnit, this.suggestedPrice) * 100;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(this.toNumber(value));
  }

  formatPercent(value: number): string {
    return `${Math.round(this.toNumber(value))}%`;
  }

  trackByLabel(_index: number, item: { label: string }): string {
    return item.label;
  }

  trackByName(_index: number, item: { name: string }): string {
    return item.name;
  }

  trackByProduct(_index: number, item: { product: string }): string {
    return item.product;
  }

  trackByPeriod(_index: number, period: string): string {
    return period;
  }

  private toNumber(value: number): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private divide(value: number, divisor: number): number {
    return divisor > 0 ? value / divisor : 0;
  }
}
