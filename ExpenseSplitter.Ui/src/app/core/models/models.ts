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

export interface ExpenseParticipantDto {
  userId: string;
  owedAmount: number;
}

export interface ExpenseDto {
  id: string;
  description: string;
  totalAmount: number;
  date: string;
  payerName: string;
  participants: ExpenseParticipantDto[];
}

export interface SplitTemplate {
  id: string;
  groupId: string;
  name: string;
  items: SplitTemplateItem[];
}

export interface SplitTemplateItem {
  userId: string;
  shareValue: number;
}

export interface CreateSplitTemplateDto {
  name: string;
  items: SplitTemplateItem[];
}

export enum Frequency {
  Weekly = 0,
  Monthly = 1,
  Yearly = 2
}

export interface RecurringExpense {
  id: string;
  groupId: string;
  payerId: string;
  description: string;
  totalAmount: number;
  category: string;
  frequency: Frequency;
  startDate: string;
  nextRunDate: string;
  splitTypeId: SplitType;
  splitTemplateId?: string;
}

export interface CreateRecurringExpenseDto {
  payerId: string;
  description: string;
  totalAmount: number;
  category: string;
  frequency: Frequency;
  startDate: string;
  splitTypeId: SplitType;
  splitTemplateId?: string;
}

export interface Settlement {
  id: string;
  groupId: string;
  payerId: string;
  payeeId: string;
  amount: number;
  date: string;
}

export interface SuggestedSettlement {
  fromUserId: string;
  toUserId: string;
  amount: number;
}

export interface CreateSettlementDto {
  payerId: string;
  payeeId: string;
  amount: number;
}

export interface Balance {
  userId: string;
  balance: number;
}
