using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using HCI.AIAssistant.API.Managers;
using HCI.AIAssistant.API.Services;


var builder = WebApplication.CreateBuilder(args);


// Replace appsettings.json values with Key Vault values
var keyVaultName = builder.Configuration[$"AppConfigurations{ConfigurationPath.KeyDelimiter}KeyVaultName"];
var secretsPrefix = builder.Configuration[$"AppConfigurations{ConfigurationPath.KeyDelimiter}SecretsPrefix"];

if (string.IsNullOrWhiteSpace(keyVaultName))
{
    throw new ArgumentNullException("KeyVaultName", "KeyVaultName is missing.");
}

if (string.IsNullOrWhiteSpace(secretsPrefix))
{
    throw new ArgumentNullException("SecretsPrefix", "SecretsPrefix is missing.");
}

var keyVaultUri = new Uri($"https://{keyVaultName}.vault.azure.net/");

builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    new DefaultAzureCredential(),
    new CustomSecretManager(secretsPrefix) // Asigură-te că ai clasa CustomSecretManager creată
);



// Configure values based on appsettings.json
builder.Services.Configure<SecretsService>(builder.Configuration.GetSection("Secrets"));
builder.Services.Configure<AppConfigurationsService>(builder.Configuration.GetSection("AppConfigurations"));

// Add services to the container.
builder.Services.AddSingleton<ISecretsService>(
    provider => provider.GetRequiredService<IOptions<SecretsService>>().Value
);

builder.Services.AddSingleton<IAppConfigurationsService>(
    provider => provider.GetRequiredService<IOptions<AppConfigurationsService>>().Value
);

builder.Services.AddSingleton<IParametricFunctions, ParametricFunctions>();


// Servicii standard (deja existente)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------
// Configurare Pipeline HTTP (partea de jos standard)
// ---------------------------------------------------------

// Configure the HTTP request pipeline.
// Poți decomenta liniile de mai jos dacă vrei Swagger și în Development
// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI();
// }

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();