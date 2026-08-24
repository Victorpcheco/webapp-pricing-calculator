import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { AuthService } from '../../services/auth.service';
import { SpotlightDirective } from '../../shared/directives/spotlight.directive';
import {
  AtividadeRecente,
  DashboardResumo,
  DashboardService,
  DesempenhoProduto,
  TipoAtividade
} from '../../services/dashboard.service';

type ActivityIcon = 'pricing' | 'products' | 'supplies';
export type ChartView = 'bars' | 'columns';

interface ChartBar {
  nome: string;
  custoLabel: string;
  lucroLabel: string;
  vendaLabel: string;
  custoTitle: string;
  lucroTitle: string;
  custoWidth: number;
  /** Lucro empilhado sobre o custo (capado ao espaço restante) — usado na visão em barras. */
  lucroWidth: number;
  /** Lucro como % isolada do maior valor da série — usado nas visões em colunas e linha. */
  lucroValueWidth: number;
}

interface ActivityRow {
  icon: ActivityIcon;
  bg: string;
  color: string;
  title: string;
  desc: string;
  time: string;
}

const ACTIVITY_STYLE: Record<TipoAtividade, { icon: ActivityIcon; bg: string; color: string }> = {
  precificacao: { icon: 'pricing', bg: 'var(--success-light)', color: 'var(--success)' },
  produto: { icon: 'products', bg: '#f3e8ff', color: '#7e22ce' },
  insumo: { icon: 'supplies', bg: 'var(--primary-light)', color: 'var(--primary)' }
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, AppShellComponent, WorkspaceToastComponent, SpotlightDirective],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly authService = inject(AuthService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;

  loading = false;

  kpiHour = 'R$ 0,00';
  kpiItems = 0;
  kpiRecipes = 0;
  kpiSims = 0;

  /** Média ponderada de margem (lucro / preço) dos produtos com preço definido. */
  avgMarginPercent = 0;

  chartBars: ChartBar[] = [];
  activities: ActivityRow[] = [];

  /** Estilo de visualização escolhido pelo usuário para o card de desempenho. */
  chartView: ChartView = 'bars';

  setChartView(view: ChartView) {
    this.chartView = view;
  }

  get firstName(): string {
    const name = this.authService.getUserName();
    return name ? name.split(' ')[0] : '';
  }

  get greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Bom dia';
    if (hour < 18) return 'Boa tarde';
    return 'Boa noite';
  }

  ngOnInit() {
    this.loading = true;
    this.dashboardService.getResumo().subscribe({
      next: resumo => {
        this.applyResumo(resumo);
        this.loading = false;
      },
      error: err => {
        this.toast.show(this.mensagemErro(err, 'Erro ao carregar o painel.'));
        this.loading = false;
      }
    });
  }

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

  private applyResumo(resumo: DashboardResumo) {
    this.kpiHour = this.currency.format(resumo.valorHora);
    this.kpiItems = resumo.totalInsumos;
    this.kpiRecipes = resumo.totalReceitas;
    this.kpiSims = resumo.totalSimulacoes;

    this.chartBars = this.buildChartBars(resumo.desempenhoProdutos);
    this.activities = resumo.atividadesRecentes.map(atividade => this.buildActivity(atividade));
    this.avgMarginPercent = this.buildAvgMargin(resumo.desempenhoProdutos);
  }

  /** Margem média real: soma do lucro sobre a soma do preço de todos os produtos com preço > 0. */
  private buildAvgMargin(produtos: DesempenhoProduto[]): number {
    const comPreco = produtos.filter(produto => produto.preco > 0);
    if (!comPreco.length) {
      return 0;
    }

    const totalPreco = comPreco.reduce((soma, produto) => soma + produto.preco, 0);
    const totalLucro = comPreco.reduce((soma, produto) => soma + Math.max(0, produto.preco - produto.custo), 0);

    return totalPreco > 0 ? Math.round((totalLucro / totalPreco) * 100) : 0;
  }

  private buildChartBars(produtos: DesempenhoProduto[]): ChartBar[] {
    if (!produtos.length) {
      return [];
    }

    const maxVal = Math.max(...produtos.map(p => p.preco || p.custo || 1), 1);

    return produtos.slice(0, 5).map(produto => {
      const lucro = Math.max(0, produto.preco - produto.custo);
      const custoWidth = Math.min(100, Math.round((produto.custo / maxVal) * 100));
      const lucroWidth = Math.min(100 - custoWidth, Math.round((lucro / maxVal) * 100));
      const lucroValueWidth = Math.min(100, Math.round((lucro / maxVal) * 100));

      return {
        nome: produto.nome,
        custoWidth,
        lucroWidth,
        lucroValueWidth,
        custoLabel: custoWidth > 20 ? this.currency.format(produto.custo) : '',
        lucroLabel: lucroWidth > 20 ? `+${this.currency.format(lucro)}` : '',
        vendaLabel: this.currency.format(produto.preco),
        custoTitle: `Custo: ${this.currency.format(produto.custo)}`,
        lucroTitle: `Lucro: ${this.currency.format(lucro)}`
      };
    });
  }

  private buildActivity(atividade: AtividadeRecente): ActivityRow {
    const style = ACTIVITY_STYLE[atividade.tipo] ?? ACTIVITY_STYLE.produto;

    return {
      ...style,
      title: atividade.titulo,
      desc: atividade.descricao,
      time: this.relativeTime(atividade.data)
    };
  }

  private relativeTime(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const diffHours = (Date.now() - date.getTime()) / 36e5;
    if (diffHours < 1) {
      return 'Recente';
    }

    const startOfToday = new Date();
    startOfToday.setHours(0, 0, 0, 0);
    const diffDays = Math.floor((startOfToday.getTime() - date.getTime()) / 864e5) + 1;

    if (diffDays <= 0) {
      return 'Hoje';
    }
    if (diffDays === 1) {
      return 'Ontem';
    }
    return `Há ${diffDays} dias`;
  }
}
