import { Component, inject } from '@angular/core';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';

import { AuthSessionStore } from '../../auth/auth-session.store';
import { LocaleStore } from '../../i18n/locale.store';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
})
export class AdminLayout {
  protected readonly locale = inject(LocaleStore);
  protected readonly session = inject(AuthSessionStore);
  private readonly router = inject(Router);

  protected text(english: string, arabic: string): string {
    return this.locale.locale() === 'ar' ? arabic : english;
  }

  protected logout(): void {
    this.session.clear();
    void this.router.navigate(['/auth/login']);
  }
}
