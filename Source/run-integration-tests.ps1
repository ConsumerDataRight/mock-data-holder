#Requires -PSEdition Core

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Output "***********************************************************"
Write-Output "MockDataHolder integration tests"
Write-Output ""
Write-Output "⚠ WARNING: Integration tests for MockDataHolder will use the existing 'mock-register' image found on this machine. Rebuild that image if you wish to test with latest code changes for MockRegister"
Write-Output "***********************************************************"

# Run integration tests
docker compose -f docker-compose.IntegrationTests.yml up --build --abort-on-container-exit --exit-code-from mock-data-holder-integration-tests
$_lastExitCode = $LASTEXITCODE

# Stop containers
docker compose -f docker-compose.IntegrationTests.yml down

if ($_lastExitCode -eq 0) {
    Write-Output "***********************************************************"
    Write-Output "✔ SUCCESS: MockDataHolder integration tests passed"
    Write-Output "***********************************************************"
    exit 0
}
else {
    Write-Output "***********************************************************"
    Write-Output "❌ FAILURE: MockDataHolder integration tests failed"
    Write-Output "***********************************************************"
    exit 1
}
