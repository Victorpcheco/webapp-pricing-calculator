import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import {
  AtividadeRecente,
  DashboardResumo,
  DashboardService,
  DesempenhoProduto,
  TipoAtividade
} from '../../services/dashboard.service';

interface ChartBar {
  nome: string;
  custoLabel: string;
  lucroLabel: string;
  vendaLabel: string;
  custoTitle: string;
  lucroTitle: string;
  custoWidth: number;
  lucroWidth: number;
}

interface ActivityRow {
  icon: string;
  bg: string;
  color: string;
  title: string;
  desc: string;
  time: string;
}

const ACTIVITY_STYLE: Record<TipoAtividade, { icon: string; bg: string; color: string }> = {
  precificacao: { icon: '↗', bg: 'var(--success-light)', color: 'var(--success)' },
  produto: { icon: '▦', bg: '#f3e8ff', color: '#7e22ce' },
  insumo: { icon: '◇', bg: 'var(--primary-light)', color: 'var(--primary)' }
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, AppShellComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  /** Dados de sessão — hoje mockados, virão do login quando o backend expuser o perfil. */
  readonly userName = 'Marcia Oliveira';
  readonly userCompany = 'Microempreendedora';

  kpiHour = 'R$ 0,00';
  kpiItems = 0;
  kpiRecipes = 0;
  kpiSims = 0;

  chartBars: ChartBar[] = [];
  activities: ActivityRow[] = [];

  get firstName(): string {
    return this.userName.split(' ')[0];
  }

  ngOnInit() {
    this.dashboardService.getResumo().subscribe(resumo => this.applyResumo(resumo));
  }

  private applyResumo(resumo: DashboardResumo) {
    this.kpiHour = this.currency.format(resumo.valorHora);
    this.kpiItems = resumo.totalInsumos;
    this.kpiRecipes = resumo.totalReceitas;
    this.kpiSims = resumo.totalSimulacoes;

    this.chartBars = this.buildChartBars(resumo.desempenhoProdutos);
    this.activities = resumo.atividadesRecentes.map(atividade => this.buildActivity(atividade));
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

      return {
        nome: produto.nome,
        custoWidth,
        lucroWidth,
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
