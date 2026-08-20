import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KnowledgeBasePage } from './knowledge-base-page';

describe('KnowledgeBasePage', () => {
  let component: KnowledgeBasePage;
  let fixture: ComponentFixture<KnowledgeBasePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KnowledgeBasePage],
    }).compileComponents();

    fixture = TestBed.createComponent(KnowledgeBasePage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
