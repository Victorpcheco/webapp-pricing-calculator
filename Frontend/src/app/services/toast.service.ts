import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type ToastType = 'success' | 'error';

export interface ToastMessage {
  id: number;
  message: string;
  type: ToastType;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  public toasts$: Observable<ToastMessage[]> = this.toastsSubject.asObservable();
  private nextId = 0;

  showSuccess(message: string): void {
    this.addToast(message, 'success');
  }

  showError(message: string): void {
    this.addToast(message, 'error');
  }

  private addToast(message: string, type: ToastType): void {
    const id = this.nextId++;
    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next([...currentToasts, { id, message, type }]);

    // Auto remove after 4 seconds
    setTimeout(() => {
      this.removeToast(id);
    }, 4000);
  }

  removeToast(id: number): void {
    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next(currentToasts.filter(t => t.id !== id));
  }
}
