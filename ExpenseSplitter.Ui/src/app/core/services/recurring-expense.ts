import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RecurringExpense, CreateRecurringExpenseDto, ExpenseDto } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class RecurringExpenseService {
  private readonly apiUrl = 'http://localhost:5242/api/groups';

  constructor(private http: HttpClient) {}

  createRecurringExpense(groupId: string, dto: CreateRecurringExpenseDto) {
    return this.http.post<RecurringExpense>(`${this.apiUrl}/${groupId}/recurringexpenses`, dto);
  }

  previewNextCycle(groupId: string, id: string) {
    return this.http.get<ExpenseDto>(`${this.apiUrl}/${groupId}/recurringexpenses/${id}/preview`);
  }
}
