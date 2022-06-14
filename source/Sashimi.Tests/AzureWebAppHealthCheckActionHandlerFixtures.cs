#nullable disable
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Calamari.Azure;
using Calamari.AzureAppService;
using Calamari.Common.Plumbing.Proxies;
using Calamari.Tests.Shared;
using FluentAssertions;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using NUnit.Framework;
using Sashimi.Azure.Accounts;
using Sashimi.Server.Contracts.ActionHandlers;
using Sashimi.Tests.Shared.Server;
using OperatingSystem = Microsoft.Azure.Management.AppService.Fluent.OperatingSystem;

namespace Sashimi.AzureAppService.Tests
{
    [TestFixture]
    class AzureWebAppHealthCheckActionHandlerFixtures
    {
        [Test]
        public async Task WebApp_Is_Found()
        {
            var resourceGroupName = SdkContext.RandomResourceName(nameof(AzureWebAppHealthCheckActionHandlerFixtures), 60);
            var clientId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionClientId);
            var clientSecret = ExternalVariables.Get(ExternalVariable.AzureSubscriptionPassword);
            var tenantId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionTenantId);
            var subscriptionId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionId);

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId,
                AzureEnvironment.AzureGlobalCloud);

            var azure = Microsoft.Azure.Management.Fluent.Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);

            IResourceGroup resourceGroup = null;
            try
            {
                resourceGroup = await azure.ResourceGroups
                    .Define(resourceGroupName)
                    .WithRegion(Region.USWest)
                    .CreateAsync();

                var appServicePlan = await azure.AppServices.AppServicePlans
                    .Define(SdkContext.RandomResourceName(nameof(AzureWebAppHealthCheckActionHandlerFixtures), 60))
                    .WithRegion(resourceGroup.Region)
                    .WithExistingResourceGroup(resourceGroup)
                    .WithPricingTier(PricingTier.BasicB1)
                    .WithOperatingSystem(OperatingSystem.Windows)
                    .CreateAsync();

                var webAppName = SdkContext.RandomResourceName(nameof(AzureWebAppHealthCheckActionHandlerFixtures), 60);
                await azure.WebApps
                    .Define(webAppName)
                    .WithExistingWindowsPlan(appServicePlan)
                    .WithExistingResourceGroup(resourceGroup)
                    .WithRuntimeStack(WebAppRuntimeStack.NETCore)
                    .CreateAsync();

                ActionHandlerTestBuilder.CreateAsync<AzureWebAppHealthCheckActionHandler, Program>()
                    .WithArrange(context =>
                    {
                        context.Variables.Add(AccountVariables.SubscriptionId,
                            subscriptionId);
                        context.Variables.Add(AccountVariables.TenantId,
                            tenantId);
                        context.Variables.Add(AccountVariables.ClientId,
                            clientId);
                        context.Variables.Add(AccountVariables.Password,
                            clientSecret);
                        context.Variables.Add(SpecialVariables.Action.Azure.ResourceGroupName, resourceGroupName);
                        context.Variables.Add(SpecialVariables.Action.Azure.WebAppName, webAppName);
                        context.Variables.Add(SpecialVariables.AccountType, AccountTypes.AzureServicePrincipalAccountType.ToString());
                    })
                    .WithAssert(result => result.WasSuccessful.Should().BeTrue())
                    .Execute();
            }
            finally
            {
                if (resourceGroup != null)
                {
                    azure.ResourceGroups.DeleteByNameAsync(resourceGroupName).Ignore();
                }
            }
        }

        [Test]
        public void WebApp_Is_Not_Found()
        {
            var randomName = SdkContext.RandomResourceName(nameof(AzureWebAppHealthCheckActionHandlerFixtures), 60);
            var clientId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionClientId);
            var clientSecret = ExternalVariables.Get(ExternalVariable.AzureSubscriptionPassword);
            var tenantId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionTenantId);
            var subscriptionId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionId);

            ActionHandlerTestBuilder.CreateAsync<AzureWebAppHealthCheckActionHandler, Program>()
                .WithArrange(context =>
                {
                    context.Variables.Add(AccountVariables.SubscriptionId,
                        subscriptionId);
                    context.Variables.Add(AccountVariables.TenantId,
                        tenantId);
                    context.Variables.Add(AccountVariables.ClientId,
                        clientId);
                    context.Variables.Add(AccountVariables.Password,
                        clientSecret);
                    context.Variables.Add(SpecialVariables.Action.Azure.ResourceGroupName, randomName);
                    context.Variables.Add(SpecialVariables.Action.Azure.WebAppName, randomName);
                    context.Variables.Add(SpecialVariables.AccountType, AccountTypes.AzureServicePrincipalAccountType.ToString());
                })
                .WithAssert(result => result.WasSuccessful.Should().BeFalse())
                .Execute(false);
        }

        /// <summary>
        /// Configuring all the infrastructure required for a proper proxy test (with blocking certain addresses, proxy
        /// server itself etc) is over the top for a test here. We can implicitly test that the proxy settings are being
        /// picked up properly by setting a non-existent property, and ensuring that we fail with connectivity errors
        /// *to the non-existent proxy* rather than a successful healthcheck directly to Azure.
        /// </summary>
        [Test]
        public void ConfiguredProxy_IsUsedForHealthCheck()
        {
            // Arrange
            var randomName = SdkContext.RandomResourceName(nameof(AzureWebAppHealthCheckActionHandlerFixtures), 60);
            var clientId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionClientId);
            var clientSecret = ExternalVariables.Get(ExternalVariable.AzureSubscriptionPassword);
            var tenantId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionTenantId);
            var subscriptionId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionId);

            using var errorReader = new ConsoleErrorReader();
            using (new ProxySettings("non-existent-proxy.local", 3128))
            {
                // Act
                var result = ActionHandlerTestBuilder.CreateAsync<AzureWebAppHealthCheckActionHandler, Program>()
                    .WithArrange(context =>
                    {
                        context.Variables.Add(AccountVariables.SubscriptionId, subscriptionId);
                        context.Variables.Add(AccountVariables.TenantId, tenantId);
                        context.Variables.Add(AccountVariables.ClientId, clientId);
                        context.Variables.Add(AccountVariables.Password, clientSecret);
                        context.Variables.Add(SpecialVariables.Action.Azure.ResourceGroupName, randomName);
                        context.Variables.Add(SpecialVariables.Action.Azure.WebAppName, randomName);
                        context.Variables.Add(SpecialVariables.AccountType, AccountTypes.AzureServicePrincipalAccountType.ToString());
                    })
                    .Execute(false);

                // Assert
                result.Outcome.Should().Be(ExecutionOutcome.Unsuccessful);

                var errors = errorReader.ReadErrors();
                errors.Should().Contain("No such host is known. (non-existent-proxy.local:3128)");
            }
        }

        /// <summary>
        /// Redirects StandardError to a local text reader for the lifetime of the ConsoleErrorReader object,
        /// then allows reading what was written at any stage.
        /// Original StandardError target is restored on dispose.
        /// </summary>
        class ConsoleErrorReader : IDisposable
        {
            private StringWriter stringWriter;
            private TextWriter originalError;

            public ConsoleErrorReader()
            {
                originalError = Console.Error;

                stringWriter = new StringWriter();
                Console.SetError(stringWriter);
            }

            public string ReadErrors()
            {
                return stringWriter.ToString();
            }

            public void Dispose()
            {
                Console.SetError(originalError);
            }
        }

        /// <summary>
        /// Sets a DefaultWebProxy for the lifetime of the ProxySettings object.
        /// Original DefaultWebProxy is restored on dispose.
        /// </summary>
        class ProxySettings : IDisposable
        {
            private IWebProxy? originalProxySettings;

            public ProxySettings(string hostname, int port)
            {
                originalProxySettings = WebRequest.DefaultWebProxy;

                var proxy = new UseCustomProxySettings(hostname, port, null, null);
                WebRequest.DefaultWebProxy = proxy.CreateProxy().Value;
            }

            public void Dispose()
            {
                WebRequest.DefaultWebProxy = originalProxySettings;
            }
        }
    }
}