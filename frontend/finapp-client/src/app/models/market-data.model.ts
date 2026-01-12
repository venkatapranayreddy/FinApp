// Base response wrapper
export interface ApiResponse<T> {
  data: T;
  count?: number;
}

// Ticker Models
export interface Ticker {
  ticker: string;
  name: string;
  market: string;
  locale: string;
  primaryExchange: string;
  type: string;
  active: boolean;
  currencyName: string;
  marketCap: number | null;
}

export interface TickersResponse extends ApiResponse<Ticker[]> {}

// Snapshot Models
export interface SnapshotBar {
  o: number; // open
  h: number; // high
  l: number; // low
  c: number; // close
  v: number; // volume
  vw?: number; // volume weighted average price
}

export interface LastTrade {
  T: string; // ticker
  i: string; // id
  p: number; // price
  s?: number; // size
  x: number; // exchange
  t: number; // timestamp
  c?: number[]; // conditions
}

export interface LastQuote {
  T: string; // ticker
  P: number; // ask price
  S: number; // ask size
  p: number; // bid price
  s: number; // bid size
  t: number; // timestamp
}

export interface TickerSnapshot {
  ticker: string;
  todaysChange: number;
  todaysChangePerc: number;
  updated: number;
  day?: SnapshotBar;
  prevDay?: SnapshotBar;
  min?: SnapshotBar;
  lastTrade?: LastTrade;
  lastQuote?: LastQuote;
}

export interface SnapshotResponse extends ApiResponse<TickerSnapshot> {}
export interface SnapshotsResponse extends ApiResponse<TickerSnapshot[]> {}
export interface TopMoversResponse extends ApiResponse<TickerSnapshot[]> {}

// Trade Models
export interface Trade {
  i: string; // id
  p: number; // price
  s: number; // size
  x: number; // exchange
  t: number; // sip timestamp
  y: number; // participant timestamp
  c?: number[]; // conditions
  z?: number; // tape
}

export interface TradesResponse extends ApiResponse<Trade[]> {}
export interface LastTradeResponse extends ApiResponse<LastTrade> {}

// Quote Models
export interface Quote {
  ask_price: number;
  ask_size: number;
  ask_exchange: number;
  bid_price: number;
  bid_size: number;
  bid_exchange: number;
  sip_timestamp: number;
  participant_timestamp: number;
  conditions?: number[];
  tape?: number;
}

export interface QuotesResponse extends ApiResponse<Quote[]> {}
export interface LastQuoteResponse extends ApiResponse<LastQuote> {}

// Technical Indicator Models
export interface IndicatorValue {
  timestamp: number;
  value: number;
}

export interface MacdValue {
  timestamp: number;
  value: number;
  signal: number;
  histogram: number;
}

export interface IndicatorResponse extends ApiResponse<IndicatorValue[]> {
  indicator: string;
  window?: number;
  timespan: string;
}

export interface MacdResponse extends ApiResponse<MacdValue[]> {
  indicator: string;
  timespan: string;
}

// Aggregate (OHLCV) Models
export interface AggregateBar {
  o: number; // open
  h: number; // high
  l: number; // low
  c: number; // close
  v: number; // volume
  t: number; // timestamp
}

export interface AggregatesResponse {
  data: AggregateBar[];
  count: number;
  ticker: string;
}

// Request Models
export interface TickersRequest {
  tickers: string[];
}
