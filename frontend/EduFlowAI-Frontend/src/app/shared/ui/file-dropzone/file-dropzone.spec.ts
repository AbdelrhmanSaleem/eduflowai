import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileDropzone } from './file-dropzone';

describe('FileDropzone', () => {
  let component: FileDropzone;
  let fixture: ComponentFixture<FileDropzone>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FileDropzone],
    }).compileComponents();

    fixture = TestBed.createComponent(FileDropzone);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
