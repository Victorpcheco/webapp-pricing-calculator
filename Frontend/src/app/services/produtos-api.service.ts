import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type ProductionType = 'Produto inteiro' | 'Porções';

/** Uma linha da composição, com o insumo já resolvido pelo backend. */
export interface CompositionItem {
  supplyId: string;
  supplyName: string | null;
  /** false quando o insumo foi excluído depois da ficha criada. */
  supplyAvailable: boolean;
  amount: number;
  baseUnit: string;
  supplyUnitCost: number;
  cost: number;
}

/** Ficha técnica com os custos recalculados no momento da leitura. */
export interface ProductItem {
  id: string;
  name: string;
  productionType: ProductionType;
  yieldAmount: number;
  yieldName: string;
  productionTime: number;
  composition: CompositionItem[];
  materialsCost: number;
  laborCost: number;
  totalCost: number;
  unitCost: number;
  hourlyRateUsed: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProdutosResumo {
  total: number;
  /** Valor da hora vindo da configuração de custo mais recente; 0 se não houver. */
  hourlyRate: number;
}

export interface ProdutosListResponse {
  data: ProductItem[];
  meta: ProdutosResumo;
}

export interface CompositionInput {
  supplyId: string;
  amount: number;
}

export interface SalvarProdutoCommand {
  name: string;
  productionType: ProductionType;
  yieldAmount: number;
  yieldName: string;
  productionTime: number;
  composition: CompositionInput[];
}

@Injectable({ providedIn: 'root' })
export class ProdutosApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/produtos`;

  listar(nome?: string): Observable<ProdutosListResponse> {
    let params = new HttpParams();
    if (nome) params = params.set('nome', nome);

    return this.http.get<ProdutosListResponse>(this.apiUrl, { params });
  }

  criar(command: SalvarProdutoCommand): Observable<ProductItem> {
    return this.http.post<ProductItem>(this.apiUrl, command);
  }

  atualizar(id: string, command: SalvarProdutoCommand): Observable<ProductItem> {
    return this.http.put<ProductItem>(`${this.apiUrl}/${id}`, command);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  limparTudo(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }
}
