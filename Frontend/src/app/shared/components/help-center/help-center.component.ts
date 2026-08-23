import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, OnDestroy, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HELP_SECTIONS, HelpGroup, HelpSection } from './help-center.content';

/** Resultado de busca: a seção e apenas os grupos/campos que casaram. */
interface HelpSearchResult {
  section: HelpSection;
  groups: HelpGroup[];
  matches: number;
}

/**
 * Central de Ajuda — pop-up com a explicação dos campos de cada tela.
 *
 * É aberto pelo botão "?" da topbar (app-shell), que já seleciona a
 * seção correspondente à tela em que o usuário está. O conteúdo vive
 * em `help-center.content.ts`; aqui ficam só navegação e busca.
 */
@Component({
  selector: 'app-help-center',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './help-center.component.html',
  styleUrl: './help-center.component.scss'
})
export class HelpCenterComponent implements OnDestroy {
  @ViewChild('searchInput') private searchInput?: ElementRef<HTMLInputElement>;
  @ViewChild('panel') private panel?: ElementRef<HTMLElement>;

  readonly sections = HELP_SECTIONS;

  isOpen = false;
  activeId = HELP_SECTIONS[0].id;
  search = '';

  private pressedOnBackdrop = false;

  /* ===================== ABRIR / FECHAR ===================== */

  /**
   * Abre o pop-up. `reference` aceita o id da seção ou a rota da tela
   * atual — assim o botão da topbar já entra na seção certa.
   */
  open(reference?: string) {
    const match = reference
      ? this.sections.find(section => section.id === reference || section.route === reference)
      : undefined;

    if (match) this.activeId = match.id;

    this.search = '';
    this.isOpen = true;
    document.body.style.overflow = 'hidden';

    setTimeout(() => this.searchInput?.nativeElement.focus(), 60);
  }

  close() {
    this.isOpen = false;
    document.body.style.overflow = '';
  }

  /**
   * Só fecha quando o clique começa E termina no fundo escuro: evita
   * fechar sem querer ao soltar o mouse fora depois de selecionar um
   * texto dentro do pop-up.
   */
  onBackdropMouseDown(event: MouseEvent) {
    this.pressedOnBackdrop = event.target === event.currentTarget;
  }

  onBackdropClick(event: MouseEvent) {
    const shouldClose = this.pressedOnBackdrop && event.target === event.currentTarget;
    this.pressedOnBackdrop = false;
    if (shouldClose) this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    if (this.isOpen) this.close();
  }

  ngOnDestroy() {
    document.body.style.overflow = '';
  }

  /* ===================== NAVEGAÇÃO ===================== */

  selectSection(id: string) {
    this.activeId = id;
    this.search = '';
    this.panel?.nativeElement.scrollTo({ top: 0, behavior: 'smooth' });
  }

  get activeSection(): HelpSection {
    return this.sections.find(section => section.id === this.activeId) ?? this.sections[0];
  }

  fieldCount(section: HelpSection): number {
    return section.groups.reduce((total, group) => total + group.fields.length, 0);
  }

  countLabel(section: HelpSection): string {
    const total = this.fieldCount(section);
    return `${total} ${total === 1 ? 'campo' : 'campos'}`;
  }

  tagClass(tag: string): string {
    if (tag === 'Obrigatório') return 'required';
    if (tag === 'Automático') return 'auto';
    if (tag === 'Ajustável') return 'tunable';
    return 'optional';
  }

  /* ===================== BUSCA ===================== */

  /** Compara ignorando acentos e maiúsculas — "orcamento" acha "orçamento". */
  private normalize(value: string): string {
    return value
      .toLowerCase()
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '');
  }

  get isSearching(): boolean {
    return this.search.trim().length >= 2;
  }

  /** Percorre todas as telas e devolve só os campos que casam com o termo. */
  get searchResults(): HelpSearchResult[] {
    const query = this.normalize(this.search.trim());

    return this.sections
      .map(section => {
        const groups = section.groups
          .map(group => ({
            title: group.title,
            fields: group.fields.filter(field =>
              this.normalize(`${field.label} ${field.description} ${field.example ?? ''}`).includes(query)
            )
          }))
          .filter(group => group.fields.length > 0);

        return {
          section,
          groups,
          matches: groups.reduce((total, group) => total + group.fields.length, 0)
        };
      })
      .filter(result => result.matches > 0);
  }

  get totalMatches(): number {
    return this.searchResults.reduce((total, result) => total + result.matches, 0);
  }

  get searchLabel(): string {
    const total = this.totalMatches;
    if (!total) return 'Nenhum campo encontrado';
    return `${total} ${total === 1 ? 'campo encontrado' : 'campos encontrados'}`;
  }

  clearSearch() {
    this.search = '';
    this.searchInput?.nativeElement.focus();
  }
}
