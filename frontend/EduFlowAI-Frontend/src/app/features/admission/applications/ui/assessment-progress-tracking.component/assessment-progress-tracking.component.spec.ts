import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssessmentProgressTrackingComponent } from './assessment-progress-tracking.component';

describe('AssessmentProgressTrackingComponent', () => {
  let component: AssessmentProgressTrackingComponent;
  let fixture: ComponentFixture<AssessmentProgressTrackingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssessmentProgressTrackingComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(AssessmentProgressTrackingComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
