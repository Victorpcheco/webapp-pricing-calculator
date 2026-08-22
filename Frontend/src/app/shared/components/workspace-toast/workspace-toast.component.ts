import { Component } from '@angular/core';

/**
 * Toast das telas internas — réplica do `.toast` usado nos mockups.
 * O `ToastService` global permanece dedicado às telas de autenticação,
 * que têm outro visual.
 */
@Component({
  selector: 'app-workspace-toast',
  standalone: true,
  templateUrl: './workspace-toast.component.html',
  styleUrl: './workspace-toast.component.scss'
})
export class WorkspaceToastComponent {
  message = '';
  visible = false;

  private timer?: ReturnType<typeof setTimeout>;

  show(message: string) {
    this.message = message;
    this.visible = true;
    clearTimeout(this.timer);
    this.timer = setTimeout(() => (this.visible = false), 2800);
  }
}
