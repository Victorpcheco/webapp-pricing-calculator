import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginCommand {
  email: string;
  senhaHash: string;
}

export interface RegisterUserCommand {
  nome: string;
  telefone: string;
  email: string;
  senhaHash: string;
}

export interface AuthenticationResult {
  userId: string;
  token: string;
}

export interface RequestPasswordResetCommand {
  email: string;
}

export interface ResetPasswordCommand {
  email: string;
  token: string;
  novaSenha: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/auth`;

  login(command: LoginCommand): Observable<AuthenticationResult> {
    return this.http.post<AuthenticationResult>(`${this.apiUrl}/login`, command).pipe(
      tap(result => this.setToken(result.token))
    );
  }

  register(command: RegisterUserCommand): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(`${this.apiUrl}/register`, command);
  }

  requestPasswordReset(command: RequestPasswordResetCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, command);
  }

  resetPassword(command: ResetPasswordCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, command);
  }

  setToken(token: string) {
    localStorage.setItem('auth_token', token);
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getUserName(): string {
    const token = this.getToken();
    if (!token) return '';
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload['name'] || payload['unique_name'] || payload['email'] || '';
    } catch {
      return '';
    }
  }

  logout() {
    localStorage.removeItem('auth_token');
  }
}
