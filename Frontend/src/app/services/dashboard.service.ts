import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map } from 'rxjs';
import { InsumoItem, InsumosApiService } from './insumos-api.service';
import { ProductItem, ProdutosApiService } from './produtos-api.service';
import { PrecificacoesApiService, PricingSimulationItem } from './precificacoes-api.service';
import { CustosApiService } from './custos-api.service';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — DASHBOARD / VISÃO GERAL
 * Não existe um endpoint dedicado: o resumo é consolidado no cliente a
 * partir dos quatro endpoints que as demais telas já usam —
 * GET /api/insumos, /api/produtos, /api/precificacoes e /api/custos —
 * todos com Authorization: Bearer <token> (authInterceptor).
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
  private readonly insumosApi = inject(InsumosApiService);
  private readonly produtosApi = inject(ProdutosApiService);
  private readonly precificacoesApi = inject(PrecificacoesApiService);
  private readonly custosApi = inject(CustosApiService);
  private readonly currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

  getResumo(): Observable<DashboardResumo> {
    return forkJoin({
      insumos: this.insumosApi.listar(),
      produtos: this.produtosApi.listar(),
      simulacoes: this.precificacoesApi.listar(),
      custos: this.custosApi.listar()
    }).pipe(
      map(({ insumos, produtos, simulacoes, custos }) => ({
        // Histórico vem do mais recente ao mais antigo; sem custo salvo, exibe zerado
        valorHora: custos[0]?.hour > 0 ? custos[0].hour : 0,
        totalInsumos: insumos.meta.total,
        totalReceitas: produtos.meta.total,
        totalSimulacoes: simulacoes.length,
        desempenhoProdutos: this.buildDesempenho(produtos.data, simulacoes),
        atividadesRecentes: this.buildAtividades(simulacoes, produtos.data, insumos.data)
      }))
    );
  }

  /** Prioriza as simulações de preço; sem elas, estima 40% de margem sobre o custo. */
  private buildDesempenho(produtos: ProductItem[], simulacoes: PricingSimulationItem[]): DesempenhoProduto[] {
    if (simulacoes.length) {
      return simulacoes.map(simulacao => {
        // Custo vigente do produto quando ele ainda existe; senão, o retrato gravado na simulação
        const produto = produtos.find(item => item.id === simulacao.recipeId);
        const custo = produto ? produto.unitCost : simulacao.cost;
        const preco = Number(simulacao.salePrice || 0);
        return { nome: produto?.name || simulacao.recipeName || 'Produto', custo, preco, lucro: preco - custo };
      });
    }

    return produtos.map(produto => {
      const custo = Number(produto.unitCost || 0);
      const preco = custo * 1.4;
      return { nome: produto.name || 'Produto', custo, preco, lucro: preco - custo };
    });
  }

  private buildAtividades(
    simulacoes: PricingSimulationItem[],
    produtos: ProductItem[],
    insumos: InsumoItem[]
  ): AtividadeRecente[] {
    const atividades: AtividadeRecente[] = [];

    // As três listas já chegam ordenadas do mais recente ao mais antigo pelo backend
    const lastSimulation = simulacoes[0];
    if (lastSimulation) {
      atividades.push({
        tipo: 'precificacao',
        titulo: `Precificação de ${lastSimulation.recipeName || 'Produto'}`,
        descricao: `Preço definido em ${this.currency.format(lastSimulation.salePrice || 0)}`,
        data: lastSimulation.createdAt
      });
    }

    const lastProduct = produtos[0];
    if (lastProduct) {
      atividades.push({
        tipo: 'produto',
        titulo: `Produto: ${lastProduct.name}`,
        descricao: `Rendimento de ${lastProduct.yieldAmount || 1} ${lastProduct.yieldName || 'un'}`,
        data: lastProduct.updatedAt
      });
    }

    const lastSupply = insumos[0];
    if (lastSupply) {
      atividades.push({
        tipo: 'insumo',
        titulo: `Insumo: ${lastSupply.name}`,
        descricao: `${lastSupply.quantity} ${lastSupply.unit} por ${this.currency.format(lastSupply.price || 0)}`,
        data: lastSupply.createdAt
      });
    }

    return atividades;
  }
}
