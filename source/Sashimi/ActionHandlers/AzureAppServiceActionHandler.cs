﻿using Octopus.CoreUtilities;
using Sashimi.AzureScripting;
using Sashimi.Server.Contracts.ActionHandlers;

namespace Sashimi.AzureAppService
{
    class AzureAppServiceActionHandler : IActionHandler
    {
        private const string AzureWebAppDeploymentTargetTypeId = "AzureWebApp";

        public string Id => SpecialVariables.Action.Azure.ActionTypeName;

        public string Name => "Deploy an Azure App Service";

        public string Description => "Deploy a package or container image to an Azure App Service";

        public string? Keywords => null;

        public bool ShowInStepTemplatePickerUI => true;

        public bool WhenInAChildStepRunInTheContextOfTheTargetMachine => false;

        public bool CanRunOnDeploymentTarget => false;

        public ActionHandlerCategory[] Categories => new[]
            {ActionHandlerCategory.BuiltInStep, AzureConstants.AzureActionHandlerCategory};

        public IActionHandlerResult Execute(IActionHandlerContext context)
        {
            if (context.DeploymentTargetType.Some())
            {
                if (context.DeploymentTargetType.Value.Id != AzureWebAppDeploymentTargetTypeId)
                    throw new ControlledActionFailedException(
                        $"The machine {context.DeploymentTargetName.SomeOr("<unknown>")} will not be deployed to because it is not an Azure Web Application deployment target");
            }

            return context.CalamariCommand(AzureConstants.CalamariAzure, "deploy-azure-app-service").WithAzureTools(context)
                .WithStagedPackageArgument().Execute();
        }
    }
}
