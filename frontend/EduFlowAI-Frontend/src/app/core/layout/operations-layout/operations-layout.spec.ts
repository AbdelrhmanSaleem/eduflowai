import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { OperationsLayout } from './operations-layout';

describe('OperationsLayout', () => {
  it('creates the staff layout', async () => {
    await TestBed.configureTestingModule({
      imports: [OperationsLayout],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(OperationsLayout);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(fixture.componentInstance).toBeTruthy();
    expect(element.querySelector<HTMLImageElement>('.brand__logo img')?.src).toContain(
      '/edu-logo-dark.png',
    );
    expect(element.querySelector('[data-theme]')).toBeNull();
    expect(element.textContent).not.toContain('dark_mode');
  });
});
