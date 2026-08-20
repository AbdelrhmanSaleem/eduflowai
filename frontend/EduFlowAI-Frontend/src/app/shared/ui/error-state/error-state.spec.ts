import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ErrorState } from './error-state';

describe('ErrorState', () => {
  let component: ErrorState;
  let fixture: ComponentFixture<ErrorState>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorState],
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorState);
    fixture.componentRef.setInput('title', 'Request failed');
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
