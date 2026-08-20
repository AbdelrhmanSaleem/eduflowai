import { A11yModule } from '@angular/cdk/a11y';
import { Component, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';

export interface ReasonDialogConfig {
  title: string;
  explanation: string;
  label: string;
  requiredLabel: string;
  placeholder: string;
  helper: string;
  quickInsertLabel: string;
  quickReasons: readonly string[];
  cancelLabel: string;
  confirmLabel: string;
  submittingLabel: string;
  requiredError: string;
  lengthError: string;
  variant: 'danger' | 'primary';
}

@Component({
  selector: 'app-reason-dialog',
  imports: [A11yModule, ReactiveFormsModule],
  templateUrl: './reason-dialog.html',
  styleUrl: './reason-dialog.scss',
})
export class ReasonDialog {
  readonly config = input.required<ReasonDialogConfig>();
  readonly submitting = input(false);
  readonly errorMessage = input('');
  readonly cancelled = output<void>();
  readonly confirmed = output<string>();

  protected readonly reason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(10), Validators.maxLength(1000)],
  });

  protected cancel(): void {
    if (!this.submitting()) {
      this.cancelled.emit();
    }
  }

  protected insertQuickReason(value: string): void {
    const current = this.reason.value.trimEnd();
    const separator = current ? (current.endsWith('.') ? ' ' : '. ') : '';
    const next = `${current}${separator}${value}`.slice(0, 1000);
    this.reason.setValue(next);
    this.reason.markAsDirty();
  }

  protected submit(event: Event): void {
    event.preventDefault();
    this.reason.markAsTouched();
    if (this.reason.invalid || this.submitting()) {
      return;
    }

    this.confirmed.emit(this.reason.value.trim());
  }

  protected stopPropagation(event: Event): void {
    event.stopPropagation();
  }
}
