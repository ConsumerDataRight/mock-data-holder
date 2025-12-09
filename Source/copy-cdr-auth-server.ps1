# Files from CDR-Auth-Server are needed by Dockerfile.with-auth-server
# This script copies those files so that the docker image can be built locally.

#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Clear-Host
if ($host.UI.PromptForChoice("Confirm", "Copy files from ..\..\cdr-auth-server into cdr-auth-server?", @("&Yes", "&No"), 1) -ne 0) {
    exit 1
}

$source = Resolve-Path ..\..\cdr-auth-server

# Using git clone is much faster as it respects .gitignore to not copy compiled artefacts and other irrelevant files that may exist in $source
git clone $source