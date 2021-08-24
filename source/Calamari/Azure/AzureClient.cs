using System;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace Calamari.Azure
{
    internal static class AzureClient
    {
        public static IAzure CreateAzureClient(this ServicePrincipalAccount servicePrincipal)
        {
            AzureEnvironment GetAzureEnvironment(AzureKnownEnvironment environment)
            {
                return !string.IsNullOrEmpty(environment.Value)
                    ? AzureEnvironment.FromName(environment.Value) ?? throw new InvalidOperationException($"Unknown environment name {environment.Value}")
                    : AzureEnvironment.AzureGlobalCloud;
            }

            return Microsoft.Azure.Management.Fluent.Azure.Configure()
                .Authenticate(
                    SdkContext.AzureCredentialsFactory.FromServicePrincipal(servicePrincipal.ClientId,
                        servicePrincipal.Password, servicePrincipal.TenantId,
                        GetAzureEnvironment(new AzureKnownEnvironment(servicePrincipal.AzureEnvironment))
                    ))
                .WithSubscription(servicePrincipal.SubscriptionNumber);
        }
    }
}
