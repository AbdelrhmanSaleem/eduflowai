import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { ApplicantLayout } from './applicant-layout';

describe('ApplicantLayout', () => {
  let component: ApplicantLayout;
  let fixture: ComponentFixture<ApplicantLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicantLayout],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicantLayout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    const element = fixture.nativeElement as HTMLElement;
    expect(component).toBeTruthy();
    expect(element.querySelector<HTMLImageElement>('.brand__logo img')?.src).toContain(
      '/edu-logo-dark.png',
    );
  });
});
