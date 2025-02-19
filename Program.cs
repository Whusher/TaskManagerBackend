using MongoDB.Driver;  // Importa la directiva para MongoDB
using Microsoft.AspNetCore.Builder;  // Para usar el middleware en el pipeline
using Microsoft.Extensions.DependencyInjection;  // Para configurar la inyección de dependencias
using Microsoft.Extensions.Hosting;  // Para verificar el entorno de ejecución
using ApiCSharp.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "PublicPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Permite solo este origen
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
    }
    );
});

// SERVICIO DE JWT
var keyBase64 = builder.Configuration["Jwt:Key"];
var keyBytes = Convert.FromBase64String(keyBase64);
if (keyBytes.Length < 32)
{
    throw new ArgumentException("Jwt:Key en Base64 debe tener al menos 32 bytes.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\": \"El token ha expirado, por favor inicia sesión nuevamente.\"}");
                }
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization();

// Registra MongoDBService como un servicio singleton
builder.Services.AddSingleton<MongoService>();

// Agregar token service a la inyeccion de dependencias
builder.Services.AddSingleton<TokenService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//Add CORS Policy
app.UseCors("PublicPolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
