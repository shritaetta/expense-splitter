import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, SlicePipe } from '@angular/common';
import { GroupService } from '../../../core/services/group';
import { SplitTemplateService } from '../../../core/services/split-template';
import { RecurringExpenseService } from '../../../core/services/recurring-expense';
import { GroupMember, SplitTemplate, SplitType, Frequency, CreateRecurringExpenseDto, ExpenseDto } from '../../../core/models/models';

@Component({
  selector: 'app-recurring-expenses',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DatePipe, SlicePipe],
  templateUrl: './recurring-expenses.html',
  styleUrls: ['./recurring-expenses.css']
})
export class RecurringExpenses implements OnInit {
  private route = inject(ActivatedRoute);
  private groupService = inject(GroupService);
  private templateService = inject(SplitTemplateService);
  private recurringService = inject(RecurringExpenseService);
  private fb = inject(FormBuilder);

  groupId!: string;
  members: GroupMember[] = [];
  templates: SplitTemplate[] = [];
  
  previewExpense: ExpenseDto | null = null;
  createdId: string | null = null;

  form = this.fb.group({
    description: ['', Validators.required],
    totalAmount: ['', [Validators.required, Validators.min(0.01)]],
    category: ['General', Validators.required],
    frequency: [Frequency.Monthly, Validators.required],
    startDate: ['', Validators.required],
    payerId: ['', Validators.required],
    splitTypeId: [SplitType.Equal, Validators.required],
    splitTemplateId: ['']
  });

  get SplitType() { return SplitType; }
  get Frequency() { return Frequency; }

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    this.groupService.getGroupMembers(this.groupId).subscribe(m => {
      this.members = m;
      if (m.length > 0) this.form.patchValue({ payerId: m[0].userId });
    });
    this.templateService.getTemplates(this.groupId).subscribe(t => this.templates = t);
    
    // Default start date to today
    const today = new Date().toISOString().substring(0, 10);
    this.form.patchValue({ startDate: today });
  }

  onSubmit() {
    if (this.form.invalid) return;
    
    const v = this.form.value;
    
    if (Number(v.splitTypeId) === SplitType.Template && !v.splitTemplateId) {
      alert('Please select a template');
      return;
    }

    const dto: CreateRecurringExpenseDto = {
      payerId: v.payerId!,
      description: v.description!,
      totalAmount: Number(v.totalAmount),
      category: v.category!,
      frequency: Number(v.frequency),
      startDate: new Date(v.startDate!).toISOString(),
      splitTypeId: Number(v.splitTypeId),
      splitTemplateId: v.splitTemplateId || undefined
    };

    this.recurringService.createRecurringExpense(this.groupId, dto).subscribe({
      next: (res) => {
        this.createdId = res.id;
        this.loadPreview();
      },
      error: (err) => alert(err.error?.message || 'Failed to setup recurring bill')
    });
  }

  loadPreview() {
    if (this.createdId) {
      this.recurringService.previewNextCycle(this.groupId, this.createdId).subscribe({
        next: (preview) => this.previewExpense = preview,
        error: (err) => console.error(err)
      });
    }
  }
}
