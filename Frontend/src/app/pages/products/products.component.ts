import { CommonModule } from '@angular/common';
import { Component, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { CompositionEntry, MockStoreService, Product, Supply } from '../../services/mock-store.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — PRODUTOS / FICHA TÉCNICA
 * GET    /api/produtos          → lista
 * POST   /api/produtos          → cria
 * PUT    /api/produtos/{id}     → atualiza
 * DELETE /api/produtos/{id}     → remove
 * HEADERS: Authorization: Bearer <token>
 *
 * Cálculo (idêntico ao mockup):
 *   materiais = Σ (quantidade usada × custo unitário do insumo)
 *   trabalho  = (minutos ÷ 60) × valor da hora
 *   total     = materiais + trabalho
 *   unitário  = total ÷ rendimento
 * ============================================================
 */
@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AppShellComponent, WorkspaceToastComponent],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent {
  private readonly store = inject(MockStoreService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;

  editId = '';
  productCode = '';
  productName = 'Bolo de chocolate';
  productionType: 'Produto inteiro' | 'Porções' = 'Porções';
  yieldAmount: number | null = 10;
  yieldName = 'fatia';
  productionTime: number | null = 60;

  composition: CompositionEntry[] = this.store.demoComposition.map(entry => ({ ...entry }));

  search = '';

  /* ===================== DADOS ===================== */

  get supplies(): Supply[] {
    return this.store.supplies;
  }

  get hourlyRate(): number {
    return this.store.hourlyRate;
  }

  get products(): Product[] {
    return this.store.products;
  }

  get filteredProducts(): Product[] {
    const search = this.search.toLowerCase().trim();
    return this.products.filter(item => {
      const name = (item.name || '').toLowerCase();
      const code = (item.code || '').toLowerCase();
      return name.includes(search) || code.includes(search);
    });
  }

  get visibleCountLabel(): string {
    const total = this.filteredProducts.length;
    return `${total} ${total === 1 ? 'produto cadastrado' : 'produtos cadastrados'}`;
  }

  /* ===================== COMPOSIÇÃO ===================== */

  get compositionCountLabel(): string {
    const total = this.composition.length;
    return `${total} ${total === 1 ? 'item' : 'itens'}`;
  }

  entryBaseUnit(entry: CompositionEntry): string {
    const supply = this.store.findSupply(entry.itemId);
    return supply ? this.store.baseUnit(supply.unit) : 'un';
  }

  entryCost(entry: CompositionEntry): number {
    return this.store.compositionCost(entry);
  }

  addItem() {
    const first = this.supplies[0];
    if (!first) return;
    this.composition = [...this.composition, { itemId: first.id, amount: 0 }];

    setTimeout(() => {
      const inputs = document.querySelectorAll<HTMLInputElement>('.amount-input');
      inputs[inputs.length - 1]?.focus();
    });
  }

  removeItem(index: number) {
    this.composition = this.composition.filter((_, position) => position !== index);
  }

  /* ===================== CÁLCULO ===================== */

  get materialsCost(): number {
    return this.composition.reduce((total, entry) => total + this.entryCost(entry), 0);
  }

  get minutes(): number {
    return Math.max(0, Number(this.productionTime) || 0);
  }

  get yieldValue(): number {
    return Math.max(1, Number(this.yieldAmount) || 1);
  }

  get laborCost(): number {
    return (this.minutes / 60) * this.hourlyRate;
  }

  get totalCost(): number {
    return this.materialsCost + this.laborCost;
  }

  get portionCost(): number {
    return this.totalCost / this.yieldValue;
  }

  get unitName(): string {
    return this.yieldName.trim() || 'porção';
  }

  get yieldSummary(): string {
    return `Rendimento de ${this.yieldValue} ${this.unitName}${this.yieldValue === 1 ? '' : 's'}`;
  }

  get sourceBadgeText(): string {
    return this.hourlyRate > 0 ? 'Dados integrados' : 'Aguardando configuração';
  }

  get calculationStatus(): string {
    return this.hourlyRate > 0
      ? 'Ficha calculada em tempo real.'
      : 'Configure o valor da hora para completar o custo.';
  }

  get hourSourceTitle(): string {
    return this.hourlyRate > 0 ? 'Valor da hora integrado' : 'Valor da hora pendente';
  }

  get hourSourceText(): string {
    return this.hourlyRate > 0
      ? 'O custo do trabalho foi somado com base no valor salvo em custos operacionais.'
      : 'Configure o valor da hora na tela de custos operacionais.';
  }

  money(value: number): string {
    return this.currency.format(Number.isFinite(value) ? value : 0);
  }

  formatTime(minutes: number): string {
    const value = Math.max(0, Number(minutes) || 0);
    if (value < 60) return `${value} min`;
    const hours = Math.floor(value / 60);
    const rest = value % 60;
    return rest ? `${hours}h ${rest}min` : `${hours}h`;
  }

  yieldLabel(item: Product): string {
    const unitName = item.yieldName || 'porção';
    return `${item.yieldAmount} ${unitName}${item.yieldAmount === 1 ? '' : 's'}`;
  }

  productSubtitle(item: Product): string {
    const date = item.updatedAt ? new Date(item.updatedAt).toLocaleDateString('pt-BR') : '';
    if (item.code) return `Cód: ${item.code}`;
    return date ? `Atualizado em ${date}` : 'Produto cadastrado';
  }

  /* ===================== AÇÕES ===================== */

  onSubmit(event: Event) {
    event.preventDefault();

    const name = this.productName.trim();
    if (!name) {
      document.getElementById('recipeName')?.focus();
      this.toast.show('Informe o nome do produto');
      return;
    }

    const code = this.productCode.trim();
    const recipeId = this.editId || code || `recipe_${Date.now()}`;
    const product: Product = {
      id: recipeId,
      code,
      name,
      yieldAmount: this.yieldValue,
      yieldName: this.unitName,
      productionTime: this.minutes,
      composition: this.composition.map(entry => ({ ...entry })),
      materials: this.materialsCost,
      labor: this.laborCost,
      total: this.totalCost,
      unitCost: this.portionCost,
      updatedAt: new Date().toISOString()
    };

    const existingIndex = this.products.findIndex(item => item.id === recipeId);
    if (existingIndex >= 0) {
      this.store.products = this.products.map(item => (item.id === recipeId ? product : item));
    } else {
      this.store.products = [product, ...this.products];
    }

    this.editId = '';
    this.toast.show('Informação atualizada com sucesso!');
  }

  editProduct(item: Product) {
    this.editId = item.id;
    this.productCode = item.code || '';
    this.productName = item.name || '';
    this.yieldAmount = item.yieldAmount || 1;
    this.yieldName = item.yieldName || 'porção';
    this.productionTime = item.productionTime || 0;
    this.composition = Array.isArray(item.composition) ? item.composition.map(entry => ({ ...entry })) : [];

    window.scrollTo({ top: 0, behavior: 'smooth' });
    document.getElementById('recipeName')?.focus();
  }

  deleteProduct(item: Product) {
    if (!window.confirm(`Deseja realmente excluir o produto "${item.name || 'selecionado'}"?`)) return;
    this.store.products = this.products.filter(current => current.id !== item.id);
    this.toast.show('Produto excluído com sucesso!');
  }

  clearForm() {
    this.editId = '';
    this.productCode = '';
    this.productName = '';
    this.yieldAmount = 1;
    this.yieldName = 'unidade';
    this.productionTime = 0;
    this.composition = [];
    this.toast.show('Dados do formulário limpos!');
  }

  clearAllProducts() {
    if (!window.confirm('Deseja excluir todos os produtos cadastrados?')) return;
    this.store.products = [];
    this.editId = '';
    this.composition = [];
    this.toast.show('Produtos limpos com sucesso!');
  }
}
