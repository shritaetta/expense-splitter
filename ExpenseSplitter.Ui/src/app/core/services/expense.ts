import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ExpenseDto, CreateExpenseDto } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class ExpenseService {
  private readonly apiUrl = 'http://localhost:5242/api';

  constructor(private http: HttpClient) {}

  getGroupExpenses(groupId: string) {
    return this.http.get<ExpenseDto[]>(`${this.apiUrl}/groups/${groupId}/expenses`);
  }

  createExpense(expense: CreateExpenseDto) {
    return this.http.post<ExpenseDto>(`${this.apiUrl}/expenses`, expense);
  }
}
