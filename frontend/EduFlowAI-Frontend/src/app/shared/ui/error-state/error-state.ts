import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  templateUrl: './error-state.html',
  styleUrl: './error-state.scss',
})
export class ErrorState {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
  readonly traceId = input<string | null>(null);
  readonly actionLabel = input<string | null>(null);
  readonly action = output<void>();
}
