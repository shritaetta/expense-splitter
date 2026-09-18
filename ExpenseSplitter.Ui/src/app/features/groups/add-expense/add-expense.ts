import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray, FormGroup } from '@angular/forms';
import { GroupService } from '../../../core/services/group';
import { ExpenseService } from '../../../core/services/expense';
import { SplitTemplateService } from '../../../core/services/split-template';
import { GroupMember, SplitType, CreateExpenseDto, SplitTemplate } from '../../../core/models/models';
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
  private templateService = inject(SplitTemplateService);
  private fb = inject(FormBuilder);

  groupId!: string;
  members: GroupMember[] = [];
  templates: SplitTemplate[] = [];
  splitTypes = [
    { value: SplitType.Equal, label: 'Equal' },
    { value: SplitType.Percentage, label: 'Percentage' },
    { value: SplitType.Exact, label: 'Exact Amount' }
  ];

  expenseForm!: FormGroup;
  private splitTypeSub?: Subscription;
  private templateSub?: Subscription;

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    
    this.expenseForm = this.fb.group({
      description: ['', Validators.required],
      totalAmount: ['', [Validators.required, Validators.min(0.01)]],
      payerId: ['', Validators.required],
      splitType: [SplitType.Equal, Validators.required],
      splitTemplateId: [''],
      participants: this.fb.array([])
    });

    this.groupService.getGroupMembers(this.groupId).subscribe(m => {
      this.members = m;
      if (this.members.length > 0) {
        this.expenseForm.patchValue({ payerId: this.members[0].userId });
      }
      this.buildParticipantsForm();
    });

    this.templateService.getTemplates(this.groupId).subscribe(t => this.templates = t);

    this.splitTypeSub = this.expenseForm.get('splitType')?.valueChanges.subscribe(() => {
      this.recalculate();
    });

    this.templateSub = this.expenseForm.get('splitTemplateId')?.valueChanges.subscribe(templateId => {
      if (this.selectedSplitType === SplitType.Template && templateId) {
        this.applyTemplate(templateId);
      }
    });
  }

  ngOnDestroy() {
    this.splitTypeSub?.unsubscribe();
    this.templateSub?.unsubscribe();
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
        percentage: [''],
        templateShare: ['']
      }));
    });
  }

  applyTemplate(templateId: string) {
    const template = this.templates.find(t => t.id === templateId);
    if (!template) return;

    const arr = this.participantsArray;
    for (let i = 0; i < arr.length; i++) {
      const group = arr.at(i) as FormGroup;
      const userId = group.get('userId')?.value;
      const item = template.items.find(ti => ti.userId === userId);
      
      if (item) {
        group.patchValue({ included: true, templateShare: item.shareValue });
      } else {
        group.patchValue({ included: false, templateShare: '' });
      }
    }
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
      .map((p: any) => {
        let shareValue: number | undefined;
        if (Number(formValue.splitType) === SplitType.Exact) {
          shareValue = p.shareAmount ? Number(p.shareAmount) : undefined;
        } else if (Number(formValue.splitType) === SplitType.Percentage) {
          shareValue = p.percentage ? Number(p.percentage) : undefined;
        } else if (Number(formValue.splitType) === SplitType.Template) {
          shareValue = p.templateShare ? Number(p.templateShare) : undefined;
        }

        return {
          userId: p.userId,
          shareValue: shareValue
        };
      });

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
