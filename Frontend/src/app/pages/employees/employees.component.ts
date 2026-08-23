import { CommonModule } from '@angular/common';
import { Component, HostListener, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import {
  CltCharges,
  Employee,
  EmployeeContractType,
  FreelancerFrequency,
  MockStoreService
} from '../../services/mock-store.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — COLABORADORES
 * GET    /api/colaboradores          → lista
 * POST   /api/colaboradores          → cria
 * PUT    /api/colaboradores/{id}     → atualiza
 * DELETE /api/colaboradores/{id}     → remove
 * HEADERS: Authorization: Bearer <token>
 * BODY: { nome, cargo, tipoContratacao, status, admissao, valorBase, frequenciaFreelancer, telefone }
 *
 * Provisão CLT (idêntica ao mockup, sobre o salário bruto):
 *   FGTS      = salário × 8%
 *   13º       = salário ÷ 12
 *   Férias    = salário ÷ 12
 *   1/3 férias= (salário ÷ 12) ÷ 3
 *   custo total mensal = salário + soma dos encargos acima
 * ============================================================
 */
@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.scss'
})
export class EmployeesComponent {
  private readonly store = inject(MockStoreService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;

  search = '';
  typeFilter: 'all' | EmployeeContractType = 'all';

  modalOpen = false;
  editId = '';
  formCode = '';
  formName = '';
  formRole = '';
  formContractType: EmployeeContractType = 'CLT';
  formStatus: 'Ativo' | 'Inativo' = 'Ativo';
  formAdmissionDate = '';
  formPhone = '';
  formBaseValue = '';
  formFreelancerFrequency: FreelancerFrequency = 'Mensal';

  readonly freelancerFrequencyOptions: { value: FreelancerFrequency; label: string }[] = [
    { value: 'Mensal', label: 'Valor fixo mensal' },
    { value: 'Por hora', label: 'Por hora trabalhada' },
    { value: 'Por serviço', label: 'Por serviço entregue' }
  ];

  /* ===================== LISTA ===================== */

  get items(): Employee[] {
    return this.store.employees;
  }

  get filtered(): Employee[] {
    const query = this.search.trim().toLowerCase();
    return this.items.filter(
      item =>
        (!query || item.name.toLowerCase().includes(query) || item.role.toLowerCase().includes(query)) &&
        (this.typeFilter === 'all' || item.contractType === this.typeFilter)
    );
  }

  get visibleCountLabel(): string {
    const total = this.filtered.length;
    return `${total} ${total === 1 ? 'colaborador exibido' : 'colaboradores exibidos'}`;
  }

  get totalItems(): number {
    return this.items.length;
  }

  get cltCount(): number {
    return this.items.filter(item => item.contractType === 'CLT').length;
  }

  get freelancerCount(): number {
    return this.items.filter(item => item.contractType === 'Freelancer').length;
  }

  get payrollValue(): string {
    const total = this.items.reduce((sum, item) => sum + this.monthlyCost(item), 0);
    return this.currency.format(total);
  }

  /* ===================== FORMATAÇÃO ===================== */

  private parseBR(value: string | null | undefined): number {
    if (!value) return 0;
    let normalized = String(value).trim().replace(/R\$\s?/g, '').replace(/\s/g, '');
    if (normalized.includes(',')) normalized = normalized.replace(/\./g, '').replace(',', '.');
    return Math.max(0, Number.parseFloat(normalized) || 0);
  }

  initials(name: string): string {
    return name
      .split(/\s+/)
      .slice(0, 2)
      .map(part => part[0])
      .join('')
      .toUpperCase();
  }

  money(value: number): string {
    return this.currency.format(Number.isFinite(value) ? value : 0);
  }

  monthlyCost(item: Employee): number {
    return this.store.employeeMonthlyCost(item);
  }

  charges(item: Employee): CltCharges {
    return this.store.cltCharges(item.baseValue);
  }

  admissionLabel(item: Employee): string {
    return item.admissionDate ? new Date(item.admissionDate).toLocaleDateString('pt-BR') : '—';
  }

  baseValueLabel(item: Employee): string {
    if (item.contractType === 'CLT') return this.money(item.baseValue);
    const suffix =
      item.freelancerFrequency === 'Por hora' ? '/hora' : item.freelancerFrequency === 'Por serviço' ? '/serviço' : '/mês';
    return `${this.money(item.baseValue)} ${suffix}`;
  }

  itemSubtitle(item: Employee): string {
    return item.code ? `Cód: ${item.code}` : `ID: ${item.id.slice(-6).toUpperCase()}`;
  }

  /* ===================== PRÉVIA DO MODAL ===================== */

  get formBaseValueNumber(): number {
    return this.parseBR(this.formBaseValue);
  }

  get formCharges(): CltCharges {
    return this.store.cltCharges(this.formBaseValueNumber);
  }

  get formMonthlyCost(): number {
    return this.formBaseValueNumber + this.formCharges.total;
  }

  /* ===================== MODAL ===================== */

  get modalTitle(): string {
    return this.editId ? 'Editar colaborador' : 'Cadastrar novo colaborador';
  }

  get baseValueLabelText(): string {
    return this.formContractType === 'CLT' ? 'Salário bruto mensal' : 'Valor combinado';
  }

  openModal(item?: Employee) {
    this.editId = item?.id ?? '';
    this.formCode = item?.code ?? '';
    this.formName = item?.name ?? '';
    this.formRole = item?.role ?? '';
    this.formContractType = item?.contractType ?? 'CLT';
    this.formStatus = item?.status ?? 'Ativo';
    this.formAdmissionDate = item?.admissionDate ? item.admissionDate.slice(0, 10) : '';
    this.formPhone = item?.phone ?? '';
    this.formFreelancerFrequency = item?.freelancerFrequency ?? 'Mensal';
    this.formBaseValue = item
      ? item.baseValue.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
      : '';

    this.modalOpen = true;
    document.body.style.overflow = 'hidden';
    setTimeout(() => document.getElementById('employeeName')?.focus(), 120);
  }

  closeModal() {
    this.modalOpen = false;
    document.body.style.overflow = '';
  }

  onBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) this.closeModal();
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.modalOpen) this.closeModal();
  }

  onBaseValueBlur() {
    const value = this.parseBR(this.formBaseValue);
    this.formBaseValue = value
      ? value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
      : '';
  }

  /* ===================== AÇÕES ===================== */

  onSubmit(event: Event) {
    event.preventDefault();

    const name = this.formName.trim();
    const role = this.formRole.trim();
    const baseValue = this.parseBR(this.formBaseValue);

    if (!name || !role || !baseValue) {
      const targetId = !name ? 'employeeName' : !role ? 'employeeRole' : 'baseValue';
      const target = document.getElementById(targetId) as HTMLInputElement | null;
      target?.focus();
      target?.setCustomValidity('Preencha este campo com um valor válido.');
      target?.reportValidity();
      setTimeout(() => target?.setCustomValidity(''), 100);
      return;
    }

    const item: Employee = {
      id: this.editId || `colab-${Date.now().toString(36)}`,
      code: this.formCode.trim(),
      name,
      role,
      contractType: this.formContractType,
      status: this.formStatus,
      admissionDate: this.formAdmissionDate ? new Date(this.formAdmissionDate).toISOString() : new Date().toISOString(),
      baseValue,
      phone: this.formPhone.trim(),
      ...(this.formContractType === 'Freelancer' ? { freelancerFrequency: this.formFreelancerFrequency } : {})
    };

    if (this.editId) {
      this.store.employees = this.items.map(current => (current.id === this.editId ? item : current));
    } else {
      this.store.employees = [item, ...this.items];
    }

    this.closeModal();
    this.toast.show('Informação atualizada com sucesso!');
  }

  deleteItem(item: Employee) {
    if (!window.confirm(`Excluir "${item.name}" do quadro de colaboradores?`)) return;
    this.store.employees = this.items.filter(current => current.id !== item.id);
    this.toast.show('Colaborador excluído da lista.');
  }

  clearAll() {
    this.search = '';
    this.typeFilter = 'all';
    this.store.employees = [];
    this.toast.show('Dados de colaboradores limpos com sucesso!');
  }
}
