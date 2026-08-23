import { Directive, ElementRef, HostListener, Renderer2, RendererStyleFlags2, inject } from '@angular/core';

/**
 * Acende um brilho que segue o cursor sobre o elemento host, via
 * variáveis CSS (--spot-x/--spot-y/--spot-opacity). O host define
 * o visual (gradiente, blur); a diretiva só rastreia o ponteiro.
 *
 * Renderer2.setStyle atribui `el.style[prop] = value` por padrão, o
 * que não funciona para custom properties (--foo) — precisam do flag
 * DashCase para cair em `el.style.setProperty(prop, value)`.
 */
@Directive({
  selector: '[appSpotlight]',
  standalone: true
})
export class SpotlightDirective {
  private readonly el = inject(ElementRef<HTMLElement>);
  private readonly renderer = inject(Renderer2);

  private setVar(name: string, value: string) {
    this.renderer.setStyle(this.el.nativeElement, name, value, RendererStyleFlags2.DashCase);
  }

  @HostListener('pointermove', ['$event'])
  onPointerMove(event: PointerEvent) {
    const rect = this.el.nativeElement.getBoundingClientRect();
    this.setVar('--spot-x', `${event.clientX - rect.left}px`);
    this.setVar('--spot-y', `${event.clientY - rect.top}px`);
  }

  @HostListener('pointerenter')
  onPointerEnter() {
    this.setVar('--spot-opacity', '1');
  }

  @HostListener('pointerleave')
  onPointerLeave() {
    this.setVar('--spot-opacity', '0');
  }
}
