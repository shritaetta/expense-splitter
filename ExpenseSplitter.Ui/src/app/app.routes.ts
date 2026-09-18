import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { Login } from './features/auth/login/login';
import { Register } from './features/auth/register/register';
import { GroupList } from './features/groups/group-list/group-list';
import { GroupDetail } from './features/groups/group-detail/group-detail';
import { AddExpense } from './features/groups/add-expense/add-expense';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'groups', component: GroupList, canActivate: [authGuard] },
  { path: 'groups/:id', component: GroupDetail, canActivate: [authGuard] },
  { path: 'groups/:id/add-expense', component: AddExpense, canActivate: [authGuard] },
  { path: '', redirectTo: '/groups', pathMatch: 'full' },
  { path: '**', redirectTo: '/groups' }
];
