import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AppShellComponent } from '../../shared/app-shell/app-shell.component';
import { WorkspaceToastComponent } from '../../shared/components/workspace-toast/workspace-toast.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { AppSelectComponent, AppSelectOption } from '../../shared/components/app-select/app-select.component';
import { ProductItem, ProdutosApiService } from '../../services/produtos-api.service';
import {
  PrecificacoesApiService,
  PricingSimulationItem,
  SalvarSimulacaoCommand
} from '../../services/precificacoes-api.service';

interface Viability {
  cls: 'profit' | 'loss' | 'balance';
  icon: string;
  title: string;
  text: string;
  color: string;
}

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — PRECIFICAÇÃO
 * GET    /api/precificacoes         → lista o histórico de simulações
 * POST   /api/precificacoes         → salva uma simulação
 * PUT    /api/precificacoes/{id}    → atualiza
 * DELETE /api/precificacoes/{id}    → remove
 * DELETE /api/precificacoes         → limpa todo o histórico
 * HEADERS: Authorization: Bearer <token> (authInterceptor)
 *
 * O backend resolve o custo unitário vigente do produto (mesma ficha do
 * GET /api/produtos) e grava um retrato do cálculo — cost/suggested/profit/etc.
 * não mudam depois se o produto for renomeado ou tiver o custo alterado.
 * O cálculo repetido aqui serve só para a prévia em tempo real do formulário.
 * ============================================================
 */
@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule, FormsModule, AppShellComponent, WorkspaceToastComponent, ConfirmDialogComponent, AppSelectComponent],
  templateUrl: './pricing.component.html',
  styleUrl: './pricing.component.scss'
})
export class PricingComponent implements OnInit {
  private readonly api = inject(PrecificacoesApiService);
  private readonly produtosApi = inject(ProdutosApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
  private readonly percentFormat = new Intl.NumberFormat('pt-BR', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  });

  @ViewChild(WorkspaceToastComponent) toast!: WorkspaceToastComponent;
  @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;

  loading = false;
  saving = false;

  recipes: ProductItem[] = [];
  simulations: PricingSimulationItem[] = [];

  editId = '';
  selectedRecipeId = '';
  margin = 40;
  salePrice = 0;
  salesQuantity = 30;
  search = '';

  ngOnInit() {
    this.carregarTudo();
  }

  private carregarTudo() {
    this.loading = true;

    // As duas listas são independentes: uma só espera pela mais lenta
    forkJoin({
      recipes: this.produtosApi.listar(),
      simulations: this.api.listar()
    }).subscribe({
      next: ({ recipes, simulations }) => {
        this.recipes = recipes.data;
        this.simulations = simulations;

        const requested = this.route.snapshot.queryParamMap.get('recipe');
        const found = requested ? this.recipes.find(item => String(item.id) === String(requested)) : undefined;
        this.selectedRecipeId = found?.id ?? this.recipes[0]?.id ?? '';
        this.refreshPriceFromSuggestion();

        this.loading = false;
      },
      error: err => {
        this.toast.show(this.mensagemErro(err, 'Erro ao carregar a precificação.'));
        this.loading = false;
      }
    });
  }

  /* ===================== DADOS ===================== */

  get selectedRecipe(): ProductItem | undefined {
    return this.recipes.find(item => item.id === this.selectedRecipeId) ?? this.recipes[0];
  }

  recipeOptionLabel(item: ProductItem): string {
    return `${item.name} - Custo: ${this.money(Number(item.unitCost) || 0)}`;
  }

  get recipeOptions(): AppSelectOption[] {
    if (!this.recipes.length) {
      return [{ value: '', label: this.loading ? 'Carregando produtos...' : 'Nenhum produto cadastrado' }];
    }
    return this.recipes.map(item => ({ value: item.id, label: this.recipeOptionLabel(item) }));
  }

  /* ===================== CÁLCULO (prévia em tempo real) ===================== */

  get cost(): number {
    return Math.max(0, Number(this.selectedRecipe?.unitCost) || 0);
  }

  get suggested(): number {
    return this.cost * (1 + Math.max(0, Number(this.margin) || 0) / 100);
  }

  get price(): number {
    return Math.max(0, Number(this.salePrice) || 0);
  }

