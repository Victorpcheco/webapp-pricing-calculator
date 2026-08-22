import { CommonModule } from '@angular/common';
import { Component, HostListener, Input, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface NavigationItem {
  label: string;
  icon: string;
  path: string;
  /** Telas ainda não implementadas ficam visíveis, porém sem navegação. */
  disabled?: boolean;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  /** Último item do breadcrumb, exibido em destaque na topbar. */
  @Input() breadcrumb = 'Visão geral';
  @Input() userName = 'Marcia Oliveira';
  @Input() userCompany = 'Microempreendedora';

  isDropdownOpen = false;

  readonly navigation: NavigationItem[] = [
    { label: 'Visão geral', icon: '⌂', path: '/dashboard' },
    { label: 'Meus Custos', icon: 'R$', path: '/meus-custos', disabled: true },
    { label: 'Meus Insumos', icon: '◇', path: '/meus-insumos', disabled: true },
    { label: 'Meus Produtos', icon: '▦', path: '/meus-produtos', disabled: true },
    { label: 'Calcular Preços', icon: '%', path: '/precificacao', disabled: true },
    { label: 'Meus Resultados', icon: '↗', path: '/meus-resultados', disabled: true }
  ];

  get userInitials(): string {
    return (this.userName || 'U')
      .split(' ')
      .map(part => part[0])
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }

  toggleDropdown(event: Event) {
    event.stopPropagation();
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  @HostListener('document:click')
  closeDropdown() {
    this.isDropdownOpen = false;
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    this.isDropdownOpen = false;
  }

  logout() {
    this.authService.logout();
    void this.router.navigate(['/']);
  }
}
