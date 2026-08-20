import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VerificationHistoryPage } from './verification-history-page';

describe('VerificationHistoryPage', () => {
  let component: VerificationHistoryPage;
  let fixture: ComponentFixture<VerificationHistoryPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VerificationHistoryPage],
    }).compileComponents();

    fixture = TestBed.createComponent(VerificationHistoryPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
