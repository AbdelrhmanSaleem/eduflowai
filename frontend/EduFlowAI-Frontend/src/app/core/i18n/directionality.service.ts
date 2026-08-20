import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable } from '@angular/core';

import { LocaleStore } from './locale.store';

@Injectable({ providedIn: 'root' })
export class DirectionalityService {
  private readonly document = inject(DOCUMENT);
  private readonly locale = inject(LocaleStore);

  constructor() {
    effect(() => {
      const activeLocale = this.locale.locale();
      this.document.documentElement.lang = activeLocale;
      this.document.documentElement.dir =
        activeLocale === 'ar' ? 'rtl' : 'ltr';
    });
  }
}
