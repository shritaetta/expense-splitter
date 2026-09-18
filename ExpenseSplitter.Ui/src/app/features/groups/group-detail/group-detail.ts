import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { GroupService } from '../../../core/services/group';
import { ExpenseService } from '../../../core/services/expense';
import { Group, GroupMember, ExpenseDto } from '../../../core/models/models';

@Component({
  selector: 'app-group-detail',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DatePipe],
  templateUrl: './group-detail.html',
  styleUrls: ['./group-detail.css']
})
export class GroupDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private groupService = inject(GroupService);
  private expenseService = inject(ExpenseService);
  private fb = inject(FormBuilder);

  groupId!: string;
  group: Group | null = null;
  members: GroupMember[] = [];
  expenses: ExpenseDto[] = [];
  
  addMemberForm = this.fb.group({
    userId: ['', Validators.required]
  });

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    if (this.groupId) {
      this.loadGroupData();
    }
  }

  loadGroupData() {
    this.groupService.getGroup(this.groupId).subscribe(g => this.group = g);
    this.groupService.getGroupMembers(this.groupId).subscribe(m => this.members = m);
    this.expenseService.getGroupExpenses(this.groupId).subscribe(e => this.expenses = e);
  }

  onAddMember() {
    if (this.addMemberForm.valid) {
      this.groupService.addMember(this.groupId, this.addMemberForm.value.userId!).subscribe({
        next: () => {
          this.addMemberForm.reset();
          this.groupService.getGroupMembers(this.groupId).subscribe(m => this.members = m);
        },
        error: (err) => console.error(err)
      });
    }
  }
}
