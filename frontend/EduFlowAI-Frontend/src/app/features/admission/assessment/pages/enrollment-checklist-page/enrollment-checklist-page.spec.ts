import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EnrollmentChecklistPage } from './enrollment-checklist-page';

describe('EnrollmentChecklistPage', () => {
  let component: EnrollmentChecklistPage;
  let fixture: ComponentFixture<EnrollmentChecklistPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EnrollmentChecklistPage],
    }).compileComponents();

    fixture = TestBed.createComponent(EnrollmentChecklistPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
