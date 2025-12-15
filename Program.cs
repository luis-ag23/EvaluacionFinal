using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using ProyectoFinalTecWeb.Data;
using ProyectoFinalTecWeb.Repositories;
using ProyectoFinalTecWeb.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging detallado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Mostrar todas las variables de entorno relevantes
Console.WriteLine("=== ENVIRONMENT VARIABLES ===");
var envVars = new[] { "DATABASE_URL", "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE", "PORT" };
foreach (var envVar in envVars)
{
    var value = Environment.GetEnvironmentVariable(envVar);
    Console.WriteLine($"{envVar}: {(string.IsNullOrEmpty(value) ? "NOT SET" : "SET")}");
}
Console.WriteLine("==============================");

// Puerto para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"Server will run on port: {port}");

// Servicios básicos
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// JWT
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "DefaultKeyForDevelopment1234567890ABCDEFGH==";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "TaxiApi";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "TaxiClient";

Console.WriteLine($"JWT - Issuer: {jwtIssuer}, Audience: {jwtAudience}");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey.PadRight(32, '=')[..32]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.Zero
        };
    });

// Configurar conexión a PostgreSQL
var connectionString = GetConnectionString();
Console.WriteLine($"Connection String (masked): {GetMaskedConnectionString(connectionString)}");

// Registrar DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging(false));

// Registrar servicios
RegisterServices(builder.Services);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

// Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// base de datos
await SetupDatabaseAsync(app);

// Endpoints básicos
app.MapGet("/health", async (AppDbContext dbContext) =>
{
    try
    {
        // Verificar conexión a la base de datos
        var canConnect = await dbContext.Database.CanConnectAsync();

        return Results.Json(new
        {
            status = canConnect ? "healthy" : "unhealthy",
            database = canConnect ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow,
            service = "Taxi API"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "unhealthy",
            error = ex.Message,
            timestamp = DateTime.UtcNow
        }, statusCode: 503);
    }
});

app.MapGet("/", () =>
{
    return Results.Json(new
    {
        status = "ok",
        message = "Taxi API is running",
        timestamp = DateTime.UtcNow
    });
});
app.MapGet("/healthz", () => Results.Json(new { status = "OK" }));

app.MapGet("/db-status", async (HttpContext httpContext) => {
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var canConnect = await db.Database.CanConnectAsync();
        var tables = new List<string>();

        if (canConnect)
        {
            using var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                ORDER BY table_name";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        await httpContext.Response.WriteAsJsonAsync(new
        {
            database_connected = canConnect,
            table_count = tables.Count,
            tables = tables,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        await httpContext.Response.WriteAsJsonAsync(new
        {
            database_connected = false,
            error = ex.Message,
            timestamp = DateTime.UtcNow
        });
    }
});

app.MapControllers();

Console.WriteLine("Application is starting, waiting 5 seconds for health checks...");
await Task.Delay(5000); // Espera 5 segundos para que todo esté listo


app.Run();

string GetConnectionString()
{
    // 1. Intentar con DATABASE_URL de Railway
    var railwayUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(railwayUrl))
    {
        Console.WriteLine("Using Railway DATABASE_URL");
        try
        {
            var uri = new Uri(railwayUrl);
            Console.WriteLine($"   Database host: {uri.Host}");
            Console.WriteLine($"   Database port: {uri.Port}");
            Console.WriteLine($"   Database name: {uri.AbsolutePath.Trim('/')}");

            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);

            var cs = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port,
                Database = uri.AbsolutePath.Trim('/'),
                Username = username,
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
                SslMode = SslMode.Require,
                TrustServerCertificate = true,
                Pooling = true,
                MaxPoolSize = 10,
                Timeout = 30,
                CommandTimeout = 30
            };

            return cs.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
        }
    }

    // 2. Variables individuales
    Console.WriteLine("Using individual environment variables");
    var dbHost = Environment.GetEnvironmentVariable("PGHOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("PGDATABASE") ?? "TaxiDB";
    var dbUser = Environment.GetEnvironmentVariable("PGUSER") ?? "postgres";
    var dbPass = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "postgres";

    Console.WriteLine($"   Host: {dbHost}, Database: {dbName}, User: {dbUser}");

    return $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};" +
           "SSL Mode=Require;Trust Server Certificate=true";
}

