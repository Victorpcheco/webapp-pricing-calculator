import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface NavigationItem {
  label: string;
  path: string;
}

@Component({
  selector: 'app-shell',
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  readonly navigation: NavigationItem[] = [
    { label: 'Painel', path: '/dashboard' },
    { label: 'Produtos', path: '/produtos' },
    { label: 'Ingredientes', path: '/ingredientes' },
    { label: 'Pedidos', path: '/pedidos' },
    { label: 'Relatórios', path: '/relatorios' }
  ];
}
