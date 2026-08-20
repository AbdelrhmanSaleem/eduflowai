import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OperationsManagerListPage } from './operations-manager-list-page';

describe('OperationsManagerListPage', () => {
  let component: OperationsManagerListPage;
  let fixture: ComponentFixture<OperationsManagerListPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OperationsManagerListPage],
    }).compileComponents();

    fixture = TestBed.createComponent(OperationsManagerListPage);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
