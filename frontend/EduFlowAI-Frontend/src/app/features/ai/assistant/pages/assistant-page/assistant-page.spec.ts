import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AssistantPage } from './assistant-page';

describe('AssistantPage', () => {
  let component: AssistantPage;
  let fixture: ComponentFixture<AssistantPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AssistantPage],
    }).compileComponents();

    fixture = TestBed.createComponent(AssistantPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
