import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StageStepper } from './stage-stepper';

describe('StageStepper', () => {
  let component: StageStepper;
  let fixture: ComponentFixture<StageStepper>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StageStepper],
    }).compileComponents();

    fixture = TestBed.createComponent(StageStepper);
    fixture.componentRef.setInput('englishStatus', 'Pending');
    fixture.componentRef.setInput('iqStatus', 'Pending');
    fixture.componentRef.setInput('technicalStatus', 'Pending');
    fixture.componentRef.setInput('interviewStatus', 'Pending');
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
