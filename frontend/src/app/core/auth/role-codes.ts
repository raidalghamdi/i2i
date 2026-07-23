export const RoleCodes = {
  Admin: 'admin',
  Supervisor: 'supervisor',
  Evaluator: 'evaluator',
  Submitter: 'submitter',
  Judge: 'judge',
  Expert: 'expert',
  Mentor: 'mentor',
  Facilitator: 'facilitator',
} as const;

export type RoleCode = (typeof RoleCodes)[keyof typeof RoleCodes];
