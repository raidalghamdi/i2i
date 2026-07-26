/** Bilingual text pair, used throughout homepage section content. */
export interface Loc {
  ar: string;
  en: string;
}

/** Section content as received from / sent to the server-parsed JSON shape (nested object). */
export type HomeSectionContent = Record<string, unknown>;

/** A homepage section as returned by `GET /api/admin/home/sections`. */
export interface AdminHomeSection {
  id: string;
  idx: number;
  type: string;
  isVisible: boolean;
  contentJson: HomeSectionContent;
  updatedAt: string;
  updatedById: string | null;
}

/** Body shape for `PUT`/`POST /api/admin/home/sections` — contentJson goes out as a STRING. */
export interface HomeSectionInput {
  id: string | null;
  idx: number;
  type: string;
  isVisible: boolean;
  contentJson: string;
}

/** All section types the builder can add, in the seeded/display order, plus the generic `richtext` type. */
export const HOME_SECTION_TYPES = [
  'hero',
  'about',
  'objectives',
  'tracks',
  'details',
  'timeline',
  'criteria',
  'prizes',
  'gallery',
  'partners',
  'faq',
  'cta',
  'richtext',
] as const;

export type HomeSectionType = (typeof HOME_SECTION_TYPES)[number];

/** How a single contentJson field should be edited. */
export type FieldKind = 'loc' | 'locArray' | 'objArray' | 'text' | 'image' | 'video';

/** A sub-field within an `objArray` row (e.g. `timeline.stages[].title`). */
export interface ObjArraySubField {
  key: string;
  kind: 'loc' | 'text' | 'number' | 'image';
  label: string;
}

/** Describes one editable field of a section's contentJson. Drives the generic per-type editor. */
export interface FieldDef {
  key: string;
  kind: FieldKind;
  label: string;
  objFields?: ObjArraySubField[];
}

const loc = (key: string, label: string): FieldDef => ({ key, kind: 'loc', label });
const locArray = (key: string, label: string): FieldDef => ({ key, kind: 'locArray', label });
const text = (key: string, label: string): FieldDef => ({ key, kind: 'text', label });
const objArray = (key: string, label: string, objFields: ObjArraySubField[]): FieldDef => ({
  key,
  kind: 'objArray',
  label,
  objFields,
});

/** Field schema per section `type`, mirroring `home-section-contract.md`. Keep additive only. */
export const SECTION_FIELD_DEFS: Record<string, FieldDef[]> = {
  hero: [
    loc('eyebrow', 'Eyebrow'),
    locArray('words', 'Rotating words'),
    loc('headline', 'Headline'),
    loc('subheadline', 'Subheadline'),
    loc('primaryCtaLabel', 'Primary CTA label'),
    text('primaryCtaLink', 'Primary CTA link'),
    loc('secondaryCtaLabel', 'Secondary CTA label'),
    text('secondaryCtaLink', 'Secondary CTA link'),
    loc('closedNotice', 'Closed notice'),
    locArray('slogan', 'Slogan'),
  ],
  about: [loc('title', 'Title'), locArray('paragraphs', 'Paragraphs'), { key: 'imageUrl', kind: 'image', label: 'Photo' }],
  objectives: [loc('title', 'Title'), locArray('items', 'Items')],
  tracks: [loc('title', 'Title'), loc('intro', 'Intro')],
  details: [
    loc('title', 'Title'),
    loc('rulesTitle', 'Rules title'),
    locArray('rules', 'Rules'),
    loc('formatTitle', 'Format title'),
    loc('format', 'Format'),
    loc('eligibilityTitle', 'Eligibility title'),
    loc('eligibility', 'Eligibility'),
  ],
  timeline: [
    loc('title', 'Title'),
    objArray('stages', 'Stages', [
      { key: 'id', kind: 'text', label: 'Id' },
      { key: 'title', kind: 'loc', label: 'Title' },
      { key: 'date', kind: 'loc', label: 'Date' },
      { key: 'description', kind: 'loc', label: 'Description' },
      { key: 'tone', kind: 'text', label: 'Tone' },
    ]),
  ],
  criteria: [
    loc('title', 'Title'),
    loc('eyebrow', 'Eyebrow'),
    loc('lead', 'Lead'),
    objArray('items', 'Items', [
      { key: 'label', kind: 'loc', label: 'Label' },
      { key: 'description', kind: 'loc', label: 'Description' },
      { key: 'weight', kind: 'number', label: 'Weight' },
      { key: 'color', kind: 'text', label: 'Color' },
      { key: 'icon', kind: 'text', label: 'Icon' },
    ]),
  ],
  prizes: [
    loc('title', 'Title'),
    objArray('items', 'Items', [
      { key: 'tier', kind: 'loc', label: 'Tier' },
      { key: 'value', kind: 'loc', label: 'Value' },
    ]),
  ],
  gallery: [
    loc('title', 'Title'),
    loc('body', 'Body'),
    loc('galleryTitle', 'Gallery title'),
    objArray('items', 'Photos', [
      { key: 'caption', kind: 'loc', label: 'Caption' },
      { key: 'imageUrl', kind: 'image', label: 'Photo' },
    ]),
    loc('videoTitle', 'Video title'),
    loc('videoHint', 'Video hint'),
    { key: 'videoUrl', kind: 'video', label: 'Video' },
  ],
  partners: [loc('title', 'Title'), locArray('items', 'Items')],
  faq: [
    loc('title', 'Title'),
    objArray('items', 'Items', [
      { key: 'q', kind: 'loc', label: 'Question' },
      { key: 'a', kind: 'loc', label: 'Answer' },
    ]),
  ],
  cta: [
    loc('title', 'Title'),
    loc('subtitle', 'Subtitle'),
    loc('buttonLabel', 'Button label'),
    text('buttonLink', 'Button link'),
  ],
  richtext: [loc('title', 'Title'), loc('html', 'HTML')],
};

export function emptyLoc(): Loc {
  return { ar: '', en: '' };
}

export function emptyObjArrayRow(fields: ObjArraySubField[]): Record<string, unknown> {
  const row: Record<string, unknown> = {};
  for (const f of fields) {
    row[f.key] = f.kind === 'loc' ? emptyLoc() : f.kind === 'number' ? 0 : '';
  }
  return row;
}

/** Builds an empty content skeleton for a newly-added section of the given type. */
export function emptySectionContent(type: string): HomeSectionContent {
  const defs = SECTION_FIELD_DEFS[type] ?? [];
  const content: HomeSectionContent = {};
  for (const def of defs) {
    switch (def.kind) {
      case 'loc':
        content[def.key] = emptyLoc();
        break;
      case 'locArray':
      case 'objArray':
        content[def.key] = [];
        break;
      case 'text':
      case 'image':
      case 'video':
        content[def.key] = '';
        break;
    }
  }
  return content;
}