  get quantity(): number {
    return Math.max(0, Number(this.salesQuantity) || 0);
  }

  get profit(): number {
    return this.price - this.cost;
  }

  get realMargin(): number {
    return this.price > 0 ? (this.profit / this.price) * 100 : 0;
  }

  get revenue(): number {
    return this.price * this.quantity;
  }

  get totalProfit(): number {
    return this.profit * this.quantity;
  }

  get difference(): number {
    return this.price - this.suggested;
  }

  get unitName(): string {
    return this.selectedRecipe?.yieldName || 'unidade';
  }

  get viability(): Viability {
    if (this.profit > 0.005) {
      return {
        cls: 'profit',
        icon: '✓',
        title: 'Venda com lucro',
        text: `Você ganha ${this.money(this.profit)} por ${this.unitName} vendido.`,
        color: 'var(--success)'
      };
    }
    if (this.profit < -0.005) {
      return {
        cls: 'loss',
        icon: '!',
        title: 'Venda com prejuízo',
        text: `Faltam ${this.money(Math.abs(this.profit))} por ${this.unitName} para cobrir o custo.`,
        color: 'var(--danger)'
      };
    }
    return {
      cls: 'balance',
      icon: '=',
      title: 'Ponto de equilíbrio',
      text: 'O preço cobre o custo, mas ainda não gera lucro.',
      color: 'var(--warning)'
    };
  }

  get meterWidth(): number {
    return Math.min(100, Math.max(0, this.realMargin));
  }

  /** Preenche a faixa do slider até a posição atual, como no mockup. */
  get rangeBackground(): string {
    const progress = (Math.min(150, Math.max(0, this.margin)) / 150) * 100;
    return `linear-gradient(to right,var(--primary) 0%,var(--primary) ${progress}%,#dbeafe ${progress}%,#dbeafe 100%)`;
  }

  get recipeDetails(): string {
    const amount = Math.max(1, Number(this.selectedRecipe?.yieldAmount) || 1);
    return `Receita com rendimento de ${amount.toLocaleString('pt-BR')} ${this.pluralize(this.unitName, amount)}.`;
  }

  get differenceLabel(): string {
    const sign = this.difference > 0 ? '+ ' : this.difference < 0 ? '− ' : '';
    return `${sign}${this.money(Math.abs(this.difference))}`;
  }

  get differenceColor(): string {
    if (this.difference < -0.005) return 'var(--danger)';
    if (this.difference > 0.005) return 'var(--success)';
    return 'var(--text-main)';
  }

  get profitLabel(): string {
    return `${this.profit < 0 ? '− ' : ''}${this.money(Math.abs(this.profit))}`;
  }

  get profitColor(): string {
    return this.profit > 0 ? 'var(--success)' : this.profit < 0 ? 'var(--danger)' : 'var(--warning)';
  }

  get totalProfitLabel(): string {
    return `${this.totalProfit < 0 ? '− ' : ''}${this.money(Math.abs(this.totalProfit))}`;
  }

  get totalProfitColor(): string {
    return this.totalProfit > 0 ? 'var(--success)' : this.totalProfit < 0 ? 'var(--danger)' : 'var(--warning)';
  }

  private pluralize(word: string, count: number): string {
    if (count === 1) return word;
    const lower = String(word).toLowerCase();
    if (lower === 'fatia') return 'fatias';
    if (lower === 'porção') return 'porções';
    if (lower === 'unidade') return 'unidades';
    return `${word}s`;
  }

  money(value: number): string {
    return this.currency.format(Number.isFinite(value) ? value : 0);
  }

  percent(value: number): string {
    return `${this.percentFormat.format(Number.isFinite(value) ? value : 0)}%`;
  }

  /* ===================== INTERAÇÃO ===================== */

  private refreshPriceFromSuggestion() {
    this.salePrice = Number(this.suggested.toFixed(2));
  }

  onRecipeChange() {
    this.refreshPriceFromSuggestion();
  }

  onRangeChange(value: string) {
    this.margin = Number(value) || 0;
    this.refreshPriceFromSuggestion();
  }

  onMarginInput() {
    this.margin = Math.max(0, Number(this.margin) || 0);
    this.refreshPriceFromSuggestion();
  }

  /** O slider vai só até 150%, mas o campo numérico aceita mais. */
  get clampedMargin(): number {
    return Math.min(150, Math.max(0, this.margin));
  }

