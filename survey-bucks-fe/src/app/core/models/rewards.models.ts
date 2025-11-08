export interface UserPoints {
  balance: number;
  totalEarned: number;
  totalRedeemed: number;
  pendingPoints?: number;
  lifetimePoints?: number;
}

export interface PointTransaction {
  id: number;
  userId: string;
  pointsAmount: number;
  transactionType: 'Earned' | 'Redeemed' | 'Bonus' | 'Penalty';
  description: string;
  sourceType: string;
  sourceId?: number;
  transactionDate: Date;
  isProcessed: boolean;
}

export interface UserReward {
  id: number;
  userId: string;
  rewardId: number;
  rewardName: string;
  pointsUsed: number;
  redemptionDate: Date;
  claimDate?: Date;
  status: 'Pending' | 'Available' | 'Claimed' | 'Expired';
  rewardCode?: string;
  expiryDate?: Date;
}

export interface AvailableReward {
  id: number;
  name: string;
  description: string;
  pointsRequired: number;
  category: string;
  imageUrl?: string;
  isActive: boolean;
  stockQuantity?: number;
  isLimitedTime?: boolean;
  expiryDate?: Date;
}