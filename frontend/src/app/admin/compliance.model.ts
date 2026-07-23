export interface ComplianceControlRow {
  id: string;
  controlCode: string;
  standardBodyCode: string;
  standardBodyNameAr: string;
  standardBodyNameEn: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  statusCode: string;
  statusNameAr: string;
  statusNameEn: string;
  mappedFeaturePaths: string[];
}
