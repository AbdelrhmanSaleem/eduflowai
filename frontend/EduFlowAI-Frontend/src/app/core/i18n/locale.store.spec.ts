import { TestBed } from '@angular/core/testing';

import { LocaleStore } from './locale.store';

describe('LocaleStore', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
  });

  it('updates and persists the document language and direction', () => {
    const locale = TestBed.inject(LocaleStore);

    locale.setLocale('ar');

    expect(locale.locale()).toBe('ar');
    expect(locale.isRtl()).toBe(true);
    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
    expect(localStorage.getItem('eduflow.locale')).toBe('ar');
    expect(locale.t('common.email')).toBe('البريد الإلكتروني');
  });

  it('toggles back to English immediately', () => {
    const locale = TestBed.inject(LocaleStore);
    locale.setLocale('ar');

    locale.toggle();

    expect(locale.locale()).toBe('en');
    expect(document.documentElement.lang).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
  });
});
