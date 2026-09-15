/**
 * Bandera por nombre completo de pais en español (Competitors.Pais, Memberships.Pais).
 * Debe cubrir la lista de paises seleccionable en los formularios de competidor/membresia.
 */
export const COUNTRY_NAME_FLAGS: Record<string, string> = {
  'Argentina': '🇦🇷',
  'Bolivia': '🇧🇴',
  'Brasil': '🇧🇷',
  'Chile': '🇨🇱',
  'Colombia': '🇨🇴',
  'Costa Rica': '🇨🇷',
  'Ecuador': '🇪🇨',
  'El Salvador': '🇸🇻',
  'Guatemala': '🇬🇹',
  'Honduras': '🇭🇳',
  'México': '🇲🇽',
  'Nicaragua': '🇳🇮',
  'Panamá': '🇵🇦',
  'Paraguay': '🇵🇾',
  'Perú': '🇵🇪',
  'República Dominicana': '🇩🇴',
  'Uruguay': '🇺🇾',
  'Venezuela': '🇻🇪',
};

/**
 * Bandera por codigo de pais para Events.Pais (texto libre tipeado a mano en el admin).
 * Incluye los codigos IOC de 3 letras (el estandar que usan las federaciones de surf,
 * ej. "PER", "BRA", "CHI") y ademas los equivalentes ISO de 2 letras (ej. "PE", "BR")
 * por si se cargaron eventos con ese formato mas corto.
 */