  /* ===================== SIMULAÇÕES SALVAS ===================== */

  get filteredSimulations(): PricingSimulationItem[] {
    const search = this.search.toLowerCase().trim();
    return this.simulations.filter(item => (item.recipeName || '').toLowerCase().includes(search));
  }

  get visibleCountLabel(): string {
    const total = this.filteredSimulations.length;
    return `${total} ${total === 1 ? 'simulação salva' : 'simulações salvas'}`;
  }

  simulationSubtitle(item: PricingSimulationItem): string {
    const date = item.createdAt ? new Date(item.createdAt).toLocaleDateString('pt-BR') : '';
    return date ? `Simulado em ${date}` : 'Simulação de preço';
  }

  rowProfitLabel(item: PricingSimulationItem): string {
    const value = Number(item.profit || 0);
    return `${value < 0 ? '− ' : ''}${this.money(Math.abs(value))}`;
  }

  rowProfitColor(item: PricingSimulationItem): string {
    const value = Number(item.profit || 0);
    return value > 0 ? 'var(--success)' : value < 0 ? 'var(--danger)' : 'var(--warning)';
  }

  onSubmit(event: Event) {
    event.preventDefault();
    if (this.saving) return;

    if (!this.selectedRecipeId) {
      this.toast.show('Cadastre um produto antes de simular um preço.');
      return;
    }

    const command: SalvarSimulacaoCommand = {
      recipeId: this.selectedRecipeId,
      margin: this.margin,
      salePrice: this.price,
      quantity: this.quantity
    };

    this.saving = true;

    if (this.editId) {
      this.api.atualizar(this.editId, command).subscribe({
        next: atualizada => this.onSalvoComSucesso(atualizada),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao atualizar a simulação.'));
          this.saving = false;
        }
      });
    } else {
      this.api.criar(command).subscribe({
        next: criada => this.onSalvoComSucesso(criada),
        error: err => {
          this.toast.show(this.mensagemErro(err, 'Erro ao salvar a simulação.'));
          this.saving = false;
        }
      });
    }
  }

  private onSalvoComSucesso(item: PricingSimulationItem) {
    this.simulations = this.editId
      ? this.simulations.map(current => (current.id === item.id ? item : current))
      : [item, ...this.simulations];

    this.editId = '';
    this.saving = false;
    this.toast.show('Informação atualizada com sucesso!');
  }

  editSimulation(item: PricingSimulationItem) {
    this.editId = item.id;
    if (item.recipeId) this.selectedRecipeId = item.recipeId;
    this.margin = item.margin ?? 40;
    this.salePrice = item.salePrice ?? 0;
    this.salesQuantity = item.quantity ?? 1;

    window.scrollTo({ top: 0, behavior: 'smooth' });
    document.getElementById('marginInput')?.focus();
  }

  async deleteSimulation(item: PricingSimulationItem) {
    const confirmed = await this.confirmDialog.open({
      title: 'Excluir simulação',
      message: `Deseja realmente excluir a simulação de "${item.recipeName || 'selecionada'}"?`,
      confirmLabel: 'Excluir',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.excluir(item.id).subscribe({
      next: () => {
        this.simulations = this.simulations.filter(current => current.id !== item.id);
        if (this.editId === item.id) this.editId = '';
        this.toast.show('Simulação excluída com sucesso!');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao excluir a simulação.'))
    });
  }

  async clearAllSimulations() {
    const confirmed = await this.confirmDialog.open({
      title: 'Limpar dados',
      message: 'Todas as simulações salvas serão removidas permanentemente. Deseja continuar?',
      confirmLabel: 'Limpar tudo',
      variant: 'danger'
    });
    if (!confirmed) return;

    this.api.limparTudo().subscribe({
      next: () => {
        this.simulations = [];
        this.search = '';
        this.toast.show('Simulações limpas com sucesso!');
      },
      error: err => this.toast.show(this.mensagemErro(err, 'Erro ao limpar as simulações.'))
    });
  }

  clearForm() {
    this.editId = '';
    this.margin = 40;
    this.salesQuantity = 1;
    this.refreshPriceFromSuggestion();
    this.toast.show('Dados da simulação limpos com sucesso!');
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
