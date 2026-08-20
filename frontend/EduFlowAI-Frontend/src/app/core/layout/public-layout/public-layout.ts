import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { LocaleStore } from '../../i18n/locale.store';

@Component({
  selector: 'app-public-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './public-layout.html',
  styleUrl: './public-layout.scss',
})
export class PublicLayout {
  protected readonly locale = inject(LocaleStore);

  protected text(english: string, arabic: string): string {
    return this.locale.locale() === 'ar' ? arabic : english;
  }
}
