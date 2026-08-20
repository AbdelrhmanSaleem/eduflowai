import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AdminLayout } from './admin-layout';

describe('AdminLayout', () => {
  let component: AdminLayout;
  let fixture: ComponentFixture<AdminLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLayout],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminLayout);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(component).toBeTruthy();
    expect(element.querySelector<HTMLImageElement>('.brand__logo img')?.src).toContain(
      '/edu-logo-dark.png',
    );
  });
});