export const COUNTRY_CODE_FLAGS: Record<string, string> = {
  // IOC (3 letras)
  AFG: '🇦🇫', ALB: '🇦🇱', ALG: '🇩🇿', AND: '🇦🇩', ANG: '🇦🇴', ANT: '🇦🇬',
  ARG: '🇦🇷', ARM: '🇦🇲', ARU: '🇦🇼', ASA: '🇦🇸', AUS: '🇦🇺', AUT: '🇦🇹', AZE: '🇦🇿',
  BAH: '🇧🇸', BAN: '🇧🇩', BAR: '🇧🇧', BDI: '🇧🇮', BEL: '🇧🇪', BEN: '🇧🇯', BER: '🇧🇲',
  BHU: '🇧🇹', BIH: '🇧🇦', BIZ: '🇧🇿', BLR: '🇧🇾', BOL: '🇧🇴', BOT: '🇧🇼', BRA: '🇧🇷',
  BRN: '🇧🇭', BRU: '🇧🇳', BUL: '🇧🇬', BUR: '🇧🇫',
  CAF: '🇨🇫', CAM: '🇰🇭', CAN: '🇨🇦', CAY: '🇰🇾', CGO: '🇨🇬', CHA: '🇹🇩', CHI: '🇨🇱',
  CHN: '🇨🇳', CIV: '🇨🇮', CMR: '🇨🇲', COD: '🇨🇩', COK: '🇨🇰', COL: '🇨🇴', COM: '🇰🇲',
  CPV: '🇨🇻', CRC: '🇨🇷', CRO: '🇭🇷', CUB: '🇨🇺', CYP: '🇨🇾', CZE: '🇨🇿',
  DEN: '🇩🇰', DJI: '🇩🇯', DMA: '🇩🇲', DOM: '🇩🇴',
  ECU: '🇪🇨', EGY: '🇪🇬', ERI: '🇪🇷', ESA: '🇸🇻', ESP: '🇪🇸', EST: '🇪🇪', ETH: '🇪🇹',
  FIJ: '🇫🇯', FIN: '🇫🇮', FRA: '🇫🇷', FSM: '🇫🇲',
  GAB: '🇬🇦', GAM: '🇬🇲', GBR: '🇬🇧', GBS: '🇬🇼', GEO: '🇬🇪', GEQ: '🇬🇶', GER: '🇩🇪',
  GHA: '🇬🇭', GRE: '🇬🇷', GRN: '🇬🇩', GUA: '🇬🇹', GUI: '🇬🇳', GUM: '🇬🇺', GUY: '🇬🇾',
  HAI: '🇭🇹', HKG: '🇭🇰', HON: '🇭🇳', HUN: '🇭🇺',
  INA: '🇮🇩', IND: '🇮🇳', IRI: '🇮🇷', IRL: '🇮🇪', IRQ: '🇮🇶', ISL: '🇮🇸', ISR: '🇮🇱',
  ISV: '🇻🇮', ITA: '🇮🇹', IVB: '🇻🇬',
  JAM: '🇯🇲', JOR: '🇯🇴', JPN: '🇯🇵',
  KAZ: '🇰🇿', KEN: '🇰🇪', KGZ: '🇰🇬', KIR: '🇰🇮', KOR: '🇰🇷', KOS: '🇽🇰', KSA: '🇸🇦', KUW: '🇰🇼',
  LAO: '🇱🇦', LAT: '🇱🇻', LBA: '🇱🇾', LBN: '🇱🇧', LBR: '🇱🇷', LCA: '🇱🇨', LES: '🇱🇸',
  LIE: '🇱🇮', LTU: '🇱🇹', LUX: '🇱🇺',
  MAD: '🇲🇬', MAR: '🇲🇦', MAS: '🇲🇾', MAW: '🇲🇼', MDA: '🇲🇩', MDV: '🇲🇻', MEX: '🇲🇽',
  MGL: '🇲🇳', MHL: '🇲🇭', MKD: '🇲🇰', MLI: '🇲🇱', MLT: '🇲🇹', MNE: '🇲🇪', MON: '🇲🇨',
  MOZ: '🇲🇿', MRI: '🇲🇺', MTN: '🇲🇷', MYA: '🇲🇲',
  NAM: '🇳🇦', NCA: '🇳🇮', NED: '🇳🇱', NEP: '🇳🇵', NGR: '🇳🇬', NIG: '🇳🇪', NOR: '🇳🇴',
  NRU: '🇳🇷', NZL: '🇳🇿',
  OMA: '🇴🇲',
  PAK: '🇵🇰', PAN: '🇵🇦', PAR: '🇵🇾', PER: '🇵🇪', PHI: '🇵🇭', PLE: '🇵🇸', PLW: '🇵🇼',
  PNG: '🇵🇬', POL: '🇵🇱', POR: '🇵🇹', PRK: '🇰🇵', PUR: '🇵🇷',
  QAT: '🇶🇦',
  ROU: '🇷🇴', RSA: '🇿🇦', RUS: '🇷🇺', RWA: '🇷🇼',
  SAM: '🇼🇸', SEN: '🇸🇳', SEY: '🇸🇨', SGP: '🇸🇬', SKN: '🇰🇳', SLE: '🇸🇱', SLO: '🇸🇮',
  SMR: '🇸🇲', SOL: '🇸🇧', SOM: '🇸🇴', SRB: '🇷🇸', SRI: '🇱🇰', SSD: '🇸🇸', STP: '🇸🇹',
  SUD: '🇸🇩', SUI: '🇨🇭', SUR: '🇸🇷', SVK: '🇸🇰', SWE: '🇸🇪', SWZ: '🇸🇿', SYR: '🇸🇾',
  TAN: '🇹🇿', TGA: '🇹🇴', THA: '🇹🇭', TJK: '🇹🇯', TKM: '🇹🇲', TLS: '🇹🇱', TOG: '🇹🇬',
  TPE: '🇹🇼', TTO: '🇹🇹', TUN: '🇹🇳', TUR: '🇹🇷', TUV: '🇹🇻',
  UAE: '🇦🇪', UGA: '🇺🇬', UKR: '🇺🇦', URU: '🇺🇾', USA: '🇺🇸', UZB: '🇺🇿',
  VAN: '🇻🇺', VEN: '🇻🇪', VIE: '🇻🇳', VIN: '🇻🇨',
  YEM: '🇾🇪',
  ZAM: '🇿🇲', ZIM: '🇿🇼',

  // ISO alpha-2 (2 letras) — por si el evento se cargo con este formato mas corto
  AR: '🇦🇷', BO: '🇧🇴', BR: '🇧🇷', CL: '🇨🇱', CO: '🇨🇴', CR: '🇨🇷', EC: '🇪🇨',
  SV: '🇸🇻', GT: '🇬🇹', HN: '🇭🇳', MX: '🇲🇽', NI: '🇳🇮', PA: '🇵🇦', PY: '🇵🇾',
  PE: '🇵🇪', DO: '🇩🇴', UY: '🇺🇾', VE: '🇻🇪', US: '🇺🇸', ES: '🇪🇸', PT: '🇵🇹',
};

const FLAG_FALLBACK = '🏳️';

function normalizeName(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .trim()
    .toLowerCase();
}

const COUNTRY_NAME_FLAGS_NORMALIZED: Record<string, string> = Object.fromEntries(
  Object.entries(COUNTRY_NAME_FLAGS).map(([name, flag]) => [normalizeName(name), flag]),
);

/**
 * Para campos que guardan el nombre completo del pais en español (ej. Competitors.Pais).
 * Ignora tildes/mayusculas (ej. "Peru" y "Perú" resuelven igual).
 */
export function flagForCountryName(name: string | null | undefined): string {
  if (!name) return FLAG_FALLBACK;
  return COUNTRY_NAME_FLAGS_NORMALIZED[normalizeName(name)] ?? FLAG_FALLBACK;
}

/**
 * Para campos que guardan un codigo de pais IOC o ISO (ej. Events.Pais).
 * Si no matchea ningun codigo, intenta como nombre completo (algunos eventos
 * importados desde Excel guardan el nombre en vez del codigo de 3 letras).
 */
export function flagForCountryCode(code: string | null | undefined): string {
  if (!code) return FLAG_FALLBACK;
  const trimmed = code.trim();
  return COUNTRY_CODE_FLAGS[trimmed.toUpperCase()] ?? flagForCountryName(trimmed);
}
