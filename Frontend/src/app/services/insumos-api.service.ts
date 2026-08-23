import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type InsumoTipo = 'Ingrediente' | 'Embalagem';
export type InsumoUnidade = 'kg' | 'g' | 'L' | 'ml' | 'un';

/** Item de insumo já com os campos calculados pelo backend. */
export interface InsumoItem {
  id: string;
  name: string;
  type: InsumoTipo;
  quantity: number;
  unit: InsumoUnidade;
  price: number;
  /** preço ÷ quantidade convertida para a unidade base */
  unitCost: number;
  baseQuantity: number;
  baseUnit: string;
  createdAt: string;
  updatedAt: string;
}

/** Totalizadores dos cards — refletem o universo completo, não o recorte filtrado. */
export interface InsumosResumo {
  total: number;
  ingredientCount: number;
  packageCount: number;
  purchaseValue: number;
}

export interface InsumosListResponse {
  data: InsumoItem[];
  meta: InsumosResumo;
}

export interface SalvarInsumoCommand {
  name: string;
  type: InsumoTipo;
  quantity: number;
  unit: InsumoUnidade;
  price: number;
}

export interface ListarInsumosFiltros {
  nome?: string;
  tipo?: InsumoTipo;
}

@Injectable({ providedIn: 'root' })
export class InsumosApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/insumos`;

  listar(filtros?: ListarInsumosFiltros): Observable<InsumosListResponse> {
    let params = new HttpParams();
    if (filtros?.nome) params = params.set('nome', filtros.nome);
    if (filtros?.tipo) params = params.set('tipo', filtros.tipo);

    return this.http.get<InsumosListResponse>(this.apiUrl, { params });
  }

  criar(command: SalvarInsumoCommand): Observable<InsumoItem> {
    return this.http.post<InsumoItem>(this.apiUrl, command);
  }

  atualizar(id: string, command: SalvarInsumoCommand): Observable<InsumoItem> {
    return this.http.put<InsumoItem>(`${this.apiUrl}/${id}`, command);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  limparTudo(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }
}
