import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-document-upload-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './document-upload-management.component.html',
  styleUrl: './document-upload-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocumentUploadManagementComponent {
  // Input from the parent status page
  applicationId = input.required<string>();
  
  // Mode to determine if we are just showing rejections or doing a full initial upload
  mode = input<'initial' | 'revision'>('revision');
}