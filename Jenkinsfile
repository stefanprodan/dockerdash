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
        
        println "Start building version $version revision $revision"
   }
   
   stage('Build test image') {
        def dotnet = docker.image('microsoft/dotnet:latest')
        dotnet.inside("-u root -e DOCKERDASH_USER='admin' -e DOCKERDASH_PASSWORD='changeme' -e DOCKER_REMOTE_API='unix:///var/run/docker.sock'") {
            
            stage('Restore packages') {
                // restore NuGet packages for all projects in solution
                sh("dotnet restore")
            }
            
            stage('Compile project') {
                sh("dotnet build src/DockerDash -c Release")
            }
            
            stage('Run tests') {
                try{
                    sh("dotnet test test/DockerDash.Tests -c Release -xml testresult.xml")
                }catch(err){
                    // test failed
                }
                step([$class: 'XUnitBuilder', testTimeMargin: '3000', thresholdMode: 1, 
                    thresholds: 
                        [[$class: 'FailedThreshold', failureNewThreshold: '0', failureThreshold: '0', unstableNewThreshold: '0', unstableThreshold: '0'],
                        [$class: 'SkippedThreshold', failureNewThreshold: '1', failureThreshold: '1', unstableNewThreshold: '0', unstableThreshold: '0']],
                    tools: [[$class: 'XUnitDotNetTestType', 
                        deleteOutputFiles: true, 
                        failIfNotNew: false, 
                        pattern: 'testresult.xml', skipNoTestFiles: true, stopProcessingIfError: true]]])
            }
            
            stage('Publish artifacts') {
                // publish web app to the release dir
                sh("mkdir release")
                sh("dotnet publish -c Release -o \$(pwd)/release src/DockerDash")
                
                archiveArtifacts artifacts: 'release/**/*', fingerprint: true
            }
            
            // give jenkins user ownership of build artifacts
            sh("chown -R 1000 src")
            sh("chown -R 1000 test")
            sh("chown -R 1000 release")
        }
   }
   
   stage('Publish image') {
        def image = docker.build("dockerdash:$revision")
        docker.withRegistry("https://nexus.cifire.com", "nexus") {
            image.push()
            image.push('latest')
        }
   }
   
   stage('Remove local images') {
        // remove docker images
        sh("docker rmi -f nexus.cifire.com/dockerdash:latest || :")
        sh("docker rmi -f nexus.cifire.com/dockerdash:$revision || :")
        sh("docker rmi -f dockerdash:$revision || :")
   }
}

