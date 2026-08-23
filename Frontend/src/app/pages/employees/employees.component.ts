import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import {
  ColaboradoresApiService,
  ContractType,
  EmployeeCharges,
  EmployeeItem,
  EmployeeStatus,
  FreelancerFrequency,
  SalvarColaboradorCommand
} from '../../services/colaboradores-api.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — COLABORADORES
 * GET    /api/colaboradores          → lista + totais dos cards
 * POST   /api/colaboradores          → cria
 * PUT    /api/colaboradores/{id}     → atualiza
 * DELETE /api/colaboradores/{id}     → remove
 * DELETE /api/colaboradores          → limpa todos
 * HEADERS: Authorization: Bearer <token> (authInterceptor)
 *
 * O backend provisiona os encargos CLT (FGTS, 13º, férias + 1/3) e devolve
 * `charges` e `monthlyCost` prontos em cada item. O cálculo repetido aqui
 * serve só para a prévia em tempo real enquanto o usuário digita o salário.
 * ============================================================
 */
@Component({
  selector: 'app-employees',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent, ConfirmDialogComponent],
  templateUrl: './employees.component.html',
  styleUrl: './employees.component.scss'
})
export class EmployeesComponent implements OnInit {
  private readonly api = inject(ColaboradoresApiService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  /** Percentuais legais aproximados usados na provisão mensal do custo CLT — mesma fórmula do backend. */
  private readonly cltRates = {
    fgts: 0.08,
    decimoTerceiro: 1 / 12,
    ferias: 1 / 12,
    umTercoFerias: 1 / 36
  };

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;
  @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;

  loading = false;
  saving = false;

  items: EmployeeItem[] = [];

  search = '';
  typeFilter: 'all' | ContractType = 'all';

  modalOpen = false;
  editId = '';
  formCode = '';
  formName = '';
  formRole = '';
  formContractType: ContractType = 'CLT';
  formStatus: EmployeeStatus = 'Ativo';
  formAdmissionDate = '';
  formPhone = '';
  formBaseValue = '';
  formFreelancerFrequency: FreelancerFrequency = 'Mensal';

  readonly freelancerFrequencyOptions: { value: FreelancerFrequency; label: string }[] = [
    { value: 'Mensal', label: 'Valor fixo mensal' },
    { value: 'Por hora', label: 'Por hora trabalhada' },
    { value: 'Por serviço', label: 'Por serviço entregue' }
  ];

  ngOnInit() {
    this.carregarColaboradores();
  }

  /* ===================== LISTA ===================== */

  /**
   * Filtro local: a lista completa já veio do backend, então filtrar aqui
   * mantém a busca instantânea e evita uma requisição por tecla digitada.
   */
  get filtered(): EmployeeItem[] {
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

  /** Soma o custo que o backend já calculou por colaborador — sem repetir a regra CLT aqui. */
  get payrollValue(): string {
    const total = this.items.reduce((sum, item) => sum + Number(item.monthlyCost || 0), 0);
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

  // Os dois valores abaixo chegam calculados do backend
  monthlyCost(item: EmployeeItem): number {
    return item.monthlyCost;
  }

  charges(item: EmployeeItem): EmployeeCharges {
    return item.charges;
  }

  /**
   * Admissão é data de calendário, não instante: o backend grava meia-noite UTC,
   * então formatar em UTC evita exibir o dia anterior em fusos negativos.
   */
  admissionLabel(item: EmployeeItem): string {
    return item.admissionDate
      ? new Date(item.admissionDate).toLocaleDateString('pt-BR', { timeZone: 'UTC' })
      : '—';
  }

  baseValueLabel(item: EmployeeItem): string {
    if (item.contractType === 'CLT') return this.money(item.baseValue);
    const suffix =
      item.freelancerFrequency === 'Por hora' ? '/hora' : item.freelancerFrequency === 'Por serviço' ? '/serviço' : '/mês';
    return `${this.money(item.baseValue)} ${suffix}`;
  }

  itemSubtitle(item: EmployeeItem): string {
    return item.code ? `Cód: ${item.code}` : `ID: ${item.id.slice(-6).toUpperCase()}`;
  }

  /* ===================== PRÉVIA DO MODAL ===================== */

  get formBaseValueNumber(): number {
    return this.parseBR(this.formBaseValue);
  }

  /** Provisão mensal de FGTS, 13º e férias (+1/3) sobre o salário bruto — mesma fórmula do backend. */
  get formCharges(): EmployeeCharges {
    const base = Math.max(0, this.formBaseValueNumber);
    const fgts = base * this.cltRates.fgts;
    const decimoTerceiro = base * this.cltRates.decimoTerceiro;
    const ferias = base * this.cltRates.ferias;
    const umTercoFerias = base * this.cltRates.umTercoFerias;
    return { fgts, decimoTerceiro, ferias, umTercoFerias, total: fgts + decimoTerceiro + ferias + umTercoFerias };
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

  openModal(item?: EmployeeItem) {
    this.editId = item?.id ?? '';
    this.formCode = item?.code ?? '';
    this.formName = item?.name ?? '';
    this.formRole = item?.role ?? '';
    this.formContractType = item?.contractType ?? 'CLT';
    this.formStatus = item?.status ?? 'Ativo';
    // slice(0,10) lê a parte de data do ISO em UTC, pareando com admissionLabel
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

  private carregarColaboradores() {
    this.loading = true;
    this.api.listar().subscribe({
      next: response => {
        this.items = response.data;
        this.loading = false;
      },
      error: err => {
        this.toast.show(this.mensagemErro(err, 'Erro ao carregar os colaboradores.'));
        this.loading = false;
      }
    });
  }

  onSubmit(event: Event) {
    event.preventDefault();
    if (this.saving) return;

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

    const code = this.formCode.trim();
    const phone = this.formPhone.trim();

    const command: SalvarColaboradorCommand = {
      code: code || null,
      name,
      role,
      contractType: this.formContractType,
      status: this.formStatus,
      // Data pura vira meia-noite UTC: sem o sufixo Z o fuso local deslocaria o dia
      admissionDate: this.formAdmissionDate ? `${this.formAdmissionDate}T00:00:00Z` : null,
      baseValue,
      freelancerFrequency: this.formContractType === 'Freelancer' ? this.formFreelancerFrequency : null,
      phone: phone || null
    };

    this.saving = true;

    if (this.editId) {
      this.api.atualizar(this.editId, command).subscribe({
        next: atualizado => this.onSalvoComSucesso(atualizado),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao atualizar o colaborador.'));
          this.saving = false;
        }
      });
    } else {
      this.api.criar(command).subscribe({
        next: criado => this.onSalvoComSucesso(criado),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao cadastrar o colaborador.'));
          this.saving = false;
        }
      });
    }
  }

  private onSalvoComSucesso(item: EmployeeItem) {
    this.items = this.editId
      ? this.items.map(current => (current.id === item.id ? item : current))
      : [item, ...this.items];

    this.saving = false;
    this.closeModal();
    this.toast.show('Informação atualizada com sucesso!');
  }

  async deleteItem(item: EmployeeItem) {
    const confirmed = await this.confirmDialog.open({
      title: 'Excluir colaborador',
      message: `Excluir "${item.name}" do quadro de colaboradores? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.excluir(item.id).subscribe({
      next: () => {
        this.items = this.items.filter(current => current.id !== item.id);
        if (this.editId === item.id) this.closeModal();
        this.toast.show('Colaborador excluído da lista.');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao excluir o colaborador.'))
    });
  }

  async clearAll() {
    const confirmed = await this.confirmDialog.open({
      title: 'Limpar dados',
      message: 'Todos os colaboradores cadastrados serão removidos permanentemente. Deseja continuar?',
      confirmLabel: 'Limpar tudo',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.limparTudo().subscribe({
      next: () => {
        this.items = [];
        this.search = '';
        this.typeFilter = 'all';
        this.toast.show('Dados de colaboradores limpos com sucesso!');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao limpar os colaboradores.'))
    });
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
