# Vehicle Registry

A small registry service built around two ideas that matter for any system
holding sensitive records:

1. **Nothing is overwritten.** Every change to a vehicle is captured as a
   full snapshot in an append-only history table, so you can ask what a
   record looked like on any past date.
2. **Every read is accountable.** Looking up a record requires a stated
   reason, and every access is logged - who, when, why.

Both mechanisms are enforced centrally (an EF Core `SaveChangesInterceptor`
for history, an `IAsyncActionFilter` for access logging), so no individual
controller action can accidentally skip them.

## Getting this running

The files in this download go into the solution you already created. Copy
`src/` and `tests/` into your `VehicleRegistry` folder, overwriting the
generated placeholder files.

1. Install the required packages - see `SETUP_PACKAGES.md`.
2. Create the initial migration:
   ```
   dotnet ef migrations add InitialCreate --project src/VehicleRegistry.Core --startup-project src/VehicleRegistry.Api
   ```
   (If `dotnet ef` isn't found: `dotnet tool install --global dotnet-ef`)
3. Run the API - the database and tables are created automatically on startup:
   ```
   dotnet run --project src/VehicleRegistry.Api
   ```
4. Open the Swagger UI at the URL printed in the console (something like
   `http://localhost:5xxx/swagger`).
5. Run the tests:
   ```
   dotnet test
   ```

## Trying it out

Create a vehicle:
```
POST /api/vehicles
{
  "registrationNumber": "ABC123",
  "make": "Volvo",
  "model": "V60",
  "year": 2019,
  "owner": "Jonas Berg",
  "status": 0,
  "inspectionDue": "2026-05-31"
}
```

Read it back (note the required header):
```
GET /api/vehicles/1
X-Access-Reason: Case 4521, owner enquiry
```

Change the owner, then see both the current state and the history:
```
PUT /api/vehicles/1   { "owner": "Karin Lindqvist", "status": 0, "inspectionDue": "2026-05-31" }
GET /api/vehicles/1/history        X-Access-Reason: ...
GET /api/vehicles/1?asOf=2025-01-01   X-Access-Reason: ...
GET /api/vehicles/1/access-log     X-Access-Reason: ...
```

## Design decisions worth explaining in an interview

- **Full snapshots, not field-level diffs, in history.** Costs more
  storage, but makes the "as of" query a single indexed lookup instead of
  replaying a sequence of changes. A reasonable trade-off to name and
  defend.
- **History and access logging are enforced structurally**, not by
  convention. An interceptor and an action filter mean a future developer
  can't add a new update path and forget to log it.
- **`ICurrentUserService` is an interface**, implemented in the Api project
  against HTTP headers for now. The Core project has no knowledge of HTTP
  or authentication at all - swapping in real JWT-based auth later touches
  one file.
- **Not implemented, deliberately left as "next steps":** real
  authentication/roles, a hash chain on the access log to make it
  tamper-evident, and a second rule set to prove the history mechanism is
  reusable beyond vehicles. Mentioning these unprompted shows you know
  where the design goes next.

## What's general vs. what's the example

The history and access-logging mechanisms don't know anything about
vehicles specifically - they work off `DbContext.ChangeTracker` and route
data. The `Vehicle` entity is just the example domain used to demonstrate
them; the same interceptor would work unchanged for any other entity.
