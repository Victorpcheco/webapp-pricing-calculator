import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export type ContractType = 'CLT' | 'Freelancer';
export type EmployeeStatus = 'Ativo' | 'Inativo';
export type FreelancerFrequency = 'Mensal' | 'Por hora' | 'Por serviço';

/** Encargos provisionados pelo backend. Vem zerado para Freelancer. */
export interface EmployeeCharges {
  fgts: number;
  decimoTerceiro: number;
  ferias: number;
  umTercoFerias: number;
  total: number;
}

/** Colaborador já com os encargos e o custo mensal calculados pelo backend. */
export interface EmployeeItem {
  id: string;
  code: string | null;
  name: string;
  role: string;
  contractType: ContractType;
  status: EmployeeStatus;
  admissionDate: string;
  /** Salário bruto mensal (CLT) ou valor combinado (Freelancer). */
  baseValue: number;
  /** null quando o contrato é CLT. */
  freelancerFrequency: FreelancerFrequency | null;
  phone: string | null;
  charges: EmployeeCharges;
  /**
   * CLT: salário + encargos. Freelancer mensal: o valor combinado.
   * Freelancer por hora/serviço: 0 — sem volume contratado não há projeção mensal.
   */
  monthlyCost: number;
  createdAt: string;
  updatedAt: string;
}

/** Totalizadores dos cards — refletem o universo completo, não o recorte filtrado. */
export interface ColaboradoresResumo {
  total: number;
  cltCount: number;
  freelancerCount: number;
  payrollValue: number;
}

export interface ColaboradoresListResponse {
  data: EmployeeItem[];
  meta: ColaboradoresResumo;
}

export interface SalvarColaboradorCommand {
  name: string;
  role: string;
  contractType: ContractType;
  status: EmployeeStatus;
  /** ISO 8601; null deixa o backend assumir a data de hoje. */
  admissionDate: string | null;
  baseValue: number;
  /** Ignorado pelo backend quando o contrato é CLT. */
  freelancerFrequency: FreelancerFrequency | null;
  phone: string | null;
}

export interface ListarColaboradoresFiltros {
  /** Termo único aplicado a nome E cargo. */
  busca?: string;
  tipo?: ContractType;
}

@Injectable({ providedIn: 'root' })
export class ColaboradoresApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/colaboradores`;

  listar(filtros?: ListarColaboradoresFiltros): Observable<ColaboradoresListResponse> {
    let params = new HttpParams();
    if (filtros?.busca) params = params.set('busca', filtros.busca);
    if (filtros?.tipo) params = params.set('tipo', filtros.tipo);

    return this.http.get<ColaboradoresListResponse>(this.apiUrl, { params });
  }

  criar(command: SalvarColaboradorCommand): Observable<EmployeeItem> {
    return this.http.post<EmployeeItem>(this.apiUrl, command);
  }

  atualizar(id: string, command: SalvarColaboradorCommand): Observable<EmployeeItem> {
    return this.http.put<EmployeeItem>(`${this.apiUrl}/${id}`, command);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  limparTudo(): Observable<void> {
    return this.http.delete<void>(this.apiUrl);
  }
}
