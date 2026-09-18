# ExpenseSplitter 💸

ExpenseSplitter is a full-stack, end-to-end application designed to make sharing expenses and settling debts among friends, roommates, and groups simple and fair. It features advanced split capabilities (Equal, Percentage, Exact Amount, and weighted Templates), recurring billing, and a sophisticated debt simplification algorithm.

## Features
- **Groups & Members**: Create groups and manage participants.
- **Dynamic Splits**: Split costs equally, by percentages, by exact amounts, or via reusable weighted templates (e.g. rent by room size).
- **Recurring Expenses**: Automatically generate recurring monthly or weekly bills, fully prorated for members who joined or left mid-cycle.
- **Debt Simplification**: An optimized greedy algorithm reduces the number of transactions needed for everyone to settle up.

## Tech Stack
### Backend
- **C# .NET 8** Web API
- **Entity Framework Core** with **SQLite**
- **Serilog** for structured logging
- **JWT Authentication**

### Frontend
- **Angular 17+** with Standalone Components
- **TypeScript** & **RxJS**
- **Bootstrap** for clean, responsive UI

## How to Run Locally

### Using Docker Compose (Recommended)
1. Ensure you have Docker Desktop running.
2. In the root directory, run:
   ```bash
   docker-compose up --build
   ```
3. The API will be available at `http://localhost:5000` and the Angular UI at `http://localhost:4200`.

### Manual Setup
**1. Run the Backend (API)**
```bash
cd ExpenseSplitter.Api
dotnet restore
dotnet run
```
*Note: A SQLite database (`app.db`) will be automatically created and seeded in your output directory.*

**2. Run the Frontend (UI)**
```bash
cd ExpenseSplitter.Ui
npm install
npm start
```
Open `http://localhost:4200` in your browser.

## Architecture Highlights
- **Participant Snapshotting**: The API snapshots participant shares at the moment an expense is created. This ensures historical integrity: if a template changes or a user leaves, past expenses remain completely unaffected.
- **Debt Simplification Algorithm**: Implementing a greedy $O(N \log N)$ algorithm, the backend calculates net balances and recursively matches the highest creditors with the highest debtors to eliminate intermediary transactions.
- **Proration Logic**: Recurring bills dynamically adjust shares using time-based proration if a member joins or leaves during an ongoing billing cycle.
