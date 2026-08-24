import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { AppSelectComponent, AppSelectOption } from '../../shared/components/app-select/app-select.component';
import {
  CompositionInput,
  ProductItem,
  ProductionType,
  ProdutosApiService,
  SalvarProdutoCommand
} from '../../services/produtos-api.service';
import { InsumoItem, InsumosApiService } from '../../services/insumos-api.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — PRODUTOS / FICHA TÉCNICA
 * GET    /api/produtos          → lista + meta.hourlyRate
 * POST   /api/produtos          → cria
 * PUT    /api/produtos/{id}     → atualiza (substitui a composição)
 * DELETE /api/produtos/{id}     → remove
 * DELETE /api/produtos          → limpa todas
 * HEADERS: Authorization: Bearer <token> (authInterceptor)
 *
 * Os custos são recalculados pelo backend a cada leitura, com o preço
 * atual dos insumos e o valor da hora vigente. O cálculo repetido aqui
 * serve só para a prévia em tempo real enquanto o usuário digita.
 * ============================================================
 */
@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, AppShellComponent, WorkspaceToastComponent, ConfirmDialogComponent, AppSelectComponent],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent implements OnInit {
  private readonly api = inject(ProdutosApiService);
  private readonly insumosApi = inject(InsumosApiService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;
  @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;

  loading = false;
  saving = false;

  products: ProductItem[] = [];
  supplies: InsumoItem[] = [];
  hourlyRate = 0;

  editId = '';
  productName = '';
  productionType: ProductionType = 'Porções';
  yieldAmount: number | null = 1;
  yieldName = 'porção';
  productionTime: number | null = 0;

  composition: CompositionInput[] = [];

  search = '';

  ngOnInit() {
    this.carregarTudo();
  }

  /* ===================== CARGA ===================== */

  private carregarTudo() {
    this.loading = true;

    // As duas listas são independentes: uma só espera pela mais lenta
    forkJoin({
      produtos: this.api.listar(),
      insumos: this.insumosApi.listar()
    }).subscribe({
      next: ({ produtos, insumos }) => {
        this.products = produtos.data;
        this.hourlyRate = produtos.meta.hourlyRate;
        this.supplies = insumos.data;
        this.loading = false;
      },
      error: err => {
        this.toast.show(this.mensagemErro(err, 'Erro ao carregar os produtos.'));
        this.loading = false;
      }
    });
  }

  /* ===================== LISTA ===================== */

  get filteredProducts(): ProductItem[] {
    const search = this.search.toLowerCase().trim();
    if (!search) return this.products;
    return this.products.filter(item => (item.name || '').toLowerCase().includes(search));
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

  get supplyOptions(): AppSelectOption[] {
    return this.supplies.map(option => ({ value: option.id, label: `${option.name} · ${option.type}` }));
  }

  private findSupply(supplyId: string): InsumoItem | undefined {
    return this.supplies.find(item => item.id === supplyId);
  }

  entryBaseUnit(entry: CompositionInput): string {
    return this.findSupply(entry.supplyId)?.baseUnit ?? 'un';
  }

  entryCost(entry: CompositionInput): number {
    const supply = this.findSupply(entry.supplyId);
    return supply ? Math.max(0, Number(entry.amount) || 0) * supply.unitCost : 0;
  }

  addItem() {
    const first = this.supplies[0];
    if (!first) {
      this.toast.show('Cadastre um insumo antes de montar a composição.');
      return;
    }

    this.composition = [...this.composition, { supplyId: first.id, amount: 0 }];

    setTimeout(() => {
      const inputs = document.querySelectorAll<HTMLInputElement>('.amount-input');
      inputs[inputs.length - 1]?.focus();
    });
  }

  removeItem(index: number) {
    this.composition = this.composition.filter((_, position) => position !== index);
  }

  /* ===================== PRÉVIA (client-side) ===================== */

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

  /* ===================== FORMATAÇÃO ===================== */

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

  yieldLabel(item: ProductItem): string {
    const unitName = item.yieldName || 'porção';
    return `${item.yieldAmount} ${unitName}${item.yieldAmount === 1 ? '' : 's'}`;
  }

  productSubtitle(item: ProductItem): string {
    const date = item.updatedAt ? new Date(item.updatedAt).toLocaleDateString('pt-BR') : '';
    return date ? `Atualizado em ${date}` : 'Produto cadastrado';
  }

  /* ===================== AÇÕES ===================== */

  onSubmit(event: Event) {
    event.preventDefault();
    if (this.saving) return;

    const name = this.productName.trim();
    if (!name) {
      document.getElementById('recipeName')?.focus();
      this.toast.show('Informe o nome do produto');
      return;
    }

    const semInsumo = this.composition.find(entry => !entry.supplyId);
    if (semInsumo) {
      this.toast.show('Há um item da composição sem insumo selecionado.');
      return;
    }

    const semQuantidade = this.composition.find(entry => !(Number(entry.amount) > 0));
    if (semQuantidade) {
      this.toast.show('Informe a quantidade usada de cada insumo.');
      return;
    }

    const command: SalvarProdutoCommand = {
      name,
      productionType: this.productionType,
      yieldAmount: this.yieldValue,
      yieldName: this.unitName,
      productionTime: this.minutes,
      composition: this.composition.map(entry => ({
        supplyId: entry.supplyId,
        amount: Number(entry.amount)
      }))
    };

    this.saving = true;

    if (this.editId) {
      this.api.atualizar(this.editId, command).subscribe({
        next: atualizado => this.onSalvoComSucesso(atualizado),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao atualizar o produto.'));
          this.saving = false;
        }
      });
    } else {
      this.api.criar(command).subscribe({
        next: criado => this.onSalvoComSucesso(criado),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao salvar o produto.'));
          this.saving = false;
        }
      });
    }
  }

  private onSalvoComSucesso(item: ProductItem) {
    this.products = this.editId
      ? this.products.map(current => (current.id === item.id ? item : current))
      : [item, ...this.products];

    this.editId = '';
    this.saving = false;
    this.toast.show('Informação atualizada com sucesso!');
  }

  editProduct(item: ProductItem) {
    this.editId = item.id;
    this.productName = item.name || '';
    this.productionType = item.productionType || 'Porções';
    this.yieldAmount = item.yieldAmount || 1;
    this.yieldName = item.yieldName || 'porção';
    this.productionTime = item.productionTime || 0;
    this.composition = item.composition.map(entry => ({
      supplyId: entry.supplyId,
      amount: entry.amount
    }));

    window.scrollTo({ top: 0, behavior: 'smooth' });
    document.getElementById('recipeName')?.focus();
  }

  async deleteProduct(item: ProductItem) {
    const confirmed = await this.confirmDialog.open({
      title: 'Excluir produto',
      message: `Tem certeza que deseja excluir "${item.name || 'este produto'}"? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.excluir(item.id).subscribe({
      next: () => {
        this.products = this.products.filter(current => current.id !== item.id);
        if (this.editId === item.id) this.clearForm(false);
        this.toast.show('Produto excluído com sucesso!');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao excluir o produto.'))
    });
  }

  async clearAllProducts() {
    const confirmed = await this.confirmDialog.open({
      title: 'Limpar dados',
      message: 'Todos os produtos cadastrados serão removidos permanentemente. Deseja continuar?',
      confirmLabel: 'Limpar tudo',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.limparTudo().subscribe({
      next: () => {
        this.products = [];
        this.clearForm(false);
        this.search = '';
        this.toast.show('Produtos limpos com sucesso!');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao limpar os produtos.'))
    });
  }

  clearForm(avisar = true) {
    this.editId = '';
    this.productName = '';
    this.productionType = 'Porções';
    this.yieldAmount = 1;
    this.yieldName = 'unidade';
    this.productionTime = 0;
    this.composition = [];
    if (avisar) this.toast.show('Dados do formulário limpos!');
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
