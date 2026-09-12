using Microsoft.EntityFrameworkCore;
using Store.Api.Data;
using Store.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQLite locally (no server to install, disposable dev data), Postgres in
// production (Render's managed database) - see CLAUDE.md Key Trap 7. Default
// is Sqlite so local dev needs no extra configuration; Render sets
// Database__Provider=Postgres explicitly.
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("StoreDb");

builder.Services.AddDbContext<StoreDbContext>(options =>
{
    if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// See Services/IPaymentIntentGateway.cs - keyless at registration time,
// CheckoutController passes the secret key (read from configuration at the
// point of use) as an argument per call. Registering the interface here
// (not the concrete StripePaymentIntentGateway) is what makes it trivially
// substitutable with a Moq mock in tests.
builder.Services.AddScoped<IPaymentIntentGateway, StripePaymentIntentGateway>();

// The cart depends on a session cookie, so the frontend origin must be
// explicitly allowed with credentials - AllowAnyOrigin() and
// AllowCredentials() are mutually exclusive in ASP.NET Core, so the origin
// list has to be explicit (falls back to the Angular dev server if
// Cors:AllowedOrigins isn't set; override it once a production frontend
// origin exists).
const string FrontendCorsPolicy = "FrontendCors";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Prepares the database on startup so no separate manual step is needed
// (locally or on Render). Sqlite has no versioned migration history in this
// project (see CLAUDE.md Key Trap 7 - EF Core migrations aren't portable
// between providers, and the local SQLite file is disposable dev data
// anyway), so it just gets its schema created directly; Postgres applies the
// real, checked-in migration set.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
    if (databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.MapControllers();

app.Run();

// Top-level statements generate an internal Program class by default; making
// it public lets Store.Api.Tests reference it via WebApplicationFactory<Program>
// for integration tests (guideline 07).
public partial class Program { }
