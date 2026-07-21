using AlasApp.Api.Authentication;
using AlasApp.Api.Authorization;
using AlasApp.Api.Middleware;
using AlasApp.Application.Common;
using AlasApp.Domain.Enums;
using AlasApp.Infrastructure;
using AlasApp.Infrastructure.Authentication;
using AlasApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services
    .AddControllers()
    .AddNewtonsoftJson();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (corsOrigins.Length > 0)
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<ContactCaptchaOptions>(builder.Configuration.GetSection(ContactCaptchaOptions.SectionName));
builder.Services.AddSingleton<AlasApp.Application.Abstractions.Services.IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminPermissionAuthorizationHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var tokenVersionValue = context.Principal?.FindFirst("token_version")?.Value;

                if (!Guid.TryParse(userIdValue, out var userId) || !int.TryParse(tokenVersionValue, out var tokenVersion))
                {
                    context.Fail("Token inválido.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AlasAppDbContext>();
                var userAccount = await dbContext.UserAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, context.HttpContext.RequestAborted);

                if (userAccount is null || !userAccount.IsActive || userAccount.TokenVersion != tokenVersion)
                {
                    context.Fail("La sesión ya no es válida.");
                }
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminPolicies.DashboardRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Dashboard, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.UsersRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Usuarios, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.UsersWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Usuarios, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.CircuitsRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Circuitos, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.CircuitsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Circuitos, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.EventsRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Eventos, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.EventsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Eventos, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.CategoriesRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Categorias, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.CategoriesWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Categorias, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.InscriptionsRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Inscritos, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.InscriptionsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Inscritos, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.PaymentsRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Pagos, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.PaymentsWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Pagos, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.TokensRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Tokens, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.TokensWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Tokens, PermissionLevel.Full)));

    options.AddPolicy(AdminPolicies.ConfigurationRead, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Configuracion, PermissionLevel.ReadOnly)));

    options.AddPolicy(AdminPolicies.ConfigurationWrite, policy =>
        policy.RequireAuthenticatedUser()
            .AddRequirements(new AdminPermissionRequirement(AdminModule.Configuracion, PermissionLevel.Full)));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AlasAppDbContext>();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await dbContext.Database.MigrateAsync();
    }

    var bootstrapAdminInitializer = scope.ServiceProvider.GetRequiredService<BootstrapAdminInitializer>();
    await bootstrapAdminInitializer.InitializeAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
