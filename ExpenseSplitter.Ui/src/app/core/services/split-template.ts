import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SplitTemplate, CreateSplitTemplateDto } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class SplitTemplateService {
  private readonly apiUrl = 'http://localhost:5242/api/groups';

  constructor(private http: HttpClient) {}

  getTemplates(groupId: string) {
    return this.http.get<SplitTemplate[]>(`${this.apiUrl}/${groupId}/splittemplates`);
  }

  createTemplate(groupId: string, template: CreateSplitTemplateDto) {
    return this.http.post<SplitTemplate>(`${this.apiUrl}/${groupId}/splittemplates`, template);
  }
}
