import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.scss'
})
export class LandingComponent implements AfterViewInit, OnDestroy {
  @ViewChild('page', { static: true }) private readonly pageRef!: ElementRef<HTMLElement>;

  private observer?: IntersectionObserver;

  ngAfterViewInit() {
    const targets = this.pageRef.nativeElement.querySelectorAll('.reveal');

    if (!('IntersectionObserver' in window)) {
      targets.forEach((target) => target.classList.add('is-visible'));
      return;
    }

    // IntersectionObserver evita custo de scroll-listener e reflow contínuo.
    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            entry.target.classList.add('is-visible');
            this.observer?.unobserve(entry.target);
          }
        }
      },
      { threshold: 0.15, rootMargin: '0px 0px -60px 0px' }
    );

    targets.forEach((target) => this.observer?.observe(target));
  }

  ngOnDestroy() {
    this.observer?.disconnect();
  }
}
