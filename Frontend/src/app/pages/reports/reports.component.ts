import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';

interface ReportMetric {
  label: string;
  value: string;
  detail: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
}

interface ChartItem {
  label: string;
  value: string;
  percent: number;
  tone: 'green' | 'amber' | 'rose';
}

interface TopProduct {
  name: string;
  revenue: string;
  margin: string;
}

interface CashLine {
  label: string;
  incoming: string;
  outgoing: string;
  balance: string;
}

@Component({
  selector: 'app-reports',
  imports: [CommonModule, AppShellComponent],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent {
  readonly periods = ['7 dias', '30 dias', '90 dias'];
  selectedPeriod = '30 dias';

  readonly metricsByPeriod: Record<string, ReportMetric[]> = {
    '7 dias': [
      { label: 'Receita', value: 'R$ 2.180', detail: '+11% vs. período anterior', tone: 'blue' },
      { label: 'Lucro bruto', value: 'R$ 820', detail: '37,6% de margem', tone: 'green' },
      { label: 'Custos', value: 'R$ 1.360', detail: 'Ingredientes lideram', tone: 'amber' },
      { label: 'Perdas', value: 'R$ 96', detail: '4,4% da receita', tone: 'rose' }
    ],
    '30 dias': [
      { label: 'Receita', value: 'R$ 8.420', detail: '+18% vs. mês anterior', tone: 'blue' },
      { label: 'Lucro bruto', value: 'R$ 3.180', detail: '37,8% de margem', tone: 'green' },
      { label: 'Custos', value: 'R$ 5.240', detail: '62,2% da receita', tone: 'amber' },
      { label: 'Perdas', value: 'R$ 310', detail: '3,7% da receita', tone: 'rose' }
    ],
    '90 dias': [
      { label: 'Receita', value: 'R$ 23.900', detail: '+24% no trimestre', tone: 'blue' },
      { label: 'Lucro bruto', value: 'R$ 9.240', detail: '38,7% de margem', tone: 'green' },
      { label: 'Custos', value: 'R$ 14.660', detail: 'Compra média estável', tone: 'amber' },
      { label: 'Perdas', value: 'R$ 870', detail: '3,6% da receita', tone: 'rose' }
    ]
  };

  readonly costChart: ChartItem[] = [
    { label: 'Ingredientes', value: 'R$ 2.940', percent: 56, tone: 'rose' },
    { label: 'Embalagens', value: 'R$ 860', percent: 16, tone: 'amber' },
    { label: 'Taxas e entrega', value: 'R$ 740', percent: 14, tone: 'amber' },
    { label: 'Energia e gás', value: 'R$ 420', percent: 8, tone: 'green' },
    { label: 'Perdas', value: 'R$ 310', percent: 6, tone: 'rose' }
  ];

  readonly topProducts: TopProduct[] = [
    { name: 'Bolo no pote chocolate', revenue: 'R$ 1.204', margin: '58%' },
    { name: 'Brigadeiro gourmet', revenue: 'R$ 770', margin: '59%' },
    { name: 'Brownie recheado', revenue: 'R$ 703', margin: '48%' },
    { name: 'Mini cheesecake', revenue: 'R$ 512', margin: '60%' }
  ];

  readonly channelChart: ChartItem[] = [
    { label: 'Instagram', value: 'R$ 3.280', percent: 39, tone: 'green' },
    { label: 'WhatsApp', value: 'R$ 2.740', percent: 33, tone: 'green' },
    { label: 'Feiras', value: 'R$ 1.520', percent: 18, tone: 'amber' },
    { label: 'Apps', value: 'R$ 880', percent: 10, tone: 'rose' }
  ];

  readonly cashFlow: CashLine[] = [
    { label: 'Semana 1', incoming: 'R$ 1.920', outgoing: 'R$ 1.160', balance: 'R$ 760' },
    { label: 'Semana 2', incoming: 'R$ 2.140', outgoing: 'R$ 1.420', balance: 'R$ 720' },
    { label: 'Semana 3', incoming: 'R$ 1.870', outgoing: 'R$ 1.080', balance: 'R$ 790' },
    { label: 'Semana 4', incoming: 'R$ 2.490', outgoing: 'R$ 1.580', balance: 'R$ 910' }
  ];

  get metrics(): ReportMetric[] {
    return this.metricsByPeriod[this.selectedPeriod];
  }

  selectPeriod(period: string): void {
    this.selectedPeriod = period;
  }

  trackByLabel(_index: number, item: { label: string }): string {
    return item.label;
  }

  trackByName(_index: number, item: { name: string }): string {
    return item.name;
  }

  trackByPeriod(_index: number, period: string): string {
    return period;
  }
}
