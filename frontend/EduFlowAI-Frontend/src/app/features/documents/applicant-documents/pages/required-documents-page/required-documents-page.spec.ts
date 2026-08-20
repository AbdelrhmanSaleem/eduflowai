import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RequiredDocumentsPage } from './required-documents-page';

describe('RequiredDocumentsPage', () => {
  let component: RequiredDocumentsPage;
  let fixture: ComponentFixture<RequiredDocumentsPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RequiredDocumentsPage],
    }).compileComponents();

    fixture = TestBed.createComponent(RequiredDocumentsPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
