import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray, FormGroup } from '@angular/forms';
import { GroupService } from '../../../core/services/group';
import { ExpenseService } from '../../../core/services/expense';
import { GroupMember, SplitType, CreateExpenseDto } from '../../../core/models/models';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-add-expense',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './add-expense.html',
  styleUrls: ['./add-expense.css']
})
export class AddExpense implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private groupService = inject(GroupService);
  private expenseService = inject(ExpenseService);
  private fb = inject(FormBuilder);

  groupId!: string;
  members: GroupMember[] = [];
  splitTypes = [
    { value: SplitType.Equal, label: 'Equal' },
    { value: SplitType.Percentage, label: 'Percentage' },
    { value: SplitType.Exact, label: 'Exact Amount' }
  ];

  expenseForm!: FormGroup;
  private splitTypeSub?: Subscription;

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    
    this.expenseForm = this.fb.group({
      description: ['', Validators.required],
      totalAmount: ['', [Validators.required, Validators.min(0.01)]],
      payerId: ['', Validators.required],
      splitType: [SplitType.Equal, Validators.required],
      participants: this.fb.array([])
    });

    this.groupService.getGroupMembers(this.groupId).subscribe(m => {
      this.members = m;
      if (this.members.length > 0) {
        this.expenseForm.patchValue({ payerId: this.members[0].userId });
      }
      this.buildParticipantsForm();
    });

    this.splitTypeSub = this.expenseForm.get('splitType')?.valueChanges.subscribe(() => {
      // Trigger change detection for dynamic fields, reset values if needed
      this.recalculate();
    });
  }

  ngOnDestroy() {
    this.splitTypeSub?.unsubscribe();
  }

  get participantsArray() {
    return this.expenseForm.get('participants') as FormArray;
  }

  get selectedSplitType(): SplitType {
    return Number(this.expenseForm.get('splitType')?.value);
  }

  get SplitType() {
    return SplitType;
  }

  buildParticipantsForm() {
    const arr = this.participantsArray;
    arr.clear();
    // Default: all members included
    this.members.forEach(member => {
      arr.push(this.fb.group({
        userId: [member.userId],
        userName: [member.userName],
        included: [true], // Pre-checked by default
        shareAmount: [''],
        percentage: ['']
      }));
    });
  }

  recalculate() {
    // Utility hook for future auto-balancing logic if desired
  }

  onSubmit() {
    if (this.expenseForm.invalid) return;

    const formValue = this.expenseForm.value;
    
    // Filter out unchecked members, map to DTO format
    const activeParticipants = formValue.participants
      .filter((p: any) => p.included)
      .map((p: any) => ({
        userId: p.userId,
        shareAmount: p.shareAmount ? Number(p.shareAmount) : undefined,
        percentage: p.percentage ? Number(p.percentage) : undefined
      }));

    const dto: CreateExpenseDto = {
      groupId: this.groupId,
      description: formValue.description,
      totalAmount: Number(formValue.totalAmount),
      payerId: formValue.payerId,
      splitType: Number(formValue.splitType),
      participants: activeParticipants
    };

    this.expenseService.createExpense(dto).subscribe({
      next: () => this.router.navigate(['/groups', this.groupId]),
      error: (err) => alert(err.error?.message || 'Failed to create expense')
    });
  }
}
