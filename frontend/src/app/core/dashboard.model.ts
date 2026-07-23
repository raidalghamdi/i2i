export interface AdminDashboard { totalUsers: number; activeIdeas: number; pendingEvaluations: number; health: string; }
export interface CommitteeDashboard { awaitingDecision: number; decisionsThisWeek: number; }
export interface ScreeningBuckets { total: number; underReview: number; approved: number; returned: number; rejected: number; }
export interface SupervisorDashboard { teamMembers: number; sectorIdeas: number; escalationsAwaitingMe: number; screening: ScreeningBuckets; }
