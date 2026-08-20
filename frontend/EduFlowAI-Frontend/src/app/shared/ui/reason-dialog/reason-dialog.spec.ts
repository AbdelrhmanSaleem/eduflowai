import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { ReasonDialog, ReasonDialogConfig } from './reason-dialog';

const config: ReasonDialogConfig = {
  title: 'Reason',
  explanation: 'Explain the action.',
  label: 'Reason',
  requiredLabel: 'Required',
  placeholder: 'Enter a reason',
  helper: 'Be specific.',
  quickInsertLabel: 'Quick insert',
  quickReasons: ['Missing pages'],
  cancelLabel: 'Cancel',
  confirmLabel: 'Confirm',
  submittingLabel: 'Submitting',
  requiredError: 'Required',
  lengthError: 'Invalid length',
  variant: 'danger',
};

describe('ReasonDialog', () => {
  let component: ReasonDialog;
  let fixture: ComponentFixture<ReasonDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReasonDialog],
    }).compileComponents();

    fixture = TestBed.createComponent(ReasonDialog);
    fixture.componentRef.setInput('config', config);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('appends a quick reason without replacing entered text', () => {
    const instance = component as any;
    instance.reason.setValue('The uploaded document');
    instance.insertQuickReason('Missing pages');
    expect(instance.reason.value).toBe('The uploaded document. Missing pages');
  });

  it('emits the entered reason when the native form is submitted', () => {
    const confirmed = vi.fn();
    component.confirmed.subscribe(confirmed);
    const instance = component as any;
    instance.reason.setValue('The uploaded document is too blurry.');

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));

    expect(confirmed).toHaveBeenCalledWith('The uploaded document is too blurry.');
  });
});
