export interface SearchResultItem {
  type: 'idea' | 'challenge' | 'user';
  id: string;
  titleEn: string;
  titleAr: string;
  subtitle: string;
  link: string;
}

export interface SearchResults {
  ideas: SearchResultItem[];
  challenges: SearchResultItem[];
  users: SearchResultItem[];
}
