import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import {
  ResultadoResumo,
  ResultadoRow,
  ResultadosApiService,
  ResultadosPeriodo
} from '../../services/resultados-api.service';

interface ResultRowView {
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
 * GET /api/resultados?periodo=all|today|week|month|custom&inicio=&fim=
 * HEADERS: Authorization: Bearer <token> (authInterceptor)
 *
 * O backend consolida fichas técnicas e simulações de preço, recalculando o
 * lucro com o custo VIGENTE do produto (não o retrato congelado da simulação).
 * Este componente só formata e classifica (cor/rótulo) o que a API já calculou.
 * ============================================================
 */
@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent],
  templateUrl: './results.component.html',
  styleUrl: './results.component.scss'
})
export class ResultsComponent implements OnInit {
  private readonly api = inject(ResultadosApiService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  private readonly percentFormat = new Intl.NumberFormat('pt-BR', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;

  loading = false;

  activePeriod: ResultadosPeriodo = 'all';
  dateStart = '';
  dateEnd = '';

  rows: ResultadoRow[] = [];
  totals: ResultadoResumo = { totalProfit: 0, totalRevenue: 0, averageMargin: 0, analysedCount: 0 };

  readonly periods: { value: ResultadosPeriodo; label: string }[] = [
    { value: 'all', label: 'Todo o período' },
    { value: 'today', label: 'Hoje' },
    { value: 'week', label: 'Esta semana' },
    { value: 'month', label: 'Este mês' },
    { value: 'custom', label: 'Personalizado' }
  ];

  ngOnInit() {
    this.carregar();
  }

  setPeriod(period: ResultadosPeriodo) {
    this.activePeriod = period;
    // "Personalizado" só busca quando o usuário confirmar o intervalo em "Filtrar"
    if (period !== 'custom') this.carregar();
  }

  applyCustomDate() {
    this.carregar();
  }

  private carregar() {
    this.loading = true;
    this.api
      .listar({
        periodo: this.activePeriod,
        inicio: this.activePeriod === 'custom' && this.dateStart ? this.dateStart : undefined,
        fim: this.activePeriod === 'custom' && this.dateEnd ? this.dateEnd : undefined
      })
      .subscribe({
        next: response => {
          this.rows = response.rows;
          this.totals = response.totals;
          this.loading = false;
        },
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao carregar os resultados.'));
          this.loading = false;
        }
      });
  }

  /* ===================== KPIs ===================== */

  get totalProfit(): string {
    return this.currency.format(this.totals.totalProfit);
  }

  get totalProfitColor(): string {
    return this.totals.totalProfit >= 0 ? 'var(--success)' : 'var(--danger)';
  }

  get averageMargin(): string {
    return `${this.percentFormat.format(this.totals.averageMargin)}%`;
  }

  get totalRevenue(): string {
    return this.currency.format(this.totals.totalRevenue);
  }

  get analysedCount(): number {
    return this.totals.analysedCount;
  }

  /* ===================== TABELA ===================== */

  get rowViews(): ResultRowView[] {
    return this.rows.map(row => {
      if (!row.priced) {
        return {
          name: row.name || 'Produto',
          unit: row.unit || 'unidade',
          cost: this.currency.format(row.cost),
          sale: '—',
          profit: '—',
          profitClass: 'green' as const,
          margin: '—',
          viability: { cls: 'balance' as const, label: 'Sem preço' },
          priced: false
        };
      }

      const profit = row.profit ?? 0;

      return {
        name: row.name || 'Produto',
        unit: row.unit || 'unidade',
        cost: this.currency.format(row.cost),
        sale: this.currency.format(row.salePrice ?? 0),
        profit: this.currency.format(profit),
        profitClass: profit >= 0 ? ('green' as const) : ('red' as const),
        margin: `${this.percentFormat.format(row.margin ?? 0)}%`,
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

  get isEmpty(): boolean {
    return !this.loading && !this.rows.length;
  }

  /* ===================== APOIO ===================== */

  /** Extrai a mensagem do backend: { error } do Result ou { errors } das Data Annotations. */
  private mensagemErro(err: unknown, fallback: string): string {
    const body = (err as HttpErrorResponse)?.error;

    if (typeof body === 'string' && body.trim()) return body;
    if (body?.error) return body.error;

    if (body?.errors) {
      const primeira = Object.values(body.errors as Record<string, string[]>)
        .flat()
        .find(mensagem => !!mensagem);
      if (primeira) return primeira;
    }

    return fallback;
  }
}
