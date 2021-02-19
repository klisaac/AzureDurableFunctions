using System;
using System.Reflection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Azure.DurableFunctions.Configuration;
using Microsoft.Azure.WebJobs.Host.Bindings;

[assembly: FunctionsStartup(typeof(Azure.DurableFunctions.Startup))]
namespace Azure.DurableFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<AppConfiguration>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(AppConfiguration)).Bind(settings);
                });

            var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            builder.Services.PostConfigure<ServiceBusOptions>(options =>
            {
                options.ConnectionString = configuration["servicebus-con"];
                options.MessageHandlerOptions.AutoComplete = false;
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var builtConfig = builder.ConfigurationBuilder.Build();
            var keyVaultEndpoint = builtConfig["AzureKeyVaultEndpoint"];

            if (string.IsNullOrWhiteSpace(keyVaultEndpoint))
                keyVaultEndpoint = Environment.GetEnvironmentVariable("AzureKeyVaultEndpoint", EnvironmentVariableTarget.Process);

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

            builder.ConfigurationBuilder
                .AddAzureKeyVault(keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager())
                .SetBasePath(builder.GetContext().ApplicationRootPath)
                .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
