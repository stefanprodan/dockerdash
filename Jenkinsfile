#!groovy

import groovy.json.JsonSlurperClassic

def getVersion(def projectJson){
    def slurper = new JsonSlurperClassic()
    project = slurper.parseText(projectJson)
    slurper = null
    return project.version.split('-')[0]
}

def version, revision

node {
   stage('Checkout') {
        checkout([$class: 'GitSCM', branches: [[name: '*/master']], doGenerateSubmoduleConfigurations: false, 
            extensions: [[$class: 'CleanBeforeCheckout']], 
            submoduleCfg: [], 
            userRemoteConfigs: [[credentialsId: 'spgit', url: 'https://github.com/stefanprodan/dockerdash']]])
            
        version = getVersion(readFile('src/DockerDash/project.json'))
        revision = version + "-" + sprintf("%04d", env.BUILD_NUMBER.toInteger())
   }
   stage('Build') {
        println "Building version $version revision $revision"
        
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
        
        archiveArtifacts artifacts: 'release/**/*', fingerprint: true
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
        def image = docker.build("dockerdash:$revision")
        docker.withRegistry("https://nexus.osmyk.com", "nexus") {
            image.push()
            image.push('latest')
        }
   }
   stage('Clean') {
        sh("docker rmi -f nexus.osmyk.com/dockerdash:latest || :")
        sh("docker rmi -f nexus.osmyk.com/dockerdash:$revision || :")
        sh("docker rmi -f dockerdash:$revision || :")
   }
}
