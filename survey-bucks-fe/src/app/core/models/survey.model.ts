export interface SurveyListItemDto {
  id: number;
  name: string;
  description: string;
  durationInSeconds: number;
  openingDateTime: string;
  closingDateTime: string;
  companyName: string;
  industry: string;
  reward: RewardSummaryDto;
  matchScore: number;
}
export interface SurveyDetailDto {
  id: number;
  name: string;
  description: string;
  openingDateTime: string;
  closingDateTime: string;
  durationInSeconds: number;
  companyName: string;
  companyDescription: string;
  industry: string;
  minQuestions: number;
  maxTimeInMins: number;
  requireAllQuestions: boolean;
  rewards: RewardDto[];
  sections: SurveySectionDto[];
}
export interface RewardDto {
  id: number;
  surveyId: number;
  name: string;
  description: string;
  amount: number | null;
  rewardType: string;
  rewardCategory: string;
  pointsCost: number | null;
  availableQuantity: number | null;
  monetaryValue: number | null;
  imageUrl: string;
  redemptionInstructions: string;
  minimumUserLevel: number | null;
}
export interface SurveySectionDto {
  id: number;
  surveyId: number;
  name: string;
  description: string;
  order: number;
  questions: QuestionDto[];
}
export interface QuestionDto {
  id: number;
  surveySectionId: number;
  text: string;
  isMandatory: boolean;
  order: number;
  questionTypeId: number;
  questionTypeName: string;
  minValue: number | null;
  maxValue: number | null;
  validationMessage: string;
  helpText: string;
  responseChoices: QuestionResponseChoiceDto[];
  media: QuestionMediaDto[];
  matrixRows: MatrixRowDto[];
  matrixColumns: MatrixColumnDto[];
}

export interface QuestionMediaDto {
  id: number;
  questionId: number;
  mediaTypeId: number;
  mediaTypeName: string;
  fileName: string;
  fileSize: number;
  storagePath: string;
  displayOrder: number;
  altText: string;
}

export interface QuestionResponseChoiceDto {
  id: number;
  questionId: number;
  text: string;
  value: string;
  order: number;
  isExclusiveOption: boolean;
}
export interface MatrixRowDto {
  id: number;
  questionId: number;
  text: string;
  order: number;
}
export interface MatrixColumnDto {
  id: number;
  questionId: number;
  text: string;
  value: string;
  order: number;
}

export interface RewardSummaryDto {
  id: number;
  name: string;
  description?: string;
  rewardType: 'Points' | 'Cash' | 'Gift Cards' | 'Products';
  amount?: number;
  monetaryValue?: number;
  currency?: string;
  pointsCost?: number;
  isEarned: boolean;
  earnedDate?: Date | string;
  redemptionStatus?: 'Unclaimed' | 'Claimed' | 'Processing' | 'Delivered';
}

export interface SurveyParticipationSummaryDto {
  // Participation identifiers
  id: number;
  surveyId: number;
  userId: string;

  // Survey information
  surveyName: string;
  surveyTitle?: string; // Alternative field name for backward compatibility
  companyName?: string;
  industry?: string;

  // Participation timestamps
  enrolmentDateTime: Date | string;
  startedAtDateTime?: Date | string;
  completedAtDateTime?: Date | string;

  // Status information
  statusId: number;
  statusName:
    | 'Enrolled'
    | 'InProgress'
    | 'Completed'
    | 'Abandoned'
    | 'Disqualified'
    | 'Expired'
    | 'Rewarded';

  // Progress tracking
  progressPercentage: number;
  currentSectionId?: number;
  currentSectionName?: string;
  currentQuestionId?: number;
  lastQuestionAnsweredId?: number;

  // Time tracking
  timeSpentInSeconds: number;

  // Navigation helpers
  totalSections?: number;
  completedSections?: number;
  totalQuestions?: number;
  answeredQuestions?: number;

  // Completion information
  completionCode?: string;
  disqualificationReason?: string;

  // Reward information
  pointsEarned?: number;
  rewardEarned?: number; // For backward compatibility
  rewardId?: number;
  rewardName?: string;
  rewardType?: string;
  monetaryValue?: number;

  // Survey metadata (for display purposes)
  estimatedDurationSeconds?: number;
  requireAllQuestions?: boolean;
  minQuestions?: number;

  // Feedback information
  feedbackId?: number;
  feedbackRating?: number;
  feedbackSubmitted?: boolean;

  // Analytics data
  matchScore?: number;
  isHighPriority?: boolean;

  // Completion date for filtering/sorting (derived from completedAtDateTime)
  completionDate?: Date | string;
}

export interface SurveyParticipationDetailDto
  extends SurveyParticipationSummaryDto {
  // Additional survey details
  surveyDescription: string;
  closingDateTime: Date | string;

  // Detailed progress information
  sectionsProgress: SectionProgressDto[];

  // Session information
  sessionData?: any;
  lastActivity?: Date | string;

  // Detailed reward information
  rewards: RewardSummaryDto[];

  // Time tracking details
  averageTimePerQuestion?: number;
  timeBySection?: { [sectionId: number]: number };

  // Validation information
  canResume: boolean;
  canComplete: boolean;
  validationErrors?: string[];
}

export interface SectionProgressDto {
  sectionId: number;
  sectionName: string;
  order: number;
  questionCount: number;
  answeredCount: number;
  completionPercentage: number;
  timeSpent: number;
  isCompleted: boolean;
  isCurrent: boolean;
}
