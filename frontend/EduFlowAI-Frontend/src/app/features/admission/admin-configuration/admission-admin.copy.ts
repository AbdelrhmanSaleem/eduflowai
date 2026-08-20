import { Injectable, inject } from '@angular/core';

import { LocaleStore } from '../../../core/i18n/locale.store';

@Injectable({ providedIn: 'root' })
export class AdmissionAdminCopy {
  private readonly locale = inject(LocaleStore);

  readonly isRtl = this.locale.isRtl;

  text(english: string, arabic: string): string {
    return this.locale.locale() === 'ar' ? arabic : english;
  }
}
