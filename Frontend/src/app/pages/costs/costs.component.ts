import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { CostHistoryItem, CostResult, MockStoreService } from '../../services/mock-store.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — CUSTOS OPERACIONAIS / VALOR DA HORA
 * GET    /api/custos            → lista o histórico
 * POST   /api/custos            → cria uma configuração
 * PUT    /api/custos/{id}       → atualiza
 * DELETE /api/custos/{id}       → remove
 * HEADERS: Authorization: Bearer <token>
 *
 * Fórmula aplicada (idêntica ao mockup):
 *   base   = energia×% + gás×% + DAS + salário
 *   mensal = base + (base × depreciação%)
 *   hora   = mensal ÷ horas trabalhadas
 * ============================================================
 */
@Component({
  selector: 'app-costs',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent],
  templateUrl: './costs.component.html',
  styleUrl: './costs.component.scss'
})
export class CostsComponent implements OnInit {
  private readonly store = inject(MockStoreService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;

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

  search = '';

  ngOnInit() {
    this.restoreSavedData();
  }

  /* ===================== CÁLCULO ===================== */

  private parseBR(value: string | null | undefined): number {
    if (!value) return 0;
    let normalized = String(value).trim().replace(/R\$\s?/g, '').replace(/\s/g, '');
    if (normalized.includes(',')) normalized = normalized.replace(/\./g, '').replace(',', '.');
    return Math.max(0, Number.parseFloat(normalized) || 0);
  }

  private safePercent(value: number | null): number {
    return Math.min(100, Math.max(0, Number(value) || 0));
  }

  get result(): CostResult {
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

    return {
      salary,
      hours,
      energy,
      energyPercent: this.safePercent(this.energyPercent),
      gas,
      gasPercent: this.safePercent(this.gasPercent),
      hasMei: this.hasMei,
      das,
      depreciationRate,
      energyReal,
      gasReal,
      depreciation,
      monthly,
      hour
    };
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

  get history(): CostHistoryItem[] {
    return this.store.costHistory;
  }

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

  onSubmit(event: Event) {
    event.preventDefault();
    const result = this.result;

    if (result.salary <= 0 || result.hours <= 0) {
      const target = document.getElementById(result.salary <= 0 ? 'salary' : 'hours') as HTMLInputElement | null;
      target?.focus();
      target?.setCustomValidity('Preencha este campo com um valor maior que zero.');
      target?.reportValidity();
      setTimeout(() => target?.setCustomValidity(''), 100);
      return;
    }

    this.store.costSettings = result;

    const dateStr = new Date().toLocaleDateString('pt-BR');
    const existing = this.history.find(item => item.id === this.editId);
    const costItem: CostHistoryItem = {
      id: this.editId || `cost_${Date.now()}`,
      description: this.editId ? existing?.description || `Cálculo de ${dateStr}` : `Cálculo de ${dateStr}`,
      createdAt: new Date().toISOString(),
      ...result
    };

    if (this.editId) {
      this.store.costHistory = this.history.map(item => (item.id === this.editId ? costItem : item));
    } else {
      this.store.costHistory = [costItem, ...this.history];
    }

    this.editId = '';
    this.toast.show('Informação atualizada com sucesso!');
  }

  editItem(item: CostHistoryItem) {
    this.editId = item.id;
    this.salary = item.salary ? item.salary.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.hours = item.hours || null;
    this.energy = item.energy ? item.energy.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.energyPercent = item.energyPercent || null;
    this.gas = item.gas ? item.gas.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.gasPercent = item.gasPercent || null;
    this.hasMei = item.hasMei !== false;
    this.das = item.das ? item.das.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.depreciation = item.depreciationRate ?? 5;

    window.scrollTo({ top: 0, behavior: 'smooth' });
    document.getElementById('salary')?.focus();
  }

  deleteItem(item: CostHistoryItem) {
    if (!window.confirm('Deseja realmente excluir esta configuração de custo?')) return;

    this.store.costHistory = this.history.filter(current => current.id !== item.id);
    if (this.store.costHistory.length > 0) {
      this.store.costSettings = this.store.costHistory[0];
    }
    this.toast.show('Configuração excluída com sucesso!');
  }

  clearHistory() {
    if (!window.confirm('Deseja limpar todo o histórico de custos salvos?')) return;

    this.store.costHistory = [];
    this.resetForm();
    this.hasMei = false;
    this.toast.show('Dados de custos limpos com sucesso!');
  }

  clearForm() {
    this.resetForm();
    this.hasMei = false;
    this.toast.show('Dados de custos limpos com sucesso!');
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

  private restoreSavedData() {
    const saved = this.store.costSettings;
    if (!saved) return;

    this.salary = saved.salary ? saved.salary.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.hours = saved.hours || null;
    this.energy = saved.energy ? saved.energy.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.energyPercent = saved.energyPercent || null;
    this.gas = saved.gas ? saved.gas.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.gasPercent = saved.gasPercent || null;
    this.hasMei = saved.hasMei !== false;
    this.das = saved.das ? saved.das.toLocaleString('pt-BR', { minimumFractionDigits: 2 }) : '';
    this.depreciation = saved.depreciationRate ?? 5;
  }
}
