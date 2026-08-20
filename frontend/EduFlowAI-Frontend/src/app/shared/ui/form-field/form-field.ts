import { Component, input } from '@angular/core';

@Component({
  selector: 'app-form-field',
  templateUrl: './form-field.html',
  styleUrl: './form-field.scss',
})
export class FormField {
  readonly label = input.required<string>();
  readonly forId = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly errors = input<readonly string[]>([]);
  readonly required = input(false);
}
