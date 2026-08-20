import { A11yModule } from '@angular/cdk/a11y';
import { Component, input, output } from '@angular/core';

export interface ConfirmationDialogConfig {
  title: string;
  message: string;
  cancelLabel: string;
  confirmLabel: string;
  submittingLabel: string;
}

@Component({
  selector: 'app-confirmation-dialog',
  imports: [A11yModule],
  templateUrl: './confirmation-dialog.html',
  styleUrl: './confirmation-dialog.scss',
})
export class ConfirmationDialog {
  readonly config = input.required<ConfirmationDialogConfig>();
  readonly submitting = input(false);
  readonly errorMessage = input('');
  readonly cancelled = output<void>();
  readonly confirmed = output<void>();

  protected cancel(): void {
    if (!this.submitting()) {
      this.cancelled.emit();
    }
  }

  protected stopPropagation(event: Event): void {
    event.stopPropagation();
  }
}
