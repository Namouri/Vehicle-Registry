using Microsoft.EntityFrameworkCore;
using VehicleRegistry.Api.Services;
using VehicleRegistry.Core.Data;
using VehicleRegistry.Core.Services;
//startar allt, kopplar
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// IHttpContextAccessor lets HttpCurrentUserService read the current
// request's headers from outside the controller layer.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

// The interceptor is registered as a service so it can receive
// ICurrentUserService via constructor injection, then wired into the
// DbContext below via AddInterceptors.
builder.Services.AddScoped<HistorySaveChangesInterceptor>();

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlite("Data Source=vehicleregistry.db");
    options.AddInterceptors(serviceProvider.GetRequiredService<HistorySaveChangesInterceptor>());
});

var app = builder.Build();

// Apply pending migrations automatically on startup. Fine for a demo
// project; a production system would run migrations as a separate
// deployment step instead.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();  // serves wwwroot/index.html at "/"
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so WebApplicationFactory<Program> can be used in integration tests.
public partial class Program { }
