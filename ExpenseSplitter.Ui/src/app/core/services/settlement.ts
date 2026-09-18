import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SuggestedSettlement, CreateSettlementDto, Settlement, Balance } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class SettlementService {
  private readonly apiUrl = 'http://localhost:5242/api/groups';

  constructor(private http: HttpClient) {}

  getBalances(groupId: string) {
    return this.http.get<Balance[]>(`${this.apiUrl}/${groupId}/expenses/balances`);
  }

  getSuggestedSettlements(groupId: string) {
    return this.http.get<SuggestedSettlement[]>(`${this.apiUrl}/${groupId}/settlements/suggested`);
  }

  createSettlement(groupId: string, dto: CreateSettlementDto) {
    return this.http.post<Settlement>(`${this.apiUrl}/${groupId}/settlements`, dto);
  }
}
