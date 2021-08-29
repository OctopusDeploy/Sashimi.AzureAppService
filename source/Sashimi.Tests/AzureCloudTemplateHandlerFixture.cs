using System;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Sashimi.AzureAppService.Tests
{
    public class AzureCloudTemplateHandlerFixture
    {
        [Test]
        public void RespondsToCorrectTemplateAndProvider()
        {
            new AzureCloudTemplateHanlder().CanHandleTemplate("AzureAppService", "JSON").Should().BeTrue();
            new AzureCloudTemplateHanlder().CanHandleTemplate("AzureAppService", "YAML").Should().BeFalse();
        }

        [Test]
        public void ReturnsWithValidJSON()
        {
            new AzureCloudTemplateHanlder().ParseModel("{\"hi\": \"there\"}").Should().NotBeNull();
        }
        
        [Test]
        public void ThrowsWithInvalidJSON()
        {
            Action act = () => new AzureCloudTemplateHanlder().ParseModel("I am a string");
                
            act.Should().Throw<JsonReaderException>();
        }
    }
}