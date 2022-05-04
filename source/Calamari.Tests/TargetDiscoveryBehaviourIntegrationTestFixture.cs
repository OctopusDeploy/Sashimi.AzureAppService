﻿using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Calamari.Azure;
using Calamari.AzureAppService.Behaviors;
using Calamari.Common.Commands;
using Calamari.Common.Features.Discovery;
using Calamari.Common.Plumbing.Variables;
using Calamari.Tests.Shared;
using Calamari.Tests.Shared.Helpers;
using FluentAssertions;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Rest;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Calamari.AzureAppService.Tests
{
    [TestFixture]
    public class TargetDiscoveryBehaviourIntegrationTestFixture
    {
        private string clientId;
        private string clientSecret;
        private string tenantId;
        private string subscriptionId;
        private string resourceGroupName;
        private string authToken;
        private ResourceGroupsOperations resourceGroupClient;
        private WebSiteManagementClient webMgmtClient;
        private string appName = Guid.NewGuid().ToString();
        private List<string> slots = new List<string> { "blue", "green" };

        [OneTimeSetUp]
        public async Task Setup()
        {
            var resourceManagementEndpointBaseUri =
                Environment.GetEnvironmentVariable(AccountVariables.ResourceManagementEndPoint) ??
                DefaultVariables.ResourceManagementEndpoint;
            var activeDirectoryEndpointBaseUri =
                Environment.GetEnvironmentVariable(AccountVariables.ActiveDirectoryEndPoint) ??
                DefaultVariables.ActiveDirectoryEndpoint;

            resourceGroupName = Guid.NewGuid().ToString();

            clientId = Environment.GetEnvironmentVariable("Azure_OctopusAPITester_ClientId");
            clientSecret = ExternalVariables.Get(ExternalVariable.AzureSubscriptionPassword);
            tenantId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionTenantId);
            subscriptionId = ExternalVariables.Get(ExternalVariable.AzureSubscriptionId);
            var resourceGroupLocation = Environment.GetEnvironmentVariable("AZURE_NEW_RESOURCE_REGION") ?? "eastus";

            authToken = await Auth.GetAuthTokenAsync(activeDirectoryEndpointBaseUri, resourceManagementEndpointBaseUri,
                tenantId, clientId, clientSecret);

            var resourcesClient = new ResourcesManagementClient(subscriptionId,
                new ClientSecretCredential(tenantId, clientId, clientSecret));

            resourceGroupClient = resourcesClient.ResourceGroups;

            var resourceGroup = new ResourceGroup(resourceGroupLocation);
            resourceGroup = await resourceGroupClient.CreateOrUpdateAsync(resourceGroupName, resourceGroup);

            webMgmtClient = new WebSiteManagementClient(new TokenCredentials(authToken))
            {
                SubscriptionId = subscriptionId,
                HttpClient = { BaseAddress = new Uri(DefaultVariables.ResourceManagementEndpoint) },
            };

            var svcPlan = await webMgmtClient.AppServicePlans.BeginCreateOrUpdateAsync(resourceGroup.Name,
                resourceGroup.Name,
                new AppServicePlan(resourceGroup.Location) { Sku = new SkuDescription("S1", "Standard") }
            );

            var tags = new Dictionary<string, string>
            {
                { TargetTags.EnvironmentTagName, "dev" },
                { TargetTags.RoleTagName, "my-azure-app-role" },
            };

            await webMgmtClient.WebApps.BeginCreateOrUpdateAsync(
                resourceGroup.Name,
                appName,
                new Site(resourceGroup.Location, tags: tags) { ServerFarmId = svcPlan.Id });

            foreach (var slot in slots)
            {
                await webMgmtClient.WebApps.BeginCreateOrUpdateSlotAsync(
                    resourceGroup.Name,
                    appName,
                    new Site(resourceGroup.Location, tags: tags) { ServerFarmId = svcPlan.Id },
                    slot);
            }
        }

        [OneTimeTearDown]
        public async Task Cleanup()
        {
            if (resourceGroupClient != null)
                await resourceGroupClient.StartDeleteAsync(resourceGroupName);
        }

        [Test]
        public async Task Execute_FindsWebApp_WhenOneExistsWithCorrectTags()
        {
            // Arrange
            var variables = new CalamariVariables();
            var context = new RunningDeployment(variables);
            this.CreateVariables(context);
            var log = new InMemoryLog();
            var sut = new TargetDiscoveryBehaviour(log);

            // Act
            await sut.Execute(context);

            // Assert
            var expectedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(appName));
            log.StandardOut.Should().Contain(line => line.StartsWith($"##octopus[create-azurewebapptarget name=\"{expectedName}\""));
        }

        [Test]
        public async Task Execute_FindsMultipleWebAppSlots_WhenExistWithCorrectTags()
        {
            // Arrange
            var variables = new CalamariVariables();
            var context = new RunningDeployment(variables);
            this.CreateVariables(context);
            var log = new InMemoryLog();
            var sut = new TargetDiscoveryBehaviour(log);

            // Act
            await sut.Execute(context);

            // Assert
            foreach (var slot in slots)
            {
                var expectedName = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{appName}/{slot}"));
                var expectedSlotName = Convert.ToBase64String(Encoding.UTF8.GetBytes(slot));
                log.StandardOut.Should().Contain(line => line.StartsWith($"##octopus[create-azurewebapptarget name=\"{expectedName}\" azureWebAppSlot=\"{expectedSlotName}\""));
            }            
        }

        private void CreateVariables(RunningDeployment context)
        {
            string targetDiscoveryContext = $@"{{
    ""scope"": {{
        ""spaceName"": ""default"",
        ""environmentName"": ""dev"",
        ""projectName"": ""my-test-project"",
        ""tenantName"": null,
        ""roles"": [""my-azure-app-role""]
    }},
    ""authentication"": {{
        ""accountId"": ""Accounts-1"",
        ""accountDetails"": {{
            ""subscriptionNumber"": ""{subscriptionId}"",
            ""clientId"": ""{clientId}"",
            ""tenantId"": ""{tenantId}"",
            ""password"": ""{clientSecret}"",
            ""azureEnvironment"": """",
            ""resourceManagementEndpointBaseUri"": """",
            ""activeDirectoryEndpointBaseUri"": """"
        }}
    }}
}}
";

            context.Variables.Add("Octopus.TargetDiscovery.Context", targetDiscoveryContext);
        }
    }
}