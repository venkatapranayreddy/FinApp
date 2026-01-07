export interface Stock {
  id: string;
  ticker: string;
  name: string;
  exchange: string;
  marketCap: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface StockPerformance {
  ticker: string;
  name: string;
  exchange: string;
  startPrice: number;
  endPrice: number;
  percentChange: number;
  isProfit: boolean;
  volume: number;
  marketCap: number | null;
  openPrice: number;
  closePrice: number;
  highPrice: number;
  lowPrice: number;
}

export interface StockPerformanceResponse {
  data: StockPerformance[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface FilterRequest {
  startDate: string;
  endDate: string;
  exchange?: string;
  profitOnly?: boolean;
  lossOnly?: boolean;
  page: number;
  pageSize: number;
}

export interface StocksResponse {
  data: Stock[];
  totalCount: number;
  page: number;
  pageSize: number;
}
