import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmDialogOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  variant?: 'danger' | 'warning';
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
  visible = false;
  title = '';
  message = '';
  confirmLabel = 'Confirmar';
  variant: 'danger' | 'warning' = 'danger';

  private resolve?: (value: boolean) => void;

  open(options: ConfirmDialogOptions): Promise<boolean> {
    this.title = options.title;
    this.message = options.message;
    this.confirmLabel = options.confirmLabel ?? 'Confirmar';
    this.variant = options.variant ?? 'danger';
    this.visible = true;
    return new Promise(res => (this.resolve = res));
  }

  confirm() {
    this.visible = false;
    this.resolve?.(true);
  }

  cancel() {
    this.visible = false;
    this.resolve?.(false);
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.visible) this.cancel();
  }
}
