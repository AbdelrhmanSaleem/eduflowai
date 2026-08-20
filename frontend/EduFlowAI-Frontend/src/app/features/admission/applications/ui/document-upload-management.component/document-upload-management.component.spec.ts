import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentUploadManagementComponent } from './document-upload-management.component';

describe('DocumentUploadManagementComponent', () => {
  let component: DocumentUploadManagementComponent;
  let fixture: ComponentFixture<DocumentUploadManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentUploadManagementComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentUploadManagementComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
