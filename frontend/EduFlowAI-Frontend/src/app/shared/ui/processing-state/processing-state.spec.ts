import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProcessingState } from './processing-state';

describe('ProcessingState', () => {
  let component: ProcessingState;
  let fixture: ComponentFixture<ProcessingState>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProcessingState],
    }).compileComponents();

    fixture = TestBed.createComponent(ProcessingState);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
