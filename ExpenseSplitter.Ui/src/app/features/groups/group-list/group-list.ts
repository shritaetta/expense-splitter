import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth';
import { GroupService } from '../../../core/services/group';
import { Group } from '../../../core/models/models';

@Component({
  selector: 'app-group-list',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './group-list.html',
  styleUrls: ['./group-list.css']
})
export class GroupList implements OnInit {
  private authService = inject(AuthService);
  private groupService = inject(GroupService);
  private fb = inject(FormBuilder);

  groups: Group[] = [];
  showCreateForm = false;

  createGroupForm = this.fb.group({
    name: ['', Validators.required],
    currency: ['USD', Validators.required]
  });

  ngOnInit() {
    this.loadGroups();
  }

  loadGroups() {
    const user = this.authService.currentUserValue;
    if (user) {
      this.groupService.getUserGroups(user.id).subscribe({
        next: (groups) => this.groups = groups,
        error: (err) => console.error(err)
      });
    }
  }

  toggleCreateForm() {
    this.showCreateForm = !this.showCreateForm;
  }

  onCreateGroup() {
    if (this.createGroupForm.valid) {
      this.groupService.createGroup(this.createGroupForm.value).subscribe({
        next: (group) => {
          this.groups.push(group);
          this.showCreateForm = false;
          this.createGroupForm.reset({ currency: 'USD' });
        },
        error: (err) => console.error(err)
      });
    }
  }
}
