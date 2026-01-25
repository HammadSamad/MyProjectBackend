using Backend_Api.Data;
using Backend_Api.Helpers;
using Backend_Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Add services to the container
// -----------------------------
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// -----------------------------
// Database
// -----------------------------
builder.Services.AddDbContext<LaptopHarbourDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbconnection")));

// -----------------------------
// Custom Services & Helpers
// -----------------------------
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<PasswordHasherHelper>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<OrderAutoCancelService>();
builder.Services.AddHostedService<ExpiredTokenCleanupService>();


// -----------------------------
// JWT Authentication
// -----------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// -----------------------------
// CORS
// -----------------------------
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// -----------------------------
// Middleware pipeline
// -----------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();
