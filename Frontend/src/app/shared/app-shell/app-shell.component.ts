import { CommonModule } from '@angular/common';
import { Component, HostListener, Input, ViewChild, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HelpCenterComponent } from '../components/help-center/help-center.component';

interface NavigationItem {
  label: string;
  icon: string;
  path: string;
}

interface SidebarTip {
  eyebrow: string;
  title: string;
  text: string;
}

/** Cada tela do mockup traz um destaque próprio no rodapé da sidebar. */
const SIDEBAR_TIPS: Record<string, SidebarTip> = {
  '/dashboard': {
    eyebrow: 'Jornada Completa',
    title: 'Custos → Insumos → Receitas → Precificação → Lucro.',
    text: 'Tudo integrado automaticamente no seu negócio.'
  },
  '/meus-custos': {
    eyebrow: 'Por que calcular?',
    title: 'Seu tempo também faz parte do custo.',
    text: 'O valor da hora será usado depois para calcular o custo real de cada produto.'
  },
  '/meus-insumos': {
    eyebrow: 'Precisão no custo',
    title: 'Padronize para comparar.',
    text: 'Compras em kg e litros são convertidas para gramas e mililitros automaticamente.'
  },
  '/meus-produtos': {
    eyebrow: 'Composição',
    title: 'Produto com custo completo.',
    text: 'O custo dos materiais é somado ao valor do tempo dedicado à produção.'
  },
  '/meus-colaboradores': {
    eyebrow: 'Gestão da equipe',
    title: 'CLT ou Freelancer, o custo real na ponta do lápis.',
    text: 'Colaboradores CLT já somam FGTS, 13º e férias + 1/3 automaticamente.'
  },
  '/precificacao': {
    eyebrow: 'Decisão mais segura',
    title: 'Preço com base no custo real.',
    text: 'Teste margens e preços antes de vender para proteger o lucro do seu negócio.'
  },
  '/meus-resultados': {
    eyebrow: 'Decisão com base em dados',
    title: 'Seus resultados em tempo real.',
    text: 'Acompanhe o desempenho de cada produto e identifique o que dá lucro de verdade.'
  }
};

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, HelpCenterComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent {
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);


  @ViewChild('helpCenter') private helpCenter?: HelpCenterComponent;

  /** Último item do breadcrumb, exibido em destaque na topbar. */
  @Input() breadcrumb = 'Visão geral';
  /** Nível intermediário opcional do breadcrumb (ex.: "Configurações"). */
  @Input() breadcrumbParent = '';

  get userName(): string {
    return this.authService.getUserName();
  }

  isDropdownOpen = false;

  readonly navigation: NavigationItem[] = [
    { label: 'Visão geral', icon: '⌂', path: '/dashboard' },
    { label: 'Meus Custos', icon: 'R$', path: '/meus-custos' },
    { label: 'Meus Insumos', icon: '◇', path: '/meus-insumos' },
    { label: 'Meus Produtos', icon: '▦', path: '/meus-produtos' },
    { label: 'Meus Colaboradores', icon: 'RH', path: '/meus-colaboradores' },
    { label: 'Calcular Preços', icon: '%', path: '/precificacao' },
    { label: 'Meus Resultados', icon: '↗', path: '/meus-resultados' }
  ];

  private get tip(): SidebarTip {
    const path = this.router.url.split('?')[0];
    return SIDEBAR_TIPS[path] ?? SIDEBAR_TIPS['/dashboard'];
  }

  get tipEyebrow(): string {
    return this.tip.eyebrow;
  }

  get tipTitle(): string {
    return this.tip.title;
  }

  get tipText(): string {
    return this.tip.text;
  }

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

  /** Abre a Central de Ajuda já posicionada na seção da tela atual. */
  openHelp() {
    this.isDropdownOpen = false;
    this.helpCenter?.open(this.router.url.split('?')[0]);
  }

  logout() {
    this.authService.logout();
    void this.router.navigate(['/']);
  }
}
