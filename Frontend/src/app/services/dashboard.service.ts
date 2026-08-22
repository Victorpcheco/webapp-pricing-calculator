import { Injectable, inject } from '@angular/core';
import { Observable, of } from 'rxjs';
import { MockStoreService } from './mock-store.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — DASHBOARD / VISÃO GERAL
 * ENDPOINT: GET /api/dashboard/resumo
 * HEADERS:  Authorization: Bearer <token>
 * RETORNO ESPERADO (200 OK): DashboardResumo
 *
 * Enquanto o endpoint não existe, o resumo é derivado do
 * `MockStoreService` — a mesma fonte das demais telas. É o que o
 * `js/dashboard.js` do mockup fazia ao ler as chaves do
 * localStorage gravadas por insumos, produtos, custos e
 * precificação: o painel reflete o que foi cadastrado.
 * ============================================================
 */

export type TipoAtividade = 'precificacao' | 'produto' | 'insumo';

export interface DesempenhoProduto {
  nome: string;
  custo: number;
  preco: number;
  lucro: number;
}

export interface AtividadeRecente {
  tipo: TipoAtividade;
  titulo: string;
  descricao: string;
  data: string;
}

export interface DashboardResumo {
  valorHora: number;
  totalInsumos: number;
  totalReceitas: number;
  totalSimulacoes: number;
  desempenhoProdutos: DesempenhoProduto[];
  atividadesRecentes: AtividadeRecente[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private readonly store = inject(MockStoreService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  getResumo(): Observable<DashboardResumo> {
    return of({
      valorHora: this.store.hourlyRate,
      totalInsumos: this.store.supplies.length,
      totalReceitas: this.store.products.length,
      totalSimulacoes: this.store.simulations.length,
      desempenhoProdutos: this.buildDesempenho(),
      atividadesRecentes: this.buildAtividades()
    });
  }

  /** Prioriza as simulações de preço; sem elas, estima 40% de margem sobre o custo. */
  private buildDesempenho(): DesempenhoProduto[] {
    if (this.store.simulations.length) {
      return this.store.simulations.map(simulation => {
        const recipe = this.store.findProduct(simulation.recipeId);
        const custo = Number(recipe?.unitCost !== undefined ? recipe.unitCost : simulation.cost || 0);
        const preco = Number(simulation.salePrice || 0);
        return { nome: recipe?.name || simulation.recipeName || 'Produto', custo, preco, lucro: preco - custo };
      });
    }

    return this.store.products.map(product => {
      const custo = Number(product.unitCost || 0);
      const preco = custo * 1.4;
      return { nome: product.name || 'Produto', custo, preco, lucro: preco - custo };
    });
  }

  private buildAtividades(): AtividadeRecente[] {
    const atividades: AtividadeRecente[] = [];
    const now = Date.now();

    const lastSimulation = this.store.simulations[0];
    if (lastSimulation) {
      atividades.push({
        tipo: 'precificacao',
        titulo: `Precificação de ${lastSimulation.recipeName || 'Produto'}`,
        descricao: `Preço definido em ${this.currency.format(lastSimulation.salePrice || 0)}`,
        data: lastSimulation.createdAt || new Date(now).toISOString()
      });
    }

    const lastProduct = this.store.products[0];
    if (lastProduct) {
      atividades.push({
        tipo: 'produto',
        titulo: `Produto: ${lastProduct.name}`,
        descricao: `Rendimento de ${lastProduct.yieldAmount || 1} ${lastProduct.yieldName || 'un'}`,
        data: lastProduct.updatedAt || new Date(now).toISOString()
      });
    }

    const lastSupply = this.store.supplies[0];
    if (lastSupply) {
      atividades.push({
        tipo: 'insumo',
        titulo: `Insumo: ${lastSupply.name}`,
        descricao: `${lastSupply.quantity} ${lastSupply.unit} por ${this.currency.format(lastSupply.price || 0)}`,
        data: new Date(now - 27 * 60 * 60 * 1000).toISOString()
      });
    }

    return atividades;
  }
}
