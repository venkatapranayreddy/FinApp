export type DirectionalBias = 'Bullish' | 'Bearish' | 'Neutral';
export type MarketCapCategory = 'SmallCap' | 'MidCap' | 'LargeCap' | 'MegaCap';

export interface UnusualOptionsActivity {
  symbol: string;
  companyName: string;
  lastPrice: number;
  priceChange: number;
  priceChangePercent: number;

  // Volume Data
  totalOptionsVolume: number;
  avgOptionsVolume30Day: number;
  callVolume: number;
  putVolume: number;

  // Calculated Metrics
  volumeChangePercent: number;
  putCallRatio: number;
  impliedVolatility: number;
  openInterest: number;

  // Directional Analysis
  bias: DirectionalBias;
  biasLabel: string;

  // Classification
  marketCap: number;
  capCategory: MarketCapCategory;
  capCategoryLabel: string;

  // Activity Score
  activityScore: number;

  timestamp: string;
}

export interface OptionsActivityResponse {
  data: UnusualOptionsActivity[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  lastUpdated: string;
  stats: OptionsActivityStats;
}

export interface OptionsActivityStats {
  totalSymbols: number;
  bullishCount: number;
  bearishCount: number;
  neutralCount: number;
  avgVolumeChangePercent: number;
  topBullishSymbol: string;
  topBearishSymbol: string;
}

export interface OptionsActivityFilter {
  bias?: DirectionalBias;
  highIVOnly?: boolean;
  capCategory?: MarketCapCategory;
  minVolume?: number;
  minVolumeChange?: number;
  sortBy?: string;
  sortDesc?: boolean;
  page?: number;
  pageSize?: number;
}

export interface TopOptionContract {
  contractSymbol: string;
  optionType: string;
  strikePrice: number;
  expirationDate: string;
  volume: number;
  openInterest: number;
  impliedVolatility: number;
  lastPrice: number;
  bid: number;
  ask: number;
  delta: number;
  activityType: string;
  isUnusual: boolean;
  volumeToOIRatio: number;
}

export interface OptionsActivityDetails {
  symbol: string;
  companyName: string;
  summary: UnusualOptionsActivity;
  topContracts: TopOptionContract[];
  aiExplanation: string;
  potentialCatalysts: string[];
  generatedAt: string;
}
