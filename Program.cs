using System.Text;
using DotNetEnv;
using MongoDB.Driver;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;

using RealEstate.API.Middleware;
using RealEstate.API.Mappings;

// Auth
using RealEstate.API.Modules.Auth.Dto;
using RealEstate.API.Modules.Auth.Validator;
using RealEstate.API.Modules.Auth.Service;

// User
using RealEstate.API.Modules.User.Dto;
using RealEstate.API.Modules.User.Validator;
using RealEstate.API.Modules.User.Service;

// Property
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Validator;
using RealEstate.API.Modules.Property.Service;

// Owner
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Validator;
using RealEstate.API.Modules.Owner.Service;

// PropertyImage
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Validator;
using RealEstate.API.Modules.PropertyImage.Service;

// PropertyTrace
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Validator;
using RealEstate.API.Modules.PropertyTrace.Service;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
var config = builder.Configuration;

// ==========================================
// ðŸ”¹ CONFIGURACIÃ“N DE MONGODB
// ==========================================
var mongoConnectionString = config["MONGO_CONNECTION"] ?? "mongodb://localhost:27017";
var mongoDbName = config["MONGO_DATABASE"] ?? "RealEstate";

if (string.IsNullOrWhiteSpace(mongoConnectionString))
    throw new InvalidOperationException("La variable de entorno MONGO_CONNECTION no puede ser nula o vacÃ­a.");
if (string.IsNullOrWhiteSpace(mongoDbName))
    throw new InvalidOperationException("La variable de entorno MONGO_DATABASE no puede ser nula o vacÃ­a.");

Console.WriteLine($"MongoDB Connection: {mongoConnectionString}");
Console.WriteLine($"MongoDB Database: {mongoDbName}");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));

// ==========================================
// ðŸ”¹ JSON SIN camelCase
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// ==========================================
// ðŸ”¹ JWT DESDE VARIABLES DE ENTORNO
// ==========================================
var secretKey = config["JWT_SECRET"] ?? throw new InvalidOperationException("La variable JWT_SECRET no estÃ¡ definida");
var issuer = config["JWT_ISSUER"] ?? "RealEstateAPI";
var audience = config["JWT_AUDIENCE"] ?? "UsuariosAPI";
var expiryMinutes = config["JWT_EXPIRY_MINUTES"] ?? config["JWT_EXPIRY"] ?? "60";

builder.Configuration["JwtSettings:SecretKey"] = secretKey;
builder.Configuration["JwtSettings:Issuer"] = issuer;
builder.Configuration["JwtSettings:Audience"] = audience;
builder.Configuration["JwtSettings:ExpiryMinutes"] = expiryMinutes;

// ==========================================
// ðŸ”¹ VALIDACIÃ“N GLOBAL Y FLUENTVALIDATION
// ==========================================
builder.Services.AddControllers(o => o.Filters.Add<ValidationExceptionFilter>())
    .AddNewtonsoftJson(o =>
    {
        o.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(processDictionaryKeys: true, overrideSpecifiedNames: false)
        };
        o.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        o.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
        o.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Forzar nombres de propiedad en validaciónes a camelCase (afecta claves en ModelState)
FluentValidation.ValidatorOptions.Global.PropertyNameResolver = (type, member, expression) =>
{
    string? name = member?.Name;
    if (string.IsNullOrEmpty(name) && expression != null)
    {
        var exprStr = expression.ToString();
        var last = exprStr.Split('.').LastOrDefault();
        name = last;
    }
    return string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);
};

builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    o.InvalidModelStateResponseFactory = ctx =>
{
    var errors = ctx.ModelState
                .Where(x => x.Value != null && x.Value.Errors.Count > 0)
                .SelectMany(v => v.Value!.Errors)
        .Select(e => e.ErrorMessage)
        .ToArray();

    var payload = RealEstate.API.Infraestructure.Core.Logs.ServiceLogResponseWrapper<object>.Fail(
        message: "Errores de validación",
        errors: errors,
        statusCode: 400
    );

    return new BadRequestObjectResult(payload);
};
});

// ==========================================
// ðŸ”¹ CACHÃ‰
// ==========================================
builder.Services.AddMemoryCache();

// ==========================================
// ðŸ”¹ SERVICIOS
// ==========================================
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<IValidator<UserDto>, UserDtoValidator>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<IValidator<PropertyDto>, PropertyDtoValidator>();
builder.Services.AddScoped<PropertyService>();

builder.Services.AddScoped<IValidator<OwnerDto>, OwnerDtoValidator>();
builder.Services.AddScoped<OwnerService>();

builder.Services.AddScoped<IValidator<PropertyImageDto>, PropertyImageDtoValidator>();
builder.Services.AddScoped<PropertyImageService>();

builder.Services.AddScoped<IValidator<PropertyTraceDto>, PropertyTraceDtoValidator>();
builder.Services.AddScoped<PropertyTraceService>();

builder.Services.AddSingleton<JwtService>();
// AutoMapper (si se utiliza MappingProfile)
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

// ==========================================
// ðŸ”¹ CORS
// ==========================================
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ==========================================
// ðŸ”¹ JWT AUTENTICACIÃ“N
// ==========================================
var keyBytes = Encoding.UTF8.GetBytes(secretKey);
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ==========================================
// ðŸ”¹ LOGGING
// ==========================================
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.TimestampFormat = "[HH:mm:ss] ";
    o.SingleLine = true;
});

// ==========================================
// ðŸ”¹ APP
// ==========================================
var app = builder.Build();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages(async context =>
{
    var res = context.HttpContext.Response;
    var message = res.StatusCode switch
    {
        401 => "No autorizado",
        403 => "Prohibido",
        404 => "Recurso no encontrado",
        405 => "Método no permitido",
        415 => "Tipo de contenido no soportado",
        _ => "Error"
    };

    var payload = RealEstate.API.Infraestructure.Core.Logs.ServiceLogResponseWrapper<string>.Fail(
        message: message,
        statusCode: res.StatusCode
    );

    res.ContentType = "application/json";
    var json = System.Text.Json.JsonSerializer.Serialize(
        payload,
        new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        }
    );
    await res.WriteAsync(json);
});
app.MapControllers();
app.Run();







