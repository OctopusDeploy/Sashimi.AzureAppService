package patches.buildTypes

import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.CommitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.commitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.NuGetPublishStep
import jetbrains.buildServer.configs.kotlin.v2019_2.buildSteps.nuGetPublish
import jetbrains.buildServer.configs.kotlin.v2019_2.ui.*

/*
This patch script was generated by TeamCity on settings change in UI.
To apply the patch, change the buildType with id = 'PublishToFeedzIo'
accordingly, and delete the patch script.
*/
changeBuildType(RelativeId("PublishToFeedzIo")) {
    check(description == "") {
        "Unexpected description: '$description'"
    }
    description = "Build chain for Sashimi.AzureAppService"

    expectSteps {
        nuGetPublish {
            name = "Nuget Publish"
            toolPath = "%teamcity.tool.NuGet.CommandLine.DEFAULT%"
            packages = "*.nupkg"
            serverUrl = "%InternalNuget.OctopusDependeciesFeedUrl%"
            apiKey = "credentialsJSON:a7d4426a-7256-4df7-a953-266292e6ad81"
            args = "-Timeout 1200"
        }
    }
    steps {
        update<NuGetPublishStep>(0) {
            clearConditions()
            param("org.jfrog.artifactory.selectedDeployableServer.downloadSpecSource", "Job configuration")
            param("org.jfrog.artifactory.selectedDeployableServer.useSpecs", "false")
            param("org.jfrog.artifactory.selectedDeployableServer.uploadSpecSource", "Job configuration")
        }
    }

    features {
        val feature1 = find<CommitStatusPublisher> {
            commitStatusPublisher {
                publisher = github {
                    githubUrl = "https://api.github.com"
                    authType = personalToken {
                        token = "credentialsJSON:d2d6ff31-56f1-4893-a448-f7a517da6c88"
                    }
                }
            }
        }
        feature1.apply {
            publisher = github {
                githubUrl = "https://api.github.com"
                authType = personalToken {
                    token = "credentialsJSON:7416c240-5c67-48ed-97a3-f5fe49d0e744"
                }
            }
        }
    }
}
