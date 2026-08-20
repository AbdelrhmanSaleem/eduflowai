import { Component, input } from '@angular/core';

@Component({
  selector: 'app-validation-errors',
  templateUrl: './validation-errors.html',
  styleUrl: './validation-errors.scss',
})
export class ValidationErrors {
  readonly title = input<string | null>(null);
  readonly errors = input<readonly string[]>([]);
}
