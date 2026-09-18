import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Group, GroupMember } from '../models/models';

@Injectable({
  providedIn: 'root',
})
export class GroupService {
  private readonly apiUrl = 'http://localhost:5242/api';

  constructor(private http: HttpClient) {}

  getUserGroups(userId: string) {
    return this.http.get<Group[]>(`${this.apiUrl}/users/${userId}/groups`);
  }

  getGroup(groupId: string) {
    return this.http.get<Group>(`${this.apiUrl}/groups/${groupId}`);
  }

  createGroup(group: any) {
    return this.http.post<Group>(`${this.apiUrl}/groups`, group);
  }

  getGroupMembers(groupId: string) {
    return this.http.get<GroupMember[]>(`${this.apiUrl}/groups/${groupId}/members`);
  }

  addMember(groupId: string, userId: string) {
    return this.http.post(`${this.apiUrl}/groups/${groupId}/members`, { userId });
  }
}
