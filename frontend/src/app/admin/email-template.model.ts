export interface EmailTemplate {
  id: string;
  kind: string;
  subjectAr: string;
  subjectEn: string;
  bodyAr: string;
  bodyEn: string;
  isBroadcast: boolean;
}

export interface EmailTemplateUpdateInput {
  subjectAr: string;
  subjectEn: string;
  bodyAr: string;
  bodyEn: string;
  isBroadcast: boolean;
}

export interface EmailTemplateAttachment {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
}
