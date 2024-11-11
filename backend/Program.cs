using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Backend.Data;
using Backend.Api;
using Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// Swagger-dokumentaatio
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Used Phones API", Version = "v1" });
});

// CORS-käytäntö
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:5058") // Salli pyynnöt Blazor-frontendistä
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Lisää Newtonsoft.Json tuki
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

var app = builder.Build();

// Käytä CORS-käytäntöä ennen API-reittien rekisteröintiä
app.UseCors("AllowBlazorClient");

// Swagger-käyttöliittymä
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Used Phones API V1");
    c.RoutePrefix = string.Empty; // Swagger UI avautuu suoraan root-URL:ssa
});

// Tietokannan alustaminen asynkronisesti
await DatabaseInitializer.Initialize();

// Rekisteröi API-päätepisteet
app.MapPhonesApi();
app.MapAuthApi();
app.MapCartApi();
app.MapOfferApi(); // Lisää tämä rivi

// Käynnistetään sovellus
app.Run();
