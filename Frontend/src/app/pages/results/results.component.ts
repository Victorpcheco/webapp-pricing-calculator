import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { MockStoreService, PricingSimulation, Product } from '../../services/mock-store.service';

type Period = 'all' | 'today' | 'week' | 'month' | 'custom';

interface ResultRow {
  name: string;
  unit: string;
  cost: string;
  sale: string;
  profit: string;
  profitClass: 'green' | 'red';
  margin: string;
  viability: { cls: 'profit' | 'loss' | 'balance'; label: string };
  priced: boolean;
}

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — RESULTADOS
 * GET /api/resultados?periodo=all|today|week|month&inicio=&fim=
 * HEADERS: Authorization: Bearer <token>
 *
 * Consolida receitas (fichas técnicas) e simulações de preço:
 *   Lucro       = PreçoVenda − CustoUnitário
 *   MargemReal  = (Lucro ÷ PreçoVenda) × 100
 * ============================================================
 */
@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent],
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss'
})
export class ResultsComponent {
  private readonly store = inject(MockStoreService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  private readonly percentFormat = new Intl.NumberFormat('pt-BR', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  });

  activePeriod: Period = 'all';
  dateStart = '';
  dateEnd = '';

  readonly periods: { value: Period; label: string }[] = [
    { value: 'all', label: 'Todo o período' },
    { value: 'today', label: 'Hoje' },
    { value: 'week', label: 'Esta semana' },
    { value: 'month', label: 'Este mês' },
    { value: 'custom', label: 'Personalizado' }
  ];

  setPeriod(period: Period) {
    this.activePeriod = period;
  }

  applyCustomDate() {
    // O filtro já reage a dateStart/dateEnd; o botão existe para paridade com o mockup.
  }

  /* ===================== FILTRO ===================== */

  private inPeriod(iso: string | undefined): boolean {
    if (this.activePeriod === 'all') return true;
    if (!iso) return true;

    const date = new Date(iso);
    const now = new Date();

    if (this.activePeriod === 'today') return date.toDateString() === now.toDateString();
    if (this.activePeriod === 'week') return date >= new Date(now.getTime() - 7 * 864e5);
    if (this.activePeriod === 'month') {
      return date.getMonth() === now.getMonth() && date.getFullYear() === now.getFullYear();
    }
    if (this.activePeriod === 'custom') {
      const start = this.dateStart ? new Date(`${this.dateStart}T00:00:00`) : null;
      const end = this.dateEnd ? new Date(`${this.dateEnd}T23:59:59`) : null;
      if (start && date < start) return false;
      if (end && date > end) return false;
    }
    return true;
  }

  private get simulations(): PricingSimulation[] {
    return this.store.simulations.filter(item => this.inPeriod(item.createdAt));
  }

  private get recipes(): Product[] {
    return this.store.products.filter(item => this.inPeriod(item.updatedAt));
  }

  /** Custo vindo da receita atual, com fallback para o gravado na simulação. */
  private costOf(simulation: PricingSimulation): number {
    const recipe = this.store.findProduct(simulation.recipeId);
    return Number(recipe?.unitCost !== undefined ? recipe.unitCost : simulation.cost || 0);
  }

  /* ===================== KPIs ===================== */

  private get totals() {
    let revenue = 0;
    let profit = 0;

    for (const simulation of this.simulations) {
      const cost = this.costOf(simulation);
      const sale = Number(simulation.salePrice || 0);
      const quantity = Number(simulation.quantity || 1);
      revenue += sale * quantity;
      profit += (sale - cost) * quantity;
    }

    return { revenue, profit, margin: revenue > 0 ? (profit / revenue) * 100 : 0 };
  }

  get totalProfit(): string {
    return this.currency.format(this.totals.profit);
  }

  get totalProfitColor(): string {
    return this.totals.profit >= 0 ? 'var(--success)' : 'var(--danger)';
  }

  get averageMargin(): string {
    return `${this.percentFormat.format(this.totals.margin)}%`;
  }

  get totalRevenue(): string {
    return this.currency.format(this.totals.revenue);
  }

  get analysedCount(): number {
    return this.simulations.length || this.recipes.length;
  }

  /* ===================== TABELA ===================== */

  get rows(): ResultRow[] {
    if (this.simulations.length) {
      return this.simulations.map(simulation => {
        const recipe = this.store.findProduct(simulation.recipeId);
        const cost = this.costOf(simulation);
        const sale = Number(simulation.salePrice || 0);
        const profit = sale - cost;
        const margin = sale > 0 ? (profit / sale) * 100 : 0;

        return {
          name: recipe?.name || simulation.recipeName || 'Produto',
          unit: recipe?.yieldName || 'unidade',
          cost: this.currency.format(cost),
          sale: this.currency.format(sale),
          profit: this.currency.format(profit),
          profitClass: profit >= 0 ? ('green' as const) : ('red' as const),
          margin: `${this.percentFormat.format(margin)}%`,
          viability:
            profit > 0.005
              ? { cls: 'profit' as const, label: 'Lucro' }
              : profit < -0.005
                ? { cls: 'loss' as const, label: 'Prejuízo' }
                : { cls: 'balance' as const, label: 'Equilíbrio' },
          priced: true
        };
      });
    }

    // Sem simulações: lista as receitas apenas com o custo, como no mockup.
    return this.recipes.map(recipe => ({
      name: recipe.name || 'Produto',
      unit: recipe.yieldName || 'unidade',
      cost: this.currency.format(recipe.unitCost || 0),
      sale: '—',
      profit: '—',
      profitClass: 'green' as const,
      margin: '—',
      viability: { cls: 'balance' as const, label: 'Sem preço' },
      priced: false
    }));
  }

  get isEmpty(): boolean {
    return !this.simulations.length && !this.recipes.length;
  }
}
