import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ApplicantDocumentViewerPage } from './applicant-document-viewer-page';

describe('ApplicantDocumentViewerPage', () => {
  let component: ApplicantDocumentViewerPage;
  let fixture: ComponentFixture<ApplicantDocumentViewerPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ApplicantDocumentViewerPage],
    }).compileComponents();

    fixture = TestBed.createComponent(ApplicantDocumentViewerPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
