# Test Strategy and Verification

## Commands

```powershell
dotnet test BancoHorizonte.slnx -c Release
cd .\src\banco-horizonte-web
npm test -- --watch=false
npm run build
```

## Backend scenarios

Test each priority rule independently and cumulatively; all score thresholds; the 24-hour age rule; invalid amounts and future dates; the 75% alert; `In time`, `Upcoming`, and `Overdue`; customer normalization; duplicate detection; catalog consistency; valid/invalid status transitions; no reopening; required observations; assignment/reassignment; role checks; and identity, phone, email, description, catalog, and amount validation.

## Frontend scenarios

Test initial invalid form state, customer/contact/description/amount validation, valid registration without subjective impact fields, understandable sign-in/registration errors, and password visibility.

## Recorded verification status

- ASP.NET Core Release build: passed.
- Angular production build: passed.
- Angular tests: 8 passed.
- Supabase migration, demo data, and `ready/connected` health check: passed.
- xUnit assembly: compiled successfully, but Windows Application Control blocked the runner with `0x800711C7` on the original workstation. Run it in an unrestricted .NET environment for a final result.

This records verified results and does not claim a blocked test was executed.