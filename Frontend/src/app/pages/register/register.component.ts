import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-register',
  imports: [RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private readonly router = inject(Router);

  onSubmit(event: Event) {
    event.preventDefault();
    void this.router.navigate(['/dashboard']);
  }
}
