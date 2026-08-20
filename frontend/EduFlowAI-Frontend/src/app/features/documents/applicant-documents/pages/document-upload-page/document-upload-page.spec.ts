import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentUploadPage } from './document-upload-page';

describe('DocumentUploadPage', () => {
  let component: DocumentUploadPage;
  let fixture: ComponentFixture<DocumentUploadPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentUploadPage],
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentUploadPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
