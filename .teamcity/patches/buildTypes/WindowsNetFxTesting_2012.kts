package patches.buildTypes

import jetbrains.buildServer.configs.kotlin.v2019_2.*
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.CommitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.buildFeatures.commitStatusPublisher
import jetbrains.buildServer.configs.kotlin.v2019_2.ui.*

/*
This patch script was generated by TeamCity on settings change in UI.
To apply the patch, change the buildType with id = 'WindowsNetFxTesting_2012'
accordingly, and delete the patch script.
*/
changeBuildType(RelativeId("WindowsNetFxTesting_2012")) {
    vcs {
        add(DslContext.settingsRoot.id!!)
    }
}
