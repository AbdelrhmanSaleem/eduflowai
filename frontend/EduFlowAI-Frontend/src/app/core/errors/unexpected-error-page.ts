import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { LocaleStore } from '../i18n/locale.store';

@Component({
  selector: 'app-unexpected-error-page',
  imports: [RouterLink],
  template: `
    <section class="error-page" aria-labelledby="unexpected-title">
      <span class="error-page__icon material-symbols-outlined" aria-hidden="true">
        error
      </span>
      <h1 id="unexpected-title">
        {{ locale.t('error.unexpected.title') }}
      </h1>
      <p>{{ locale.t('error.unexpected.description') }}</p>
      <div>
        <button type="button" (click)="retry()">
          {{ locale.t('common.retry') }}
        </button>
        <a routerLink="/auth/login">
          {{ locale.t('error.notFound.action') }}
        </a>
      </div>
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
    .error-page__icon {
      display: grid;
      width: 4rem;
      height: 4rem;
      place-items: center;
      border-radius: 999px;
      background: #ffdad6;
      color: #93000a;
      font-size: 2rem;
    }
    h1, p { margin: 0; }
    h1 { color: #022448; font-size: 1.5rem; }
    p { max-width: 28rem; color: #505f76; line-height: 1.6; }
    div { display: flex; flex-wrap: wrap; justify-content: center; gap: .75rem; }
    button, a {
      display: inline-flex;
      min-height: 2.75rem;
      align-items: center;
      justify-content: center;
      border: 1px solid #022448;
      border-radius: .25rem;
      padding-inline: 1rem;
      font: inherit;
      font-weight: 600;
      cursor: pointer;
    }
    button { background: #022448; color: #fff; }
    a { background: #fff; color: #022448; text-decoration: none; }
  `,
})
export class UnexpectedErrorPage {
  protected readonly locale = inject(LocaleStore);
  private readonly router = inject(Router);

  protected retry(): void {
    void this.router.navigateByUrl('/');
  }
}
