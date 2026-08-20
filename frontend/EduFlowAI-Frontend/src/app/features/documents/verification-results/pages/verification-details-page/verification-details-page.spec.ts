import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VerificationDetailsPage } from './verification-details-page';

describe('VerificationDetailsPage', () => {
  let component: VerificationDetailsPage;
  let fixture: ComponentFixture<VerificationDetailsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerificationDetailsPage],
    }).compileComponents();

    fixture = TestBed.createComponent(VerificationDetailsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
