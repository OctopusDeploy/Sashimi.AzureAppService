#nullable disable
using System;
using System.Collections.Generic;
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

            using (new ProxySettingsMemento("non-existent-proxy.local", 3128))
            using (var errorReader = new ConsoleErrorReaderMemento())
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
                    .Execute(assertWasSuccess: false);

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
        private class ConsoleErrorReaderMemento : AutoResetMemento<TextWriter>
        {
            private readonly StringWriter stringWriter;

            public ConsoleErrorReaderMemento()
                : base(() => Console.Error, Console.SetError)
            {
                stringWriter = new StringWriter();
                base.SetValue(stringWriter);
            }

            public string ReadErrors()
            {
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Sets a Proxy Settings for the lifetime of the ProxySettingsMemento object.
        /// Original settings are restored on dispose.
        /// </summary>
        /// <remarks>
        /// Calamari Tests operate differently on CI (out of process Calamari.exe) and locally (in-process code execution).
        /// When in-process, the WebRequest.DefaultWebProxy is used. When out-of-process, the EnvVars are used.
        /// </remarks>
        private class ProxySettingsMemento : IDisposable
        {
            private readonly List<IDisposable> mementos;

            public ProxySettingsMemento(string hostname, int port)
            {
                mementos = new List<IDisposable>();

                SetLocalEnvironmentProxySettings(hostname, port);
                SetCiEnvironmentProxySettings(hostname, port);
            }

            private void SetLocalEnvironmentProxySettings(string hostname, int port)
            {
                var proxySettings = new UseCustomProxySettings(hostname, port, null!, null!).CreateProxy().Value;
                mementos.Add(new AutoResetMemento<IWebProxy?>(() => WebRequest.DefaultWebProxy, v => WebRequest.DefaultWebProxy = v, proxySettings));
            }

            private void SetCiEnvironmentProxySettings(string hostname, int port)
            {
                mementos.Add(new EnvironmentVariableMemento(EnvironmentVariables.TentacleProxyHost, hostname));
                mementos.Add(new EnvironmentVariableMemento(EnvironmentVariables.TentacleProxyPort, $"{port}"));
            }

            public void Dispose()
            {
                mementos.ForEach(m => m.Dispose());
            }
        }

        /// <summary>
        /// Sets an Environment Variable for the lifetime of the EnvironmentVariableMemento object.
        /// Original setting is restored on dispose.
        /// </summary>
        private class EnvironmentVariableMemento : AutoResetMemento<string>
        {
            public EnvironmentVariableMemento(string variableName, string testValue)
                : base(() => Environment.GetEnvironmentVariable(variableName),
                    value => Environment.SetEnvironmentVariable(variableName, value),
                    testValue)
            {
            }
        }

        /// <summary>
        /// A not-quite implementation of the Memento pattern, which enables a single original value to be remembered,
        /// temporarily overridden with a new value, and then automatically set back to the original value at the end
        /// of the object's lifetime.
        ///
        /// Useful for managing potentially-shared state, though in a simplistic way.
        /// </summary>
        /// <remarks>
        /// Does not attempt to handle concurrency: may produce unexpected results if multiple AutoResetMementos
        /// are modifying the same shared state (eg Environment variables) across threads.
        /// </remarks>
        /// <typeparam name="TValue"></typeparam>
        private class AutoResetMemento<TValue> : IDisposable
        {
            private readonly TValue originalValue;
            private readonly Action<TValue> setter;

            public AutoResetMemento(Func<TValue> getter, Action<TValue> setter)
            {
                originalValue = getter();
                this.setter = setter;
            }

            public AutoResetMemento(Func<TValue> getter, Action<TValue> setter, TValue newValue)
                : this(getter, setter)
            {
                SetValue(newValue);
            }

            public void SetValue(TValue value)
            {
                setter(value);
            }

            public void Dispose()
            {
                setter(originalValue);
            }
        }
    }
}
