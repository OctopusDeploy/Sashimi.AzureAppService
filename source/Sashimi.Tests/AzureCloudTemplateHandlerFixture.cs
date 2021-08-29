using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Sashimi.Server.Contracts;

namespace Sashimi.AzureAppService.Tests
{
    public class AzureCloudTemplateHandlerFixture
    {
        IFormatIdentifier formatIdentifier;

        [SetUp]
        public void SetUp()
        {
            formatIdentifier = Substitute.For<IFormatIdentifier>();
            formatIdentifier.IsJson(Arg.Any<string>()).ReturnsForAnyArgs(true);
        }

        [Test]
        public void RespondsToCorrectTemplateAndProvider()
        {
            new AzureCloudTemplateHandler(formatIdentifier).CanHandleTemplate("AzureAppService", "{\"hi\": \"there\"}").Should().BeTrue();
            new AzureCloudTemplateHandler(formatIdentifier).CanHandleTemplate("AzureAppService", "#{blah}").Should().BeTrue();
        }
    }
}