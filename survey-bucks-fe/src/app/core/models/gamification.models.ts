export interface UserLevel {
  currentLevel: number;
  currentPoints: number;
  currentLevelThreshold: number;
  nextLevelPoints: number;
  levelName: string;
  levelDescription?: string;
  pointsToNextLevel: number;
  progressPercentage: number;
}

export interface Achievement {
  id: number;
  name: string;
  description: string;
  iconName?: string;
  rarity: 'Common' | 'Uncommon' | 'Rare' | 'Epic' | 'Legendary';
  pointsReward: number;
  dateEarned?: Date;
  isUnlocked: boolean;
  category: string;
  badgeImageUrl?: string;
}

export interface Challenge {
  id: number;
  name: string;
  description: string;
  challengeType: string;
  targetValue: number;
  currentProgress: number;
  pointsReward: number;
  startDate: Date;
  endDate: Date;
  isActive: boolean;
  isCompleted: boolean;
  progressPercentage: number;
}

export interface LeaderboardInfo {
  id: number;
  name: string;
  description: string;
  leaderboardType: 'Points' | 'Surveys' | 'Streak' | 'Custom';
  period: 'Daily' | 'Weekly' | 'Monthly' | 'AllTime';
  isActive: boolean;
}

export interface LeaderboardEntry {
  rank: number;
  userId: string;
  userName: string;
  displayName: string;
  score: number;
  isCurrentUser: boolean;
  avatarUrl?: string;
  levelName?: string;
}

export interface Leaderboard {
  id: number;
  name: string;
  description: string;
  period: string;
  lastUpdated: Date;
  entries: LeaderboardEntry[];
  userRank?: number;
  totalParticipants: number;
}