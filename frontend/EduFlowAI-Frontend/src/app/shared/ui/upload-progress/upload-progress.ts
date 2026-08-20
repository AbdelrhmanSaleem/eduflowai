// shared/ui/upload-progress/upload-progress.component.ts

import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { LucideAngularModule, LoaderCircle } from 'lucide-angular';

@Component({
  selector: 'app-upload-progress',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './upload-progress.html',
  styleUrl: './upload-progress.scss',
})
export class UploadProgress {
  /** Name of the file currently being uploaded, shown next to the spinner. */
  @Input() fileName: string | null = null;
  @Input() label = 'Uploading…';

  protected readonly LoaderIcon = LoaderCircle;
}