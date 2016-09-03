# Taken from https://github.com/jbogard/MediatR

<#
.SYNOPSIS
    You can add this to you build script to ensure that psbuild is available before calling
    Invoke-MSBuild. If psbuild is not available locally it will be downloaded automatically.
#>
function EnsurePsbuildInstalled{
    [cmdletbinding()]
    param(
        [string]$psbuildInstallUri = 'https://raw.githubusercontent.com/ligershark/psbuild/master/src/GetPSBuild.ps1'
    )
    process{
        if(-not (Get-Command "Invoke-MsBuild" -errorAction SilentlyContinue)){
            'Installing psbuild from [{0}]' -f $psbuildInstallUri | Write-Verbose
            (new-object Net.WebClient).DownloadString($psbuildInstallUri) | iex
        }
        else{
            'psbuild already loaded, skipping download' | Write-Verbose
        }

        # make sure it's loaded and throw if not
        if(-not (Get-Command "Invoke-MsBuild" -errorAction SilentlyContinue)){
            throw ('Unable to install/load psbuild from [{0}]' -f $psbuildInstallUri)
        }
    }
}

# Taken from psake https://github.com/psake/psake

<#
.SYNOPSIS
  This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
  to see if an error occcured. If an error is detected then an exception is thrown.
  This function allows you to run command-line programs without having to
  explicitly check the $lastexitcode variable.
.EXAMPLE
  exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
#>
function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ("Exec: " + $errorMessage)
    }
}

if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }
if(Test-Path .\tmp) { Remove-Item .\tmp -Force -Recurse }
if(Test-Path .\release) { Remove-Item .\release -Force -Recurse }

EnsurePsbuildInstalled

exec { & dotnet restore }

Invoke-MSBuild

$revision = @{ $true = $env:APPVEYOR_BUILD_NUMBER; $false = 1 }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$revision = "{0:D4}" -f [convert]::ToInt32($revision, 10)

exec { & dotnet restore .\src\Docker.DotNet }
exec { & dotnet restore .\src\DockerDash }
exec { & dotnet build .\src\DockerDash }

$release = Join-Path $pwd release
exec { & dotnet publish .\src\DockerDash -c Release -o $release --version-suffix=$revision}

$packToZip = $false
if($packToZip){

    $tmp = Join-Path $pwd tmp
    exec { & dotnet publish .\src\DockerDash -c Release -o $tmp --version-suffix=$revision}

    # zip pack to .\artifacts\name-version.zip
    $json = Get-Content -Raw -Path (Join-Path $pwd 'src\DockerDash\project.json') | ConvertFrom-Json
    $version = $json.version
    $pack = Join-Path $pwd "artifacts/dockerdash-${version}.zip"
    New-Item -ItemType Directory -Force -Path (Join-Path $pwd artifacts) | Out-Null
    Add-Type -Assembly "System.IO.Compression.FileSystem"
    [System.IO.Compression.ZipFile]::CreateFromDirectory($tmp, $pack)
}
