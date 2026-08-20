import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SecureFileViewer } from './secure-file-viewer';

describe('SecureFileViewer', () => {
  let component: SecureFileViewer;
  let fixture: ComponentFixture<SecureFileViewer>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SecureFileViewer],
    }).compileComponents();

    fixture = TestBed.createComponent(SecureFileViewer);
    fixture.componentRef.setInput('sourceUrl', 'blob:test');
    fixture.componentRef.setInput('mimeType', 'image/png');
    fixture.componentRef.setInput('fileName', 'document.png');
    fixture.componentRef.setInput('labels', {
      zoomIn: 'Zoom in',
      zoomOut: 'Zoom out',
      rotate: 'Rotate',
      reset: 'Reset',
      download: 'Download',
      documentPreview: 'Document preview',
      unsupported: 'Unsupported',
    });
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
