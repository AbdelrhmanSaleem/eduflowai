import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EligibilityResultComponent } from './eligibility-result.component';

describe('EligibilityResultComponent', () => {
  let component: EligibilityResultComponent;
  let fixture: ComponentFixture<EligibilityResultComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EligibilityResultComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(EligibilityResultComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
