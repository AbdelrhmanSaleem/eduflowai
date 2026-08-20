import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EligibilityOverridePage } from './eligibility-override-page';

describe('EligibilityOverridePage', () => {
  let component: EligibilityOverridePage;
  let fixture: ComponentFixture<EligibilityOverridePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EligibilityOverridePage],
    }).compileComponents();

    fixture = TestBed.createComponent(EligibilityOverridePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
