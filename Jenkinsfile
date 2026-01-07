pipeline {
    agent {
        docker { image 'mcr.microsoft.com/dotnet/sdk:8.0' }
    }
    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
        stage('Restore') {
            steps {
                sh 'dotnet restore EntityTestApi/EntityTestApi.csproj'
            }
        }
        stage('Build') {
            steps {
                sh 'dotnet build EntityTestApi/EntityTestApi.csproj --configuration Release'
            }
        }
        stage('Test') {
            steps {
                sh 'dotnet test EntityTestApi.Tests/EntityTestApi.Tests.csproj --configuration Release'
                sh 'dotnet test EntityTestApi.NUnit.Tests/EntityTestApi.NUnit.Tests.csproj --configuration Release'
                sh 'dotnet test EntityTestApi.MSTest.Tests/EntityTestApi.MSTest.Tests.csproj --configuration Release'
            }
        }
        stage('Publish') {
            steps {
                sh 'dotnet publish EntityTestApi/EntityTestApi.csproj --configuration Release --output ./publish'
            }
        }
    }
    post {
        always {
            archiveArtifacts artifacts: '**/publish/**', allowEmptyArchive: true
        }
        failure {
            echo 'Pipeline failed.'
        }
        success {
            echo 'Pipeline succeeded.'
        }
    }
}
