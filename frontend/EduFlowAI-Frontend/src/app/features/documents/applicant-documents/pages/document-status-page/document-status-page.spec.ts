import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DocumentStatusPage } from './document-status-page';

describe('DocumentStatusPage', () => {
  let component: DocumentStatusPage;
  let fixture: ComponentFixture<DocumentStatusPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentStatusPage],
    }).compileComponents();

    fixture = TestBed.createComponent(DocumentStatusPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
