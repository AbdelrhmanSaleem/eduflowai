import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { LocaleStore } from '../i18n/locale.store';

@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink],
  template: `
    <section class="error-page" aria-labelledby="not-found-title">
      <span class="error-page__code" aria-hidden="true">404</span>
      <h1 id="not-found-title">{{ locale.t('error.notFound.title') }}</h1>
      <p>{{ locale.t('error.notFound.description') }}</p>
      <a routerLink="/auth/login">
        <span class="material-symbols-outlined" aria-hidden="true">
          arrow_back
        </span>
        {{ locale.t('error.notFound.action') }}
      </a>
    </section>
  `,
  styles: `
    .error-page {
      display: grid;
      width: min(100%, 34rem);
      margin-inline: auto;
      justify-items: center;
      gap: 1rem;
      border: 1px solid #e0e2e5;
      border-radius: .75rem;
      background: #fff;
      padding: clamp(2rem, 8vw, 4.5rem) 1.5rem;
      box-shadow: 0 1rem 2.5rem rgb(2 36 72 / 8%);
      text-align: center;
    }
    .error-page__code {
      color: #d0e1fb;
      font-size: clamp(4rem, 16vw, 7rem);
      font-weight: 700;
      line-height: .85;
    }
    h1, p { margin: 0; }
    h1 { color: #022448; font-size: 1.5rem; }
    p { max-width: 28rem; color: #505f76; line-height: 1.6; }
    a {
      display: inline-flex;
      min-height: 2.75rem;
      align-items: center;
      gap: .5rem;
      border-radius: .25rem;
      background: #022448;
      padding-inline: 1rem;
      color: #fff;
      font-weight: 600;
      text-decoration: none;
    }
    [dir='rtl'] a .material-symbols-outlined { transform: rotate(180deg); }
  `,
})
export class NotFoundPage {
  protected readonly locale = inject(LocaleStore);
}
