namespace Calamari.Azure
{
    public sealed class AzureKnownEnvironment
    {
        private AzureKnownEnvironment(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static readonly AzureKnownEnvironment Global = new AzureKnownEnvironment("AzureGlobalCloud");
        public static readonly AzureKnownEnvironment AzureChinaCloud = new AzureKnownEnvironment("AzureChinaCloud");
        public static readonly AzureKnownEnvironment AzureUSGovernment = new AzureKnownEnvironment("AzureUSGovernment");
        public static readonly AzureKnownEnvironment AzureGermanCloud = new AzureKnownEnvironment("AzureGermanCloud");
        
        public static implicit operator AzureKnownEnvironment(string environment)
        {
            if (environment == Global.Value || environment == "AzureCloud") // this environment name is defined in Sashimi.Azure.Accounts.AzureEnvironmentsListAction
                return Global;
            if (environment == AzureChinaCloud.Value)
                return AzureChinaCloud;
            if (environment == AzureUSGovernment.Value)
                return AzureUSGovernment;
            if (environment == AzureGermanCloud.Value)
                return AzureGermanCloud;

            return new AzureKnownEnvironment(environment);
        }
    }
}
