import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { GroupList } from './features/groups/group-list/group-list';
import { GroupDetail } from './features/groups/group-detail/group-detail';
import { AddExpense } from './features/groups/add-expense/add-expense';
import { SplitTemplates } from './features/groups/split-templates/split-templates';
import { RecurringExpenses } from './features/groups/recurring-expenses/recurring-expenses';
import { Settlements } from './features/groups/settlements/settlements';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'groups', component: GroupList, canActivate: [authGuard] },
  { path: 'groups/:id', component: GroupDetail, canActivate: [authGuard] },
  { path: 'groups/:id/add-expense', component: AddExpense, canActivate: [authGuard] },
  { path: 'groups/:id/templates', component: SplitTemplates, canActivate: [authGuard] },
  { path: 'groups/:id/recurring', component: RecurringExpenses, canActivate: [authGuard] },
  { path: 'groups/:id/settlements', component: Settlements, canActivate: [authGuard] },
  { path: '', redirectTo: '/groups', pathMatch: 'full' },
  { path: '**', redirectTo: '/groups' }
];
