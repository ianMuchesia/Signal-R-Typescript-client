using Microsoft.AspNetCore.Authentication.JwtBearer;

using server.AppDataContext;
using server.Models;
using server.Hubs;
using Microsoft.IdentityModel.Tokens;
using System.Text;  // Add this line to import the Hubs namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
builder.Services.Configure<DbSettings>(builder.Configuration.GetSection("DbSettings"));
builder.Services.AddSingleton<ChatDBContext>();

builder.Services.AddProblemDetails();
builder.Services.AddLogging();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key")))
        };
    });

builder.Services.AddAuthorization();
// CORS configuration
builder.Services.AddCors(options =>             
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder
                .SetIsOriginAllowed(_ => true)  // Allow any origin
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();  // This is required for SignalR
        });
});

var app = builder.Build();

// Use CORS before other middleware
app.UseCors("AllowAllOrigins");

if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Add this line to map your Hub
app.MapHub<ChatHub>("/chathub");  // Add this line

app.Run();