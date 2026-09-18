export interface User {
  id: string;
  name: string;
  email: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface Group {
  id: string;
  name: string;
  currency: string;
}

export interface GroupMember {
  userId: string;
  userName: string;
  groupId: string;
}

export enum SplitType {
  Equal = 0,
  Percentage = 1,
  Exact = 2,
  Template = 3
}

export interface ExpenseParticipantDto {
  userId: string;
  shareAmount?: number;
  percentage?: number;
}

export interface CreateExpenseDto {
  groupId: string;
  description: string;
  totalAmount: number;
  splitType: SplitType;
  payerId: string;
  splitTemplateId?: string;
  participants: ExpenseParticipantDto[];
}

export interface ExpenseDto {
  id: string;
  description: string;
  totalAmount: number;
  date: string;
  payerName: string;
}
