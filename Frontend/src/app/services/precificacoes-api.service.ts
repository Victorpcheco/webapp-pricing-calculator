import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/** Simulação de preço salva, com todos os valores derivados já calculados pelo backend. */
export interface PricingSimulationItem {
  id: string;
  recipeId: string;
  recipeName: string;
  cost: number;
  margin: number;
  suggested: number;
  salePrice: number;
  quantity: number;
  profit: number;
  realMargin: number;
  revenue: number;
  totalProfit: number;
  createdAt: string;
}

export interface SalvarSimulacaoCommand {
  recipeId: string;
  margin: number;
  salePrice: number;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class PrecificacoesApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/precificacoes`;

  listar(): Observable<PricingSimulationItem[]> {
    return this.http.get<PricingSimulationItem[]>(this.apiUrl);
  }

  criar(command: SalvarSimulacaoCommand): Observable<PricingSimulationItem> {
    return this.http.post<PricingSimulationItem>(this.apiUrl, command);
  }

  atualizar(id: string, command: SalvarSimulacaoCommand): Observable<PricingSimulationItem> {
    return this.http.put<PricingSimulationItem>(`${this.apiUrl}/${id}`, command);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  limparTudo(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }
}
