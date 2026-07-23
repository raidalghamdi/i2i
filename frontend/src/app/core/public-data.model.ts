export interface PublicTrack {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  priority: number;
}

export interface PublicIdeaSummary {
  id: string;
  code: string;
  titleAr: string;
  titleEn: string;
  status: string;
}

export interface PublicChallenge {
  id: string;
  textAr: string;
  textEn: string;
}

export interface PublicTrackDetail {
  track: PublicTrack;
  challenges: PublicChallenge[];
  ideas: PublicIdeaSummary[];
}

export interface PublicActivity {
  id: string;
  nameAr: string;
  nameEn: string;
  type: string;
  status: string;
  startDate: string;
  endDate: string;
  ideaCount: number;
}

export interface PublicActivityDetail {
  activity: PublicActivity;
  approvedCount: number;
  pilotingCount: number;
  ideas: PublicIdeaSummary[];
}

export interface PublicSearchResults {
  ideas: PublicIdeaSummary[];
  tracks: PublicTrack[];
}
