import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UploadProgress } from './upload-progress';

describe('UploadProgress', () => {
  let component: UploadProgress;
  let fixture: ComponentFixture<UploadProgress>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UploadProgress],
    }).compileComponents();

    fixture = TestBed.createComponent(UploadProgress);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
