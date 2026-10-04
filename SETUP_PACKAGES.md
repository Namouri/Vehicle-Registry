# Commands to run in your VehicleRegistry folder

## Core project - EF Core, no web dependencies
dotnet add src/VehicleRegistry.Core package Microsoft.EntityFrameworkCore

## Api project - Sqlite provider, design-time tools, Swagger
dotnet add src/VehicleRegistry.Api package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/VehicleRegistry.Api package Microsoft.EntityFrameworkCore.Design
dotnet add src/VehicleRegistry.Api package Swashbuckle.AspNetCore

## Tests project - EF Core InMemory for fast, no-database tests
dotnet add tests/VehicleRegistry.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/VehicleRegistry.Tests package Microsoft.AspNetCore.Mvc.Testing
