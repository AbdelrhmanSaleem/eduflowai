import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthSessionStore } from '../../auth/auth-session.store';
import { LocaleStore } from '../../i18n/locale.store';

@Component({
  selector: 'app-operations-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './operations-layout.html',
  styleUrl: './operations-layout.scss',
})
export class OperationsLayout {
  protected readonly locale = inject(LocaleStore);
  protected readonly session = inject(AuthSessionStore);
  protected readonly menuOpen = signal(false);
  private readonly router = inject(Router);

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected logout(): void {
    this.session.clear();
    void this.router.navigate(['/auth/login']);
  }
}