string GetMaskedConnectionString(string connectionString)
{
    try
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return $"Host={builder.Host};Port={builder.Port};Database={builder.Database};Username={builder.Username};Password=***";
    }
    catch
    {
        return "Invalid connection string";
    }
}

void RegisterServices(IServiceCollection services)
{
    services.AddScoped<IDriverRepository, DriverRepository>();
    services.AddScoped<IPassengerRepository, PassengerRepository>();
    services.AddScoped<ITripRepository, TripRepository>();
    services.AddScoped<IVehicleRepository, VehicleRepository>();
    services.AddScoped<IModelRepository, ModelRepository>();

    services.AddScoped<IDriverService, DriverService>();
    services.AddScoped<IPassengerService, PassengerService>();
    services.AddScoped<ITripService, TripService>();
    services.AddScoped<IVehicleService, VehicleService>();
    services.AddScoped<IModelService, ModelService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IDriverVehicleService, DriverVehicleService>();
}

async Task SetupDatabaseAsync(WebApplication app)
{
    Console.WriteLine("\nSETTING UP DATABASE...");

    // Crear un scope MANUALMENTE
    var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

    using var scope = scopeFactory.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        // 1. Verificar conexión
        Console.WriteLine("Testing database connection...");
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (!canConnect)
        {
            Console.WriteLine("Cannot connect to database");
            Console.WriteLine("Connection string used: " + GetMaskedConnectionString(dbContext.Database.GetConnectionString()));
            return;
        }

        Console.WriteLine("Database connection successful");

        // 2. Verificar si existe la tabla de migraciones
        Console.WriteLine("Checking for migrations table...");
        using var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '__efmigrationshistory'";
        var migrationsTableExists = (long)command.ExecuteScalar()! > 0;

        Console.WriteLine($"Migrations table exists: {migrationsTableExists}");

        // 3. Verificar archivos de migración en el proyecto
        var migrationFilesExist = Directory.Exists("Data/Migrations") &&
                                 Directory.GetFiles("Data/Migrations", "*.cs").Length > 0;
        Console.WriteLine($"Migration files in project: {migrationFilesExist}");

        if (migrationFilesExist)
        {
            // Intentar usar migraciones
            try
            {
                Console.WriteLine("Attempting to apply migrations...");
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying migrations: {ex.Message}");
                Console.WriteLine("Falling back to EnsureCreated...");
                await dbContext.Database.EnsureCreatedAsync();
                Console.WriteLine("Database created via EnsureCreated");
            }
        }
        else
        {
            // No hay archivos de migración, usar EnsureCreated
            Console.WriteLine("No migration files found. Using EnsureCreated...");
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("Database created via EnsureCreated");
        }

        // 4. Verificar tablas creadas
        Console.WriteLine("\nCHECKING CREATED TABLES:");
        command.CommandText = @"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            ORDER BY table_name";

        using var reader = await command.ExecuteReaderAsync();
        var tables = new List<string>();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Console.WriteLine($"Total tables: {tables.Count}");
        foreach (var table in tables)
        {
            Console.WriteLine($"  - {table}");
        }

        if (tables.Count == 0)
        {
            Console.WriteLine("WARNING: No tables were created!");
            Console.WriteLine("This might indicate:");
            Console.WriteLine("  1. Connection to wrong database");
            Console.WriteLine("  2. Database user lacks CREATE permissions");
            Console.WriteLine("  3. Entity Framework configuration issue");
        }

        Console.WriteLine("DATABASE SETUP COMPLETE\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DATABASE SETUP FAILED: {ex.Message}");
        Console.WriteLine($"Exception type: {ex.GetType().Name}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");

        // Para debugging: mostrar inner exception si existe
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}