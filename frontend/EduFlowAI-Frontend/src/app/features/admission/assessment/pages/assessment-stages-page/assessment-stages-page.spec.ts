import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssessmentStagesPage } from './assessment-stages-page';

describe('AssessmentStagesPage', () => {
  let component: AssessmentStagesPage;
  let fixture: ComponentFixture<AssessmentStagesPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssessmentStagesPage],
    }).compileComponents();

    fixture = TestBed.createComponent(AssessmentStagesPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
