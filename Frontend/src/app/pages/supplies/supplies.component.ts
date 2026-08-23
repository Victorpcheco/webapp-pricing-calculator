import { CommonModule } from '@angular/common';
import { Component, HostListener, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { MockStoreService, Supply, SupplyType, SupplyUnit } from '../../services/mock-store.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — INSUMOS
 * GET    /api/insumos           → lista
 * POST   /api/insumos           → cria
 * PUT    /api/insumos/{id}      → atualiza
 * DELETE /api/insumos/{id}      → remove
 * HEADERS: Authorization: Bearer <token>
 * BODY: { nome, tipo, quantidade, unidade, preco }
 * O backend calcula precoUnitario = preco ÷ quantidade convertida
 * para a unidade base (g, ml ou un).
 * ============================================================
 */
@Component({
  selector: 'app-supplies',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent],
  templateUrl: './supplies.component.html',
  styleUrl: './supplies.component.scss'
})
export class SuppliesComponent {
  private readonly store = inject(MockStoreService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  private readonly numberBR = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;

  search = '';
  typeFilter: 'all' | SupplyType = 'all';

  modalOpen = false;
  editId = '';
  formType: SupplyType = 'Ingrediente';
  formCode = '';
  formName = '';
  formQuantity: number | null = null;
  formUnit: SupplyUnit = 'kg';
  formPrice = '';

  readonly unitOptions: { value: SupplyUnit; label: string }[] = [
    { value: 'kg', label: 'Quilograma (kg)' },
    { value: 'g', label: 'Grama (g)' },
    { value: 'L', label: 'Litro (L)' },
    { value: 'ml', label: 'Mililitro (ml)' },
    { value: 'un', label: 'Unidade (un)' }
  ];

  /* ===================== LISTA ===================== */

  get items(): Supply[] {
    return this.store.supplies;
  }

  get filtered(): Supply[] {
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

  formatUnit(unit: SupplyUnit): string {
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

  baseQuantity(item: Supply): number {
    return item.quantity * this.store.unitFactor(item.unit);
  }

  baseUnit(item: Supply): string {
    return this.store.baseUnit(item.unit);
  }

  unitCost(item: Supply): number {
    return this.store.supplyUnitCost(item);
  }

  itemSubtitle(item: Supply): string {
    return item.code ? `Cód: ${item.code}` : `ID: ${item.id.slice(-6).toUpperCase()}`;
  }

  /* ===================== PRÉVIA DO MODAL ===================== */

  private get previewData() {
    const quantity = Math.max(0, Number(this.formQuantity) || 0);
    const price = this.parseBR(this.formPrice);
    const baseQuantity = quantity * this.store.unitFactor(this.formUnit);
    return {
      quantity,
      price,
      baseQuantity,
      baseUnit: this.store.baseUnit(this.formUnit),
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

  openModal(item?: Supply) {
    this.editId = item?.id ?? '';
    this.formCode = item?.code ?? '';
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

  onSubmit(event: Event) {
    event.preventDefault();

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

    const item: Supply = {
      id: this.editId || `item-${Date.now().toString(36)}`,
      name,
      type: this.formType,
      quantity,
      unit: this.formUnit,
      code: this.formCode.trim(),
      price
    };

    if (this.editId) {
      this.store.supplies = this.items.map(current => (current.id === this.editId ? item : current));
    } else {
      this.store.supplies = [item, ...this.items];
    }

    this.closeModal();
    this.toast.show('Informação atualizada com sucesso!');
  }

  deleteItem(item: Supply) {
    if (!window.confirm(`Excluir "${item.name}"?`)) return;
    this.store.supplies = this.items.filter(current => current.id !== item.id);
    this.toast.show('Item excluído da lista.');
  }

  clearAll() {
    this.search = '';
    this.typeFilter = 'all';
    this.store.supplies = [];
    this.toast.show('Dados de insumos limpos com sucesso!');
  }
}
