# FinApp - Stock Performance Tracker

## Project Overview
A web application to view and filter stocks from NASDAQ and NYSE exchanges, allowing users to analyze stock performance over custom date ranges.

## Tech Stack
- **Frontend:** Angular 20 with Angular Material
- **Backend:** C# .NET Core 9 Web API
- **Database:** Supabase (PostgreSQL)
- **Stock Data API:** Massive API (https://api.massive.com/v3)

## Project Structure
```
FinApp/
├── backend/
│   └── FinApp.API/           # .NET Core Web API
│       ├── Controllers/      # API endpoints
│       ├── Services/         # Business logic & API integrations
│       ├── Models/           # Data models
│       └── Data/             # Supabase connection
├── frontend/
│   └── finapp-client/        # Angular SPA
│       └── src/app/
│           ├── components/   # UI components
│           ├── services/     # HTTP services
│           └── models/       # TypeScript interfaces
└── database/
    └── schema.sql            # Supabase schema
```

## Key Commands

### Backend
```bash
cd backend/FinApp.API
dotnet run                    # Start API server (http://localhost:5000)
dotnet build                  # Build project
```

### Frontend
```bash
cd frontend/finapp-client
ng serve                      # Start dev server (http://localhost:4200)
ng build                      # Production build
npm install                   # Install dependencies
```

## API Endpoints
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/stocks` | List all stocks (paginated) |
| GET | `/api/stocks/sync` | Sync stocks from Massive API |
| POST | `/api/stocks/performance` | Get performance for date range |

## Configuration
Configuration is in `backend/FinApp.API/appsettings.Development.json`:
- `Massive:ApiKey` - Massive API key
- `Supabase:Url` - Supabase project URL
- `Supabase:Key` - Supabase anon key

## Key Files
- `backend/FinApp.API/Services/MassiveApiService.cs` - Massive API integration with rate limiting
- `backend/FinApp.API/Services/StockService.cs` - Stock sync and performance calculation
- `frontend/finapp-client/src/app/services/stock.service.ts` - Angular HTTP service
- `database/schema.sql` - Supabase table schema

## Development Notes
- Massive API has rate limits (~50 requests/min on free tier)
- Stock sync takes several minutes due to rate limiting
- Frontend runs on port 4200, backend on port 5000
- CORS is configured to allow localhost:4200
