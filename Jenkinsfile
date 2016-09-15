#!groovy

node {
   stage('Checkout') {
        checkout([$class: 'GitSCM', branches: [[name: '*/master']], doGenerateSubmoduleConfigurations: false, 
            extensions: [[$class: 'CleanBeforeCheckout']], 
            submoduleCfg: [], 
            userRemoteConfigs: [[credentialsId: 'spgit', url: 'https://github.com/stefanprodan/dockerdash']]])
   }
   stage('Build') {
        def dotnet = docker.image('microsoft/dotnet:latest')
        dotnet.inside('-u root') {
            // restore NuGet packages for all projects in solution
            sh("dotnet restore")
            // publish web app to the release dir
            sh("mkdir release")
            sh("dotnet publish -c Release -o \$(pwd)/release src/DockerDash")
            // give jenkins user ownership of build artifacts
            sh("chown -R 1000 src")
            sh("chown -R 1000 release")
        }
   }
   stage('Test') {
       step([$class: 'XUnitBuilder', testTimeMargin: '3000', thresholdMode: 1, 
        thresholds: 
            [[$class: 'FailedThreshold', failureNewThreshold: '1', failureThreshold: '1', unstableNewThreshold: '1', unstableThreshold: '1'],
            [$class: 'SkippedThreshold', failureNewThreshold: '1', failureThreshold: '1', unstableNewThreshold: '1', unstableThreshold: '1']],
        tools: [[$class: 'XUnitDotNetTestType', 
            deleteOutputFiles: true, 
            failIfNotNew: false, 
            pattern: '**/*.testresult', skipNoTestFiles: true, stopProcessingIfError: true]]])
   }
   stage('Publish') {
        def image = docker.build("dockerdash:1.0.0.${env.BUILD_NUMBER}")
        docker.withRegistry("https://nexus.osmyk.com", "nexus") {
            image.push()
            image.push('latest')
        }
   }
   stage('Clean') {
       sh("docker rmi -f nexus.osmyk.com/dockerdash:latest")
        sh("docker rmi -f nexus.osmyk.com/dockerdash:1.0.0.${env.BUILD_NUMBER}")
        sh("docker rmi -f dockerdash:1.0.0.${env.BUILD_NUMBER}")
   }
}
