# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [Unreleased]
### Changed
- Updated Authorisation Server git clone command in readme

## [1.2.0] - 2023-03-21
### Added
- The Mock Data Holder now utilises the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) as the Identity Provider
- Get Metrics API

### Changed 
- Updated to be compliant with FAPI 1.0 phase 3
- Removed Identity Server 4 project
- Removed Get Data Recipients Azure Function

## [1.1.1] - 2022-10-19
### Fixed
- Fix for Content-Type check in JwtInputFormatter used in DCR

## [1.1.0] - 2022-10-05
### Added
- Logging middleware to create a centralised list of all API requests and responses

### Fixed
- Updated supported response modes in OIDC discovery endpoint. [Issue 46](https://github.com/ConsumerDataRight/mock-data-holder/issues/46)

## [1.0.1] - 2022-08-30
### Changed
- Updated package references.

## [1.0.0] - 2022-07-22
### Added
- Azure function to perform Data Recipient discovery by polling the Get Data Recipients API of the Register.

### Changed
- First version of the Mock Data Holder deployed into the CDR Sandbox.

## [0.3.1] - 2022-06-09
### Changed
- Account transactions dates and person information in seed data.
- Build and Test action to download containers from docker hub.

### Fixed
- Issuing of refresh_token when FapiComplianceLevel is set to Fapi1Phase2.
- Intermittent issue when creating the LogEventsManageAPI database table.

## [0.3.0] - 2022-05-25
### Changed
- Upgraded the Mock Register codebase to .NET 6.
- Replaced SQLite database with MSSQL database.
- Changed the TLS certificates for the mock data holder to be signed by the Mock CDR CA.
- Extra steps detailed for using the solution in visual studio, docker containers and docker compose file.
- Removed GitHub Dependabot config.
- Ignore unsupported scopes in preparation for the introduction of energy scopes from an ADR.
- Removed references to the MD5 algorithm and replaced with SHA512.
	- This effects the output of the ID Permanence algorithm moving forward.
- Now complies with Phase 1 of the FAPI 1.0 advanced profile migration. Contains configuration to switch over to Phase 2 requirements.
- Regenerated all certificates to allow for another year before they expire.

### Fixed
- Added missing x-v header. [Issue 24](https://github.com/ConsumerDataRight/mock-data-holder/issues/24)
- Added object to Get Common Customer 400 error. [Issue 26](https://github.com/ConsumerDataRight/mock-data-holder/issues/26)

## [0.2.0] - 2021-11-24
### Added
- GitHub Actions Workflow for Build, Unit Test, and Integration Test project. 
- GitHub Issue config with supporting links to CDR related information. 
- GitHub Dependabot config. 

### Changed
- Instructions for certificates needed in Postman when using the Mock Data Holder Postman collection. 
- Minor changes to pipeline appsettings files to support GitHub Actions.
- Updates to GetMetrics and FAPI notes in ReadMe
- Minor changes to docker command in the ReadMe. [Issue 25](https://github.com/ConsumerDataRight/mock-data-holder/issues/25)
- Completed FAPI 0.6 testing.
- Updates to Refresh Token Request. [Issue 21](https://github.com/ConsumerDataRight/mock-data-holder/issues/21)

## [0.1.0] - 2021-10-01

### Added
- First release of the Mock Data Holder.
