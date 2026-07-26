export interface IdentityResponse {
  samAccountName: string | null;
  email: string | null;
  department: string | null;
  roles: string[];
}

export interface IdentityState extends IdentityResponse {
  activeRole: string | null;
}
