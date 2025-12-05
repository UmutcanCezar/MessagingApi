using api1.Data;
using api1.Hubs;
using api1.repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// *** RENDER ORTAM DEÐÝÞKENÝ DÜZELTMESÝ BAÞLANGICI ***
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = databaseUrl;
}
// *** RENDER ORTAM DEÐÝÞKENÝ DÜZELTMESÝ SONU ***

// CORS ayarý
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500", "https://*.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// VÝTAL DEÐÝÞÝKLÝK: PostgreSQL Baðlantý Dizesi Yapýlandýrmasý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- VÝTAL: OTOMATÝK MÝGRASYON BLOÐU ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabaný migrasyonlarý baþarýyla uygulandý.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabaný migrasyonu sýrasýnda bir hata oluþtu.");
    }
}
// --- OTOMATÝK MÝGRASYON BLOÐU BÝTÝÞÝ ---

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -----------------------------------------------------
// BU SATIR YORUMA ALINDI: Render zaten HTTPS kullandýðý için.
// app.UseHttpsRedirection(); 
// -----------------------------------------------------

app.UseAuthorization();
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();