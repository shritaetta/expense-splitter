import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { SlicePipe } from '@angular/common';
import { GroupService } from '../../../core/services/group';
import { SplitTemplateService } from '../../../core/services/split-template';
import { GroupMember, SplitTemplate, CreateSplitTemplateDto } from '../../../core/models/models';

@Component({
  selector: 'app-split-templates',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, SlicePipe],
  templateUrl: './split-templates.html',
  styleUrls: ['./split-templates.css']
})
export class SplitTemplates implements OnInit {
  private route = inject(ActivatedRoute);
  private groupService = inject(GroupService);
  private templateService = inject(SplitTemplateService);
  private fb = inject(FormBuilder);

  groupId!: string;
  members: GroupMember[] = [];
  templates: SplitTemplate[] = [];
  showCreateForm = false;

  templateForm = this.fb.group({
    name: ['', Validators.required],
    items: this.fb.array([])
  });

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    this.loadData();
  }

  loadData() {
    this.groupService.getGroupMembers(this.groupId).subscribe(m => {
      this.members = m;
      this.buildItemsForm();
    });
    this.templateService.getTemplates(this.groupId).subscribe(t => this.templates = t);
  }

  get itemsArray() {
    return this.templateForm.get('items') as FormArray;
  }

  buildItemsForm() {
    const arr = this.itemsArray;
    arr.clear();
    this.members.forEach(member => {
      arr.push(this.fb.group({
        userId: [member.userId],
        userName: [member.userName],
        included: [true],
        shareValue: ['']
      }));
    });
  }

  toggleCreateForm() {
    this.showCreateForm = !this.showCreateForm;
    if (this.showCreateForm) {
      this.templateForm.reset({ name: '' });
      this.buildItemsForm();
    }
  }

  onSubmit() {
    if (this.templateForm.invalid) return;

    const formValue = this.templateForm.value;
    const items = formValue.items || [];
    const activeItems = items
      .filter((i: any) => i.included && i.shareValue)
      .map((i: any) => ({
        userId: i.userId,
        shareValue: Number(i.shareValue)
      }));

    if (activeItems.length === 0) {
      alert('You must provide share values for at least one member.');
      return;
    }

    const dto: CreateSplitTemplateDto = {
      name: formValue.name!,
      items: activeItems
    };

    this.templateService.createTemplate(this.groupId, dto).subscribe({
      next: (template) => {
        this.templates.push(template);
        this.showCreateForm = false;
      },
      error: (err) => alert(err.error?.message || 'Failed to create template')
    });
  }
}
