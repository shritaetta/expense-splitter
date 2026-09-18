import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AddExpense } from './add-expense';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { GroupService } from '../../../core/services/group';
import { ExpenseService } from '../../../core/services/expense';
import { SplitTemplateService } from '../../../core/services/split-template';
import { SplitType } from '../../../core/models/models';
import { of } from 'rxjs';

describe('AddExpense Component', () => {
  let component: AddExpense;
  let fixture: ComponentFixture<AddExpense>;

  beforeEach(async () => {
    const groupServiceSpy = jasmine.createSpyObj('GroupService', ['getGroupMembers']);
    const expenseServiceSpy = jasmine.createSpyObj('ExpenseService', ['createExpense']);
    const splitTemplateServiceSpy = jasmine.createSpyObj('SplitTemplateService', ['getGroupTemplates']);

    // Mock data
    groupServiceSpy.getGroupMembers.and.returnValue(of([
      { userId: 'user1', userName: 'Alice' },
      { userId: 'user2', userName: 'Bob' },
      { userId: 'user3', userName: 'Charlie' }
    ]));
    splitTemplateServiceSpy.getGroupTemplates.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AddExpense, ReactiveFormsModule, RouterTestingModule, HttpClientTestingModule],
      providers: [
        { provide: GroupService, useValue: groupServiceSpy },
        { provide: ExpenseService, useValue: expenseServiceSpy },
        { provide: SplitTemplateService, useValue: splitTemplateServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AddExpense);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize with all participants included by default', () => {
    expect(component.participantsArray.length).toBe(3);
    expect(component.participantsArray.at(0).get('included')?.value).toBeTrue();
    expect(component.participantsArray.at(1).get('included')?.value).toBeTrue();
    expect(component.participantsArray.at(2).get('included')?.value).toBeTrue();
  });

  it('should correctly exclude participant from payload when unchecked', () => {
    // Uncheck Bob
    component.participantsArray.at(1).get('included')?.setValue(false);
    
    // Set split type to equal
    component.expenseForm.patchValue({ splitType: SplitType.Equal });
    
    // Get payload
    const payload = component['buildExpensePayload']();
    
    // Bob should not be in the participants payload
    expect(payload.participants.length).toBe(2);
    expect(payload.participants.find((p: any) => p.userId === 'user2')).toBeUndefined();
    expect(payload.participants.find((p: any) => p.userId === 'user1')).toBeDefined();
    expect(payload.participants.find((p: any) => p.userId === 'user3')).toBeDefined();
  });
});
