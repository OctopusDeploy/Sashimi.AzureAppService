using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Octopus.Server.Extensibility.Metadata;
using Sashimi.Server.Contracts.CloudTemplates;

namespace Sashimi.AzureAppService
{
    public class AzureCloudTemplateHanlder : ICloudTemplateHandler
    {
        private const string Provider = "AzureAppService";
        private const string Template = "JSON";
        
        public bool CanHandleTemplate(string providerId, string template)
        {
            return providerId == Provider && template == Template;
        }

        public Metadata ParseTypes(string template)
        {
            return new Metadata();
        }

        public object ParseModel(string template)
        {
            // This ensure the input is valid JSON (assuming it doesn't have variable substitution
            if (!template.Contains("#{")) JObject.Parse(template);
            return new Dictionary<string, object>();
        }
    }
}