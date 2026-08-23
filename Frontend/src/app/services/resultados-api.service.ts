import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type ResultadosPeriodo = 'all' | 'today' | 'week' | 'month' | 'custom';

/**
 * Uma linha da tabela de desempenho. Quando priced é false, o produto ainda não
 * tem simulação salva no período — salePrice/profit/margin vêm nulos.
 */
export interface ResultadoRow {
  productId: string;
  name: string;
  unit: string;
  cost: number;
  salePrice: number | null;
  profit: number | null;
  margin: number | null;
  priced: boolean;
}

/** KPIs do topo da tela — somados sobre as mesmas linhas devolvidas em rows. */
export interface ResultadoResumo {
  totalProfit: number;
  totalRevenue: number;
  averageMargin: number;
  analysedCount: number;
}

export interface ResultadoListResponse {
  rows: ResultadoRow[];
  totals: ResultadoResumo;
}

export interface ListarResultadosFiltros {
  periodo: ResultadosPeriodo;
  /** Só enviados quando periodo = 'custom'. */
  inicio?: string;
  fim?: string;
}

@Injectable({ providedIn: 'root' })
export class ResultadosApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/resultados`;

  listar(filtros: ListarResultadosFiltros): Observable<ResultadoListResponse> {
    let params = new HttpParams().set('periodo', filtros.periodo);
    if (filtros.inicio) params = params.set('inicio', filtros.inicio);
    if (filtros.fim) params = params.set('fim', filtros.fim);

    return this.http.get<ResultadoListResponse>(this.apiUrl, { params });
  }
}
