import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { CostHistoryItem, CustosApiService, SalvarCustoCommand } from '../../services/custos-api.service';

@Component({
  selector: 'app-costs',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent, ConfirmDialogComponent],
  templateUrl: './costs.component.html',
  styleUrl: './costs.component.scss'
})
export class CostsComponent implements OnInit {
  private readonly api = inject(CustosApiService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;
  @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;

  loading = false;
  editId = '';

  salary = '';
  hours: number | null = null;
  energy = '';
  energyPercent: number | null = null;
  gas = '';
  gasPercent: number | null = null;
  hasMei = true;
  das = '';
  depreciation: number | null = 5;

  history: CostHistoryItem[] = [];
  search = '';

  ngOnInit() {
    this.carregarHistorico();
  }

  /* ===================== CÁLCULO (client-side para preview em tempo real) ===================== */

  private parseBR(value: string | null | undefined): number {
    if (!value) return 0;
    let normalized = String(value).trim().replace(/R\$\s?/g, '').replace(/\s/g, '');
    if (normalized.includes(',')) normalized = normalized.replace(/\./g, '').replace(',', '.');
    return Math.max(0, Number.parseFloat(normalized) || 0);
  }

  private safePercent(value: number | null): number {
    return Math.min(100, Math.max(0, Number(value) || 0));
  }

  get result() {
    const salary = this.parseBR(this.salary);
    const hours = Math.max(0, Number(this.hours) || 0);
    const energy = this.parseBR(this.energy);
    const gas = this.parseBR(this.gas);
    const energyReal = (energy * this.safePercent(this.energyPercent)) / 100;
    const gasReal = (gas * this.safePercent(this.gasPercent)) / 100;
    const das = this.hasMei ? this.parseBR(this.das) : 0;
    const depreciationRate = this.safePercent(this.depreciation);
    const baseCost = energyReal + gasReal + das + salary;
    const depreciation = (baseCost * depreciationRate) / 100;
    const monthly = baseCost + depreciation;
    const hour = hours > 0 ? monthly / hours : 0;

    return { salary, hours, energy, energyReal, gas, gasReal, das, depreciationRate, depreciation, monthly, hour };
  }

  get isReady(): boolean {
    const { salary, hours } = this.result;
    return salary > 0 && hours > 0;
  }

  get resultMessage(): string {
    return this.isReady
      ? `Com os valores informados, cada hora trabalhada precisa gerar pelo menos ${this.money(this.result.hour)} para cobrir seus custos.`
      : 'Preencha o salário desejado e as horas trabalhadas para descobrir o valor da sua hora.';
  }

  get calculationStatus(): string {
    return this.isReady
      ? `Cálculo atualizado com ${this.result.hours.toLocaleString('pt-BR')} horas mensais.`
      : 'Aguardando os campos obrigatórios.';
  }

  money(value: number): string {
    return this.currency.format(Number.isFinite(value) ? value : 0);
  }

  /* ===================== CAMPOS DE DINHEIRO ===================== */

  onMoneyFocus(field: 'salary' | 'energy' | 'gas' | 'das') {
    const current = this[field];
    if (current) this[field] = this.parseBR(current).toString().replace('.', ',');
  }

  onMoneyBlur(field: 'salary' | 'energy' | 'gas' | 'das') {
    const value = this.parseBR(this[field]);
    this[field] = value ? value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '';
  }

  /* ===================== HISTÓRICO ===================== */

  get filteredHistory(): CostHistoryItem[] {
    const search = this.search.toLowerCase().trim();
    return this.history.filter(item => {
      const description = (item.description || 'Configuração').toLowerCase();
      const dateStr = item.createdAt ? new Date(item.createdAt).toLocaleDateString('pt-BR') : '';
      return description.includes(search) || dateStr.includes(search);
    });
  }

  get visibleCountLabel(): string {
    const total = this.filteredHistory.length;
    return `${total} ${total === 1 ? 'configuração salva' : 'configurações salvas'}`;
  }

  formatDate(iso: string): string {
    return iso ? new Date(iso).toLocaleDateString('pt-BR') : 'Data recente';
  }

  hoursLabel(hours: number): string {
    return `${Number(hours || 0).toLocaleString('pt-BR')}h`;
  }

  /* ===================== AÇÕES ===================== */

  private carregarHistorico() {
    this.loading = true;
    this.api.listar().subscribe({
      next: items => {
        this.history = items;
        if (items.length > 0 && !this.editId) {
          this.preencherFormulario(items[0]);
        }
        this.loading = false;
      },
      error: () => {
        this.toast.show('Erro ao carregar o histórico de custos.');
        this.loading = false;
      }
    });
  }

  onSubmit(event: Event) {
    event.preventDefault();
    const r = this.result;

    if (r.salary <= 0 || r.hours <= 0) {
      const target = document.getElementById(r.salary <= 0 ? 'salary' : 'hours') as HTMLInputElement | null;
      target?.focus();
      target?.setCustomValidity('Preencha este campo com um valor maior que zero.');
      target?.reportValidity();
      setTimeout(() => target?.setCustomValidity(''), 100);
      return;
    }

    const command: SalvarCustoCommand = {
      salary: r.salary,
      hours: r.hours,
      energy: this.parseBR(this.energy),
      energyPercent: this.safePercent(this.energyPercent),
      gas: this.parseBR(this.gas),
      gasPercent: this.safePercent(this.gasPercent),
      hasMei: this.hasMei,
      das: this.hasMei ? this.parseBR(this.das) : 0,
      depreciationRate: this.safePercent(this.depreciation)
    };

    this.loading = true;

    if (this.editId) {
      this.api.atualizar(this.editId, command).subscribe({
        next: updated => this.onSalvoComSucesso(updated),
        error: () => {
          this.toast.show('Erro ao atualizar configuração.');
          this.loading = false;
        }
      });
    } else {
      this.api.criar(command).subscribe({
        next: criado => this.onSalvoComSucesso(criado),
        error: () => {
          this.toast.show('Erro ao salvar configuração.');
          this.loading = false;
        }
      });
    }
  }

  private onSalvoComSucesso(item: CostHistoryItem) {
    if (this.editId) {
      this.history = this.history.map(h => h.id === item.id ? item : h);
    } else {
      this.history = [item, ...this.history];
    }
    this.editId = '';
    this.loading = false;
    this.toast.show('Informação atualizada com sucesso!');
  }

  editItem(item: CostHistoryItem) {
    this.editId = item.id;
    this.preencherFormulario(item);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    document.getElementById('salary')?.focus();
  }

  async deleteItem(item: CostHistoryItem) {
    const confirmed = await this.confirmDialog.open({
      title: 'Excluir configuração',
      message: 'Tem certeza que deseja excluir esta configuração? Esta ação não pode ser desfeita.',
      confirmLabel: 'Excluir',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.excluir(item.id).subscribe({
      next: () => {
        this.history = this.history.filter(h => h.id !== item.id);
        if (this.editId === item.id) this.resetForm();
        this.toast.show('Configuração excluída com sucesso!');
      },
      error: () => this.toast.show('Erro ao excluir configuração.')
    });
  }

  async clearHistory() {
    const confirmed = await this.confirmDialog.open({
      title: 'Limpar histórico',
      message: 'Todos os custos salvos serão removidos permanentemente. Deseja continuar?',
      confirmLabel: 'Limpar tudo',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.limparHistorico().subscribe({
      next: () => {
        this.history = [];
        this.resetForm();
        this.hasMei = false;
        this.toast.show('Dados de custos limpos com sucesso!');
      },
      error: () => this.toast.show('Erro ao limpar o histórico.')
    });
  }

  clearForm() {
    this.resetForm();
    this.hasMei = false;
    this.toast.show('Formulário limpo.');
  }

  private preencherFormulario(item: CostHistoryItem) {
    this.salary = item.salary ? item.salary.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.hours = item.hours || null;
    this.energy = item.energy ? item.energy.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.energyPercent = item.energyPercent || null;
    this.gas = item.gas ? item.gas.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.gasPercent = item.gasPercent || null;
    this.hasMei = item.hasMei !== false;
    this.das = item.das ? item.das.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.depreciation = item.depreciationRate ?? 5;
  }

  private resetForm() {
    this.editId = '';
    this.salary = '';
    this.hours = null;
    this.energy = '';
    this.energyPercent = null;
    this.gas = '';
    this.gasPercent = null;
    this.das = '';
    this.depreciation = null;
  }
}
