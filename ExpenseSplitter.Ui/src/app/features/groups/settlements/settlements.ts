import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { GroupService } from '../../../core/services/group';
import { SettlementService } from '../../../core/services/settlement';
import { GroupMember, Balance, SuggestedSettlement } from '../../../core/models/models';
import { DecimalPipe, SlicePipe } from '@angular/common';

@Component({
  selector: 'app-settlements',
  standalone: true,
  imports: [RouterLink, DecimalPipe, SlicePipe],
  templateUrl: './settlements.html',
  styleUrls: ['./settlements.css']
})
export class Settlements implements OnInit {
  private route = inject(ActivatedRoute);
  private groupService = inject(GroupService);
  private settlementService = inject(SettlementService);

  groupId!: string;
  members: GroupMember[] = [];
  balances: Balance[] = [];
  suggested: SuggestedSettlement[] = [];
  
  // Stats for the visual graph
  initialDebtsCount = 0;
  simplifiedDebtsCount = 0;

  ngOnInit() {
    this.groupId = this.route.snapshot.paramMap.get('id')!;
    this.loadData();
  }

  loadData() {
    this.groupService.getGroupMembers(this.groupId).subscribe(m => this.members = m);
    
    this.settlementService.getBalances(this.groupId).subscribe(b => {
      this.balances = b;
      // Rough estimation: if we didn't simplify, there could be up to N*(N-1)/2 debts, 
      // but let's just count the non-zero balances as initial nodes to show a nice ratio.
      const nonZero = b.filter(x => Math.abs(x.balance) > 0.01).length;
      this.initialDebtsCount = nonZero > 1 ? (nonZero * (nonZero - 1)) / 2 : 0;
    });

    this.settlementService.getSuggestedSettlements(this.groupId).subscribe(s => {
      this.suggested = s;
      this.simplifiedDebtsCount = s.length;
    });
  }

  getUserName(userId: string): string {
    const member = this.members.find(m => m.userId === userId);
    return member ? member.userName : (userId.substring(0, 8) + '...');
  }

  markAsPaid(settlement: SuggestedSettlement) {
    if (confirm(`Record payment of $${settlement.amount} from ${this.getUserName(settlement.fromUserId)} to ${this.getUserName(settlement.toUserId)}?`)) {
      this.settlementService.createSettlement(this.groupId, {
        payerId: settlement.fromUserId,
        payeeId: settlement.toUserId,
        amount: settlement.amount
      }).subscribe({
        next: () => {
          // Reload data to reflect the new balances
          this.loadData();
        },
        error: (err) => alert(err.error?.message || 'Failed to record payment')
      });
    }
  }
}
