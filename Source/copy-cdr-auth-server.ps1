# Files from CDR-Auth-Server are needed by Dockerfile.with-auth-server
# This script copies those files so that the docker image can be built locally.

#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Clear-Host
if ($host.UI.PromptForChoice("Confirm", "Copy files from ..\..\cdr-auth-server into cdr-auth-server?", @("&Yes", "&No"), 1) -ne 0) {
    exit 1
}

copy-item ..\..\cdr-auth-server\Source\CdrAuthServer\. cdr-auth-server\Source\CdrAuthServer -Recurse
copy-item ..\..\cdr-auth-server\Source\CdrAuthServer.Domain\. cdr-auth-server\Source\CdrAuthServer.Domain -Recurse
copy-item ..\..\cdr-auth-server\Source\CdrAuthServer.Repository\. cdr-auth-server\Source\CdrAuthServer.Repository -Recurse
copy-item ..\..\cdr-auth-server\Source\CdrAuthServer.Infrastructure\. cdr-auth-server\Source\CdrAuthServer.Infrastructure -Recurse
copy-item ..\..\cdr-auth-server\Source\CdrAuthServer.API.Logger\. cdr-auth-server\Source\CdrAuthServer.API.Logger -Recurse
copy-item ..\..\cdr-auth-server\Source\CdrAuthServer.UI\. cdr-auth-server\Source\CdrAuthServer.UI -Recurse

copy-item ..\..\cdr-auth-server\Source\Directory.Build.props cdr-auth-server\Source\Directory.Build.props
copy-item ..\..\cdr-auth-server\Source\.editorconfig cdr-auth-server\Source\.editorconfig