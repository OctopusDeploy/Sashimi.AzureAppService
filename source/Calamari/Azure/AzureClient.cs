using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace Calamari.Azure
{
    internal static class AzureClient
    {
        public static IAzure CreateAzureClient(this ServicePrincipalAccount servicePrincipal)
        {
            AzureEnvironment GetAzureEnvironment(string environmentName)
            {
                return !string.IsNullOrEmpty(environmentName)
                    ? AzureEnvironment.FromName(environmentName) ?? AzureEnvironment.AzureGlobalCloud
                    : AzureEnvironment.AzureGlobalCloud;
            }

            return Microsoft.Azure.Management.Fluent.Azure.Configure()
                .Authenticate(
                    SdkContext.AzureCredentialsFactory.FromServicePrincipal(servicePrincipal.ClientId,
                        servicePrincipal.Password, servicePrincipal.TenantId,
                        GetAzureEnvironment(servicePrincipal.AzureEnvironment)
                    ))
                .WithSubscription(servicePrincipal.SubscriptionNumber);
        }
    }
}
