import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CostHistoryItem {
  id: string;
  description: string;
  createdAt: string;
  salary: number;
  hours: number;
  energy: number;
  energyPercent: number;
  gas: number;
  gasPercent: number;
  hasMei: boolean;
  das: number;
  depreciationRate: number;
  energyReal: number;
  gasReal: number;
  depreciation: number;
  monthly: number;
  hour: number;
}

export interface SalvarCustoCommand {
  description?: string | null;
  salary: number;
  hours: number;
  energy: number;
  energyPercent: number;
  gas: number;
  gasPercent: number;
  hasMei: boolean;
  das: number;
  depreciationRate: number;
}

@Injectable({ providedIn: 'root' })
export class CustosApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/custos`;

  listar(): Observable<CostHistoryItem[]> {
    return this.http.get<CostHistoryItem[]>(this.apiUrl);
  }

  criar(command: SalvarCustoCommand): Observable<CostHistoryItem> {
    return this.http.post<CostHistoryItem>(this.apiUrl, command);
  }

  atualizar(id: string, command: SalvarCustoCommand): Observable<CostHistoryItem> {
    return this.http.put<CostHistoryItem>(`${this.apiUrl}/${id}`, command);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  limparHistorico(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }
}
