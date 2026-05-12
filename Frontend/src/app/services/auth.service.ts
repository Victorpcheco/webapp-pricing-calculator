import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

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

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5253/api/auth';

  login(command: LoginCommand): Observable<AuthenticationResult> {
    return this.http.post<AuthenticationResult>(`${this.apiUrl}/login`, command).pipe(
      tap(result => this.setToken(result.token))
    );
  }

  register(command: RegisterUserCommand): Observable<{ userId: string }> {
    return this.http.post<{ userId: string }>(`${this.apiUrl}/register`, command);
  }

  setToken(token: string) {
    localStorage.setItem('auth_token', token);
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  logout() {
    localStorage.removeItem('auth_token');
  }
}
