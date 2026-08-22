import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';

/**
 * ============================================================
 * BACKEND (C# / ASP.NET) — DASHBOARD / VISÃO GERAL
 * ENDPOINT: GET /api/dashboard/resumo
 * HEADERS:  Authorization: Bearer <token>
 * RETORNO ESPERADO (200 OK): DashboardResumo
 *
 * Enquanto o endpoint não existe, `getResumo()` devolve o mock
 * abaixo. Para plugar no backend basta trocar o `of(MOCK_RESUMO)`
 * por `this.http.get<DashboardResumo>(`${apiUrl}/dashboard/resumo`)`.
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

function isoHorasAtras(horas: number): string {
  return new Date(Date.now() - horas * 60 * 60 * 1000).toISOString();
}

const MOCK_RESUMO: DashboardResumo = {
  valorHora: 19.94,
  totalInsumos: 14,
  totalReceitas: 6,
  totalSimulacoes: 4,
  desempenhoProdutos: [
    { nome: 'Cesta de Café da Manhã', custo: 42.3, preco: 75.0, lucro: 32.7 },
    { nome: 'Bolo de Chocolate', custo: 21.4, preco: 38.0, lucro: 16.6 },
    { nome: 'Torta de Limão', custo: 18.6, preco: 32.0, lucro: 13.4 },
    { nome: 'Kit Brownie (6 un)', custo: 12.75, preco: 24.0, lucro: 11.25 },
    { nome: 'Pão de Mel (10 un)', custo: 9.8, preco: 18.5, lucro: 8.7 }
  ],
  atividadesRecentes: [
    {
      tipo: 'precificacao',
      titulo: 'Precificação de Cesta de Café da Manhã',
      descricao: 'Preço definido em R$ 75,00',
      data: isoHorasAtras(0)
    },
    {
      tipo: 'produto',
      titulo: 'Produto: Bolo de Chocolate',
      descricao: 'Rendimento de 12 fatias',
      data: isoHorasAtras(3)
    },
    {
      tipo: 'insumo',
      titulo: 'Insumo: Chocolate em pó 50%',
      descricao: '500 g por R$ 18,90',
      data: isoHorasAtras(27)
    }
  ]
};

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  getResumo(): Observable<DashboardResumo> {
    return of(MOCK_RESUMO);
  }
}
