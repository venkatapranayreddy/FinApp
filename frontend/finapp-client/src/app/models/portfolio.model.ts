export interface TradeEntry {
  id: string;
  ticker: string;
  shares: number;
  entryPrice: number;
  stopLossPrice: number;
  targetPrice: number;
  currentPrice: number;
  entryDate: string;
  status: string;
  notes?: string;
}

export interface AddTradeRequest {
  ticker: string;
  shares: number;
  stopLossPrice: number;
  targetPrice: number;
  notes?: string;
}

export interface TradeRiskAnalysis {
  id: string;
  ticker: string;
  shares: number;

  // Prices
  entryPrice: number;
  currentPrice: number;
  stopLossPrice: number;
  targetPrice: number;

  // Position Values
  positionValue: number;
  currentValue: number;

  // Risk Calculations
  riskPerShare: number;
  totalRiskAmount: number;
  riskPercentOfPortfolio: number;

  // Profit Calculations
  profitPerShare: number;
  totalProfitPotential: number;
  profitPercentOfPortfolio: number;

  // Current P&L
  unrealizedPnL: number;
  unrealizedPnLPercent: number;

  // Risk/Reward
  riskRewardRatio: number;

  // Status
  status: string;
  statusColor: 'green' | 'red' | 'neutral';

  // 3-5-7 Rule
  isWithin3PercentRule: boolean;
  ruleViolation: string;

  // Dates
  entryDate: string;
  notes?: string;
}

export interface PortfolioSummary {
  totalPortfolioValue: number;
  cashBalance: number;
  investedAmount: number;

  // 3-5-7 Rule Metrics
  maxRiskPerTrade: number;
  maxSectorExposure: number;
  maxTotalRisk: number;

  currentTotalRisk: number;
  currentTotalRiskPercent: number;

  // Stats
  totalOpenTrades: number;
  tradesInProfit: number;
  tradesInLoss: number;
  totalUnrealizedPnL: number;

  trades: TradeRiskAnalysis[];
}

export interface PositionSizeRequest {
  ticker: string;
  stopLossPrice: number;
  riskAmount?: number;
  portfolioValue: number;
}

export interface PositionSizeResponse {
  ticker: string;
  currentPrice: number;
  stopLossPrice: number;
  riskPerShare: number;
  maxRiskAmount: number;
  recommendedShares: number;
  positionValue: number;
  actualRiskAmount: number;
  riskPercentOfPortfolio: number;
}

export interface PriceResponse {
  ticker: string;
  companyName: string;
  price: number;
  exchange?: string;
  marketCap?: number;
}
