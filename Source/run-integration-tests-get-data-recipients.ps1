#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "GetDataRecipients integration tests"
Write-Output ""
Write-Output "⚠ WARNING: Integration tests for GetDataRecipients will use the existing 'mock-register' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockRegister"
Write-Output "***********************************************************"

# Run integration tests
docker-compose -f docker-compose.GetDataRecipients.IntegrationTests.yml up --build --abort-on-container-exit --exit-code-from getdatarecipients-integration-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker-compose -f docker-compose.GetDataRecipients.IntegrationTests.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: GetDataRecipients integration tests passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: GetDataRecipients integration tests failed"
    Write-Output "***********************************************************"
    exit 1
}
