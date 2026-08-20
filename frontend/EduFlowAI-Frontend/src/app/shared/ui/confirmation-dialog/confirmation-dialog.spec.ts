import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfirmationDialog } from './confirmation-dialog';

describe('ConfirmationDialog', () => {
  let component: ConfirmationDialog;
  let fixture: ComponentFixture<ConfirmationDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmationDialog],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmationDialog);
    fixture.componentRef.setInput('config', {
      title: 'Approve?',
      message: 'Confirm this action.',
      cancelLabel: 'Cancel',
      confirmLabel: 'Confirm',
      submittingLabel: 'Submitting',
    });
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
