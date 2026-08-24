import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { AppSelectComponent, AppSelectOption } from '../../shared/components/app-select/app-select.component';
import {
  InsumoItem,
  InsumoTipo,
  InsumoUnidade,
  InsumosApiService,
  SalvarInsumoCommand
} from '../../services/insumos-api.service';

/** Fatores de conversão para a unidade base (g / ml / un) — mesma tabela do backend. */
const UNIT_FACTOR: Record<InsumoUnidade, number> = { kg: 1000, g: 1, L: 1000, ml: 1, un: 1 };
const BASE_UNIT: Record<InsumoUnidade, string> = { kg: 'g', g: 'g', L: 'ml', ml: 'ml', un: 'un' };

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — INSUMOS
 * GET    /api/insumos           → lista + totais dos cards
 * POST   /api/insumos           → cria
 * PUT    /api/insumos/{id}      → atualiza
 * DELETE /api/insumos/{id}      → remove
 * DELETE /api/insumos           → limpa todos
 * HEADERS: Authorization: Bearer <token> (authInterceptor)
 * O backend calcula unitCost = price ÷ quantidade convertida
 * para a unidade base (g, ml ou un).
 * ============================================================
 */
@Component({
  selector: 'app-supplies',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent, ConfirmDialogComponent, AppSelectComponent],
  templateUrl: './supplies.component.html',
  styleUrl: './supplies.component.scss'
})
export class SuppliesComponent implements OnInit {
  private readonly api = inject(InsumosApiService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  private readonly numberBR = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;
  @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;

  loading = false;
  saving = false;

  items: InsumoItem[] = [];

  search = '';
  typeFilter: 'all' | InsumoTipo = 'all';

  modalOpen = false;
  editId = '';
  formType: InsumoTipo = 'Ingrediente';
  formName = '';
  formQuantity: number | null = null;
  formUnit: InsumoUnidade = 'kg';
  formPrice = '';

  readonly unitOptions: { value: InsumoUnidade; label: string }[] = [
    { value: 'kg', label: 'Quilograma (kg)' },
    { value: 'g', label: 'Grama (g)' },
    { value: 'L', label: 'Litro (L)' },
    { value: 'ml', label: 'Mililitro (ml)' },
    { value: 'un', label: 'Unidade (un)' }
  ];

  readonly typeFilterOptions: AppSelectOption[] = [
    { value: 'all', label: 'Todos os tipos' },
    { value: 'Ingrediente', label: 'Ingredientes' },
    { value: 'Embalagem', label: 'Embalagens' }
  ];

  ngOnInit() {
    this.carregarInsumos();
  }

  /* ===================== LISTA ===================== */

  /**
   * Filtro local: a lista completa já veio do backend, então filtrar aqui
   * mantém a busca instantânea e evita uma requisição por tecla digitada.
   */
  get filtered(): InsumoItem[] {
    const query = this.search.trim().toLowerCase();
    return this.items.filter(
      item =>
        (!query || item.name.toLowerCase().includes(query)) &&
        (this.typeFilter === 'all' || item.type === this.typeFilter)
    );
  }

  get visibleCountLabel(): string {
    const total = this.filtered.length;
    return `${total} ${total === 1 ? 'item exibido' : 'itens exibidos'}`;
  }

  get totalItems(): number {
    return this.items.length;
  }

  get ingredientCount(): number {
    return this.items.filter(item => item.type === 'Ingrediente').length;
  }

  get packageCount(): number {
    return this.items.filter(item => item.type === 'Embalagem').length;
  }

  get purchaseValue(): string {
    return this.currency.format(this.items.reduce((sum, item) => sum + Number(item.price || 0), 0));
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

  formatUnit(unit: InsumoUnidade): string {
    return unit === 'un' ? 'unidades' : unit;
  }

  money(value: number): string {
    return this.currency.format(value);
  }

  number(value: number): string {
    return this.numberBR.format(value);
  }

  /** Abaixo de um centavo o mockup mostra até 5 casas para não zerar o custo. */
  formatUnitCost(value: number): string {
    if (value > 0 && value < 0.01) {
      return `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 4, maximumFractionDigits: 5 })}`;
    }
    return this.currency.format(value);
  }

  // Os três valores abaixo chegam calculados do backend
  baseQuantity(item: InsumoItem): number {
    return item.baseQuantity;
  }

  baseUnit(item: InsumoItem): string {
    return item.baseUnit;
  }

  unitCost(item: InsumoItem): number {
    return item.unitCost;
  }

  itemSubtitle(item: InsumoItem): string {
    return `ID: ${item.id.slice(-6).toUpperCase()}`;
  }

  /* ===================== PRÉVIA DO MODAL ===================== */

  /** Prévia em tempo real: calculada no cliente porque o item ainda não foi salvo. */
  private get previewData() {
    const quantity = Math.max(0, Number(this.formQuantity) || 0);
    const price = this.parseBR(this.formPrice);
    const baseQuantity = quantity * (UNIT_FACTOR[this.formUnit] ?? 1);
    return {
      quantity,
      price,
      baseQuantity,
      baseUnit: BASE_UNIT[this.formUnit] ?? this.formUnit,
      unitCost: baseQuantity > 0 ? price / baseQuantity : 0
    };
  }

  get unitCostPreview(): string {
    const { quantity, price, unitCost, baseUnit } = this.previewData;
    if (!quantity || !price) return 'Preencha a quantidade e o preço';
    return `${this.formatUnitCost(unitCost)} por ${baseUnit}`;
  }

  get conversionPreview(): string {
    const { quantity, price, baseQuantity, baseUnit } = this.previewData;
    if (!quantity || !price) return 'A conversão aparecerá aqui.';
    return `${this.number(quantity)} ${this.formatUnit(this.formUnit)} = ${this.number(baseQuantity)} ${baseUnit}.`;
  }

  /* ===================== MODAL ===================== */

  get modalTitle(): string {
    return this.editId ? 'Editar item cadastrado' : 'Cadastrar novo insumo';
  }

  openModal(item?: InsumoItem) {
    this.editId = item?.id ?? '';
    this.formName = item?.name ?? '';
    this.formQuantity = item?.quantity ?? null;
    this.formUnit = item?.unit ?? 'kg';
    this.formType = item?.type ?? 'Ingrediente';
    this.formPrice = item
      ? item.price.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
      : '';

    this.modalOpen = true;
    document.body.style.overflow = 'hidden';
    setTimeout(() => document.getElementById('itemName')?.focus(), 120);
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

  onPriceBlur() {
    const value = this.parseBR(this.formPrice);
    this.formPrice = value
      ? value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
      : '';
  }

  /* ===================== AÇÕES ===================== */

  private carregarInsumos() {
    this.loading = true;
    this.api.listar().subscribe({
      next: response => {
        this.items = response.data;
        this.loading = false;
      },
      error: err => {
        this.toast.show(this.mensagemErro(err, 'Erro ao carregar os insumos.'));
        this.loading = false;
      }
    });
  }

  onSubmit(event: Event) {
    event.preventDefault();
    if (this.saving) return;

    const name = this.formName.trim();
    const quantity = Math.max(0, Number(this.formQuantity) || 0);
    const price = this.parseBR(this.formPrice);

    if (!name || !quantity || !price) {
      const targetId = !name ? 'itemName' : !quantity ? 'quantity' : 'price';
      const target = document.getElementById(targetId) as HTMLInputElement | null;
      target?.focus();
      target?.setCustomValidity('Preencha este campo com um valor válido.');
      target?.reportValidity();
      setTimeout(() => target?.setCustomValidity(''), 100);
      return;
    }

    const command: SalvarInsumoCommand = {
      name,
      type: this.formType,
      quantity,
      unit: this.formUnit,
      price
    };

    this.saving = true;

    if (this.editId) {
      this.api.atualizar(this.editId, command).subscribe({
        next: atualizado => this.onSalvoComSucesso(atualizado),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao atualizar o insumo.'));
          this.saving = false;
        }
      });
    } else {
      this.api.criar(command).subscribe({
        next: criado => this.onSalvoComSucesso(criado),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao cadastrar o insumo.'));
          this.saving = false;
        }
      });
    }
  }

  private onSalvoComSucesso(item: InsumoItem) {
    this.items = this.editId
      ? this.items.map(current => (current.id === item.id ? item : current))
      : [item, ...this.items];

    this.saving = false;
    this.closeModal();
    this.toast.show('Informação atualizada com sucesso!');
  }

  async deleteItem(item: InsumoItem) {
    const confirmed = await this.confirmDialog.open({
      title: 'Excluir insumo',
      message: `Tem certeza que deseja excluir "${item.name}"? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.excluir(item.id).subscribe({
      next: () => {
        this.items = this.items.filter(current => current.id !== item.id);
        if (this.editId === item.id) this.closeModal();
        this.toast.show('Item excluído da lista.');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao excluir o insumo.'))
    });
  }

  async clearAll() {
    const confirmed = await this.confirmDialog.open({
      title: 'Limpar dados',
      message: 'Todos os insumos cadastrados serão removidos permanentemente. Deseja continuar?',
      confirmLabel: 'Limpar tudo',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.limparTudo().subscribe({
      next: () => {
        this.items = [];
        this.search = '';
        this.typeFilter = 'all';
        this.toast.show('Dados de insumos limpos com sucesso!');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao limpar os insumos.'))
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
