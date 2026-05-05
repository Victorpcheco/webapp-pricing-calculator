import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';

interface OrderMetric {
  label: string;
  value: string;
  detail: string;
  tone: 'blue' | 'green' | 'amber' | 'rose';
}

interface Order {
  id: string;
  customer: string;
  items: string;
  channel: string;
  due: string;
  value: number;
  payment: string;
  status: 'Novo' | 'Produção' | 'Pronto' | 'Entregue';
  tone: 'good' | 'warning' | 'danger';
}

interface ProductionStep {
  title: string;
  count: number;
  detail: string;
  percent: number;
  tone: 'green' | 'amber' | 'rose';
}

@Component({
  selector: 'app-orders',
  imports: [CommonModule, AppShellComponent],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss'
})
export class OrdersComponent {
  readonly statusFilters = ['Todos', 'Novo', 'Produção', 'Pronto', 'Entregue'];
  selectedStatus = 'Todos';

  readonly metrics: OrderMetric[] = [
    { label: 'Pedidos abertos', value: '18', detail: 'R$ 2.740 em carteira', tone: 'blue' },
    { label: 'Entregas hoje', value: '7', detail: '3 já estão prontas', tone: 'amber' },
    { label: 'Pagos', value: '82%', detail: '+6 pts no mês', tone: 'green' },
    { label: 'Atraso possível', value: '2', detail: 'Produção apertada', tone: 'rose' }
  ];

  readonly orders: Order[] = [
    {
      id: '#1048',
      customer: 'Ana Paula',
      items: '32 bolo no pote chocolate',
      channel: 'WhatsApp',
      due: 'Hoje, 16h',
      value: 448,
      payment: 'Pix pago',
      status: 'Produção',
      tone: 'warning'
    },
    {
      id: '#1049',
      customer: 'Mercadinho Sol',
      items: '120 brigadeiros gourmet',
      channel: 'Feira',
      due: 'Amanhã, 9h',
      value: 420,
      payment: '50% sinal',
      status: 'Novo',
      tone: 'danger'
    },
    {
      id: '#1050',
      customer: 'Camila Rocha',
      items: '48 brownies recheados',
      channel: 'Instagram',
      due: 'Amanhã, 14h',
      value: 456,
      payment: 'Pix pago',
      status: 'Novo',
      tone: 'danger'
    },
    {
      id: '#1051',
      customer: 'Lucas Martins',
      items: '2 cento de salgados',
      channel: 'WhatsApp',
      due: 'Hoje, 18h',
      value: 192,
      payment: 'Na entrega',
      status: 'Pronto',
      tone: 'good'
    },
    {
      id: '#1044',
      customer: 'Beatriz Lima',
      items: '24 mini cheesecakes',
      channel: 'Instagram',
      due: 'Ontem',
      value: 192,
      payment: 'Pix pago',
      status: 'Entregue',
      tone: 'good'
    }
  ];

  readonly productionSteps: ProductionStep[] = [
    { title: 'Separar insumos', count: 5, detail: '2 com estoque baixo', percent: 55, tone: 'amber' },
    { title: 'Produzir massas', count: 3, detail: '4h estimadas', percent: 42, tone: 'rose' },
    { title: 'Embalar pedidos', count: 7, detail: 'Etiquetas prontas', percent: 78, tone: 'green' }
  ];

  get filteredOrders(): Order[] {
    return this.orders.filter((order) => this.selectedStatus === 'Todos' || order.status === this.selectedStatus);
  }

  selectStatus(status: string): void {
    this.selectedStatus = status;
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

  trackById(_index: number, item: { id: string }): string {
    return item.id;
  }

  trackByStatus(_index: number, status: string): string {
    return status;
  }

  trackByTitle(_index: number, item: { title: string }): string {
    return item.title;
  }
}
