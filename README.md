![Consumer Data Right Logo](./Assets/cdr-logo.png?raw=true) 

[![Consumer Data Standards v1.22.0](https://img.shields.io/badge/Consumer%20Data%20Standards-v1.22.0-blue.svg)](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#introduction)
[![Conformance Test Suite 4.3.1](https://img.shields.io/badge/Conformance%20Test%20Suite-v4.3.1-darkblue.svg)](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-holders)
[![FAPI 1.0 Advanced Profile](https://img.shields.io/badge/FAPI%201.0-orange.svg)](https://openid.net/specs/openid-financial-api-part-2-1_0.html)
[![made-with-dotnet](https://img.shields.io/badge/Made%20with-.NET-1f425Ff.svg)](https://dotnet.microsoft.com/)
[![made-with-csharp](https://img.shields.io/badge/Made%20with-C%23-1f425Ff.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/github/license/ConsumerDataRight/mock-data-holder)](./LICENSE)
[![Pull Requests Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](./CONTRIBUTING.md)

# Consumer Data Right - Mock Data Holder
This project includes source code, documentation and instructions for a Consumer Data Right (CDR) Mock Data Holder.

This repository contains a mock implementation of a Data Holder and is offered to help the community in the development and testing of their CDR solutions.

The Mock Data Holder solution can be configured for Banking or Energy industries.

## Mock Data Holder - Alignment
The Mock Data Holder:
* aligns to [v1.22.0](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#introduction) of the [Consumer Data Standards](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#introduction) in particular [FAPI 1.0 Migration Phase 3](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#introduction) with backwards compatbility to Migration Phase 2;
* has passed v4.3.1 of the [Conformance Test Suite for Data Holders](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-holders); and
* is compliant with the [FAPI 1.0 Advanced Profile](https://openid.net/specs/openid-financial-api-part-2-1_0.html).

## Getting Started
The Mock Data Holder uses the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server), the [Mock Register](https://github.com/ConsumerDataRight/mock-register) and the [Mock Data Recipient](https://github.com/ConsumerDataRight/mock-data-recipient). You can swap out any of the Mock Data Holder, Mock Register and Mock Data Recipient solutions with a solution of your own.

There are a number of ways that the artefacts within this project can be used:
1. Build and deploy the source code
2. Use the pre-built image
3. Use the docker compose file to run a multi-container mock CDR Ecosystem

### Build and deploy the source code

To get started, clone the source code.
```
git clone https://github.com/ConsumerDataRight/mock-data-holder.git
```

Starting from version 1.2.0, the Mock Data Holder now utilises the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) as an Identity Provider. The [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) also needs to be running when running the Mock Data Holder. The [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) repository can be cloned using following command.
```
git clone https://github.com/ConsumerDataRight/authorisation-server.git ./cdr-auth-server
```
To get help on setting the industry profile, launching and debugging the solution, see the [help guide](./Help/debugging/HELP.md).

If you would like to contribute features or fixes back to the Mock Data Holder repository, consult the [contributing guidelines](./CONTRIBUTING.md).

### Use the pre-built Banking or Energy image

Docker images are available in [Docker Hub](https://hub.docker.com/r/consumerdataright) for the [Banking](https://hub.docker.com/r/consumerdataright/mock-data-holder) and [Energy](https://hub.docker.com/r/consumerdataright/mock-data-holder-energy) Mock Data Holders.

**Note: Starting from version 1.2.0, the Identity Server has been replaced with the  [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server). Although the  [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) exists as a separate repository, when the mock-data-holder banking or energy image is built for Docker, the Authorization Server is copied into the image, replacing Identity Server 4.**

#### Pull the latest Banking or Energy image from Docker

Run the following command to pull the latest Banking Mock Data Holder image from Docker Hub:
```
docker pull consumerdataright/mock-data-holder:latest
```

Run the following command to pull the latest Energy Mock Data Holder image from Docker Hub:
```
docker pull consumerdataright/mock-data-holder-energy:latest
```

To get help on setting the industry as Banking or Energy, launching and debugging the solutions as containers and switching out your solution(s), see the [help guide](./Help/container/HELP.md).

#### Try it out

Once the Mock Data Holder container is running, you can use the provided [Mock Data Holder Postman API collection](./Postman/README.md) to try it out.

#### Certificate Management

Consult the [Certificate Management](./CertificateManagement/README.md) documentation for more information about how certificates are used for the Mock Data Holder.

#### Load your own Banking or Energy seed data

When the Mock Data Holder container first starts it will load data from the included `seed-data-{industry}.json` file that is included in the `CDR.DataHolder.Manage.API` project. Running the Mock Data Holder using the Banking profile will load data from the `seed-data-banking.json` file. 
Running the Mock Data Holder using the Energy profile will load data from the `seed-data-energy.json` file. The files include dummy banking and energy data (customers, accounts, banking transactions, energy concessions) as well as data recipient metadata that can be obtained from the Register.  When calls are made to the Resource API the dummy banking or energy data is returned. 

There are a couple of ways to load your own data into the container instance:
1. Provide your own `seed-data.json` file within the Mock Data Holder container
  - Within the `/app/manage/Data` folder of the container, make a copy of the `seed-data-{industry}.json` file, renaming to a name of your choice, e.g. `my-seed-data.json`.
  - Update your seed data file with your desired metadata.
  - Change the `/app/manage/appsettings.json` file to load the new data file and overwrite the existing data:

```
"SeedData": {
    "FilePath": "Data/my-seed-data.json",
    "OverwriteExistingData": true
},
```

  - Restart the container.

2. Use the Manage API endpoint to load data

The Mock Data Holder includes a Manage API that allows metadata to be downloaded and uploaded from the repository.  

To retrieve the current metadata held within the repository make the following request to the Manage API:

```
Mock Data Holder Banking
GET https://localhost:8005/manage/metadata
```

```
Mock Data Holder Energy
GET https://localhost:8105/manage/metadata
```

The response will be metadata in JSON format that conforms to the same structure of the `seed-data-{industry}.json` file.  This payload structure is also the same structure that is used to load new metadata via the Manage API.

To re-load the repository with metadata make the following request to the Manage API:

**Note: calling this API will delete all existing metadata and re-load with the provided metadata** 

```
Mock Data Holder Banking
POST https://localhost:8005/manage/metadata

{
    <JSON metadata - as per the GET /manage/metadata response or seed-data.json file>
}
```

```
Mock Data Holder Energy
POST https://localhost:8105/manage/metadata

{
    <JSON metadata - as per the GET /manage/metadata response or seed-data.json file>
}
```

**Note:** there is currently no authentication/authorisation applied to the Manage API endpoints as these are seen to be under the control of the container owner.  This can be added if there is community feedback to that effect or if a pull request is submitted.

### Use the docker compose file to run a multi-container mock CDR Ecosystem

The [docker compose file](./Source/DockerCompose/docker-compose.yml) can be used to run multiple containers from the Mock CDR Ecosystem.

**Note:** the [docker compose file](./Source/DockerCompose/docker-compose.yml) utilises the Microsoft SQL Server Image from Docker Hub. The Microsoft EULA for the Microsoft SQL Server Image must be accepted to use the [docker compose file](./Source/DockerCompose/docker-compose.yml). See the Microsoft SQL Server Image on Docker Hub for more information.

To get help on launching and debugging the solutions as containers and switching out your solution(s), see the [help guide](./Help/container/HELP.md).

## Mock Data Holder - Requirements
Data Holders require these core functions defined in the [Consumer Data Standards](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#introduction) to operate in the [Consumer Data Right](https://www.cdr.gov.au/):
- Identity Provider for [authentication and authorisation](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#security-profile). Compliant with OAuth and [FAPI 1.0 Advanced Profile](https://openid.net/specs/openid-financial-api-part-2-1_0.html).
- [Dynamic Client Registration](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#dcr-apis) to allow clients to register their Software Products.
- Data Recipient and Software Product metadata updates using the [Register APIs](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#register-apis).
- Industry specific data (one of)
  - [Banking APIs](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#banking-apis).
  - [Energy APIs](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#energy-apis)
- Industry agnostic data
  - [Common APIs](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#common-apis).
- [Metrics and metadata update requests](https://consumerdatastandardsaustralia.github.io/standards-archives/standards-1.22.0/#admin-apis).

The Mock Data Holder combined with the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) provides all of these functions for the Banking and Energy industries. The below diagram provides a view of the Mock Data Holder and and Authorisation Server when opened in an integrated development environment.

[<img src="./Assets/mock-data-holder-components.png?raw=true" height='600' width='500' alt="Mock Data Holder Components"/>](./Assets/mock-data-holder-components.png?raw=true)

Switching between Banking and Energy is achieved by starting the projects needed for the given industry and using industry specific data and ports. The below diagrams diplay which projects are started depending on the industry profile. The diagrams also illustrate which of the Data Holder functions are shared across industries.

[<img src="./Assets/mock-data-holder-banking-profile.png?raw=true" height='600' width='500' alt="Mock Data Holder Banking Profile"/>](./Assets/mock-data-holder-banking-profile.png?raw=true)
[<img src="./Assets/mock-data-holder-energy-profile.png?raw=true" height='600' width='500' alt="Mock Data Holder Energy Profile"/>](./Assets/mock-data-holder-energy-profile.png?raw=true)

## Mock Data Holder - Architecture
The following sections outline the high level architecture and components of the Mock Data Holder:

### Mock Data Holder with Banking Profile
[<img src="./Assets/mock-data-holder-banking-architecture.png?raw=true" height='600' width='800' alt="Mock Data Holder Banking - Architecture"/>](./Assets/mock-data-holder-banking-architecture.png?raw=true)

### Mock Data Holder with Banking Profile - Components
The Mock Data Holder contains the following components:

- Public API
  - Hosted at `https://localhost:8000`
  - Contains the public discovery APIs - `Get Status` and `Get Outages`. 
  - Accessed directly on `port 8000`.
- Identity Provider
  - Hosted at `https://localhost:8001`
  - Mock Data Holder Identity Provider implementation utilising the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) hosted as separate repository.
  - Accessed directly (TLS only) as well as the mTLS Gateway, depending on the target endpoint.
- mTLS Gateway
  - Hosted at `https://localhost:8002`
  - Provides the base URL endpoint for mTLS communications, including Infosec, Resource and Admin APIs.
  - Performs certificate validation.
- Resource API
  - `Get Accounts` and `Get Transactions` endpoints hosted at `https://localhost:8003`.
  - `Get Customer` endpoint hosted at `https://localhost:8006`.
  - Accessed via the mTLS Gateway.
- Manage API
  - Hosted at `https://localhost:8005`
  - Not part of the Consumer Data Standards, but allows for the maintenance of data in the Mock Data Holder repository.
  - Also includes trigger points to refresh the Data Recipient, Data Recipient Status and Software Product Status from the Mock Register.
  - A user interface may be added at some time in the future to provide user friendly access to the repository data.
- Repository
  - A SQL database containing Mock Data Holder data.

  
### Mock Data Holder with Energy Profile
[<img src="./Assets/mock-data-holder-energy-architecture.png?raw=true" height='600' width='800' alt="Mock Data Holder Energy - Architecture"/>](./Assets/mock-data-holder-energy-architecture.png?raw=true)

### Mock Data Holder with Energy Profile - Components
The Mock Data Holder contains the following components:

- Public API
  - Hosted at `https://localhost:8100`
  - Contains the public discovery APIs - `Get Status` and `Get Outages`. 
  - Accessed directly on `port 8100`.
- Identity Provider
  - Hosted at `https://localhost:8101`  
  - Mock Data Holder Identity Provider implementation utilising the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) hosted as separate repository.
  - Accessed directly (TLS only) as well as the mTLS Gateway, depending on the target endpoint.
- mTLS Gateway
  - Hosted at `https://localhost:8102`
  - Provides the base URL endpoint for mTLS communications, including Infosec, Resource and Admin APIs.
  - Performs certificate validation.
- Resource API
  - `Get Accounts` and `Get Concessions` endpoints hosted at `https://localhost:8103`.
  - `Get Customer` endpoint hosted at `https://localhost:8106`.
  - Accessed via the mTLS Gateway.
- Manage API
  - Hosted at `https://localhost:8105`
  - Not part of the Consumer Data Standards, but allows for the maintenance of data in the Mock Data Holder repository.
  - Also includes trigger points to refresh the Data Recipient, Data Recipient Status and Software Product Status from the Mock Register.
  - A user interface may be added at some time in the future to provide user friendly access to the repository data.
- Repository
  - A SQL database containing Mock Data Holder data.

## Technology Stack

The following technologies have been used to build the Mock Data Holder:
- The source code has been written in `C#` using the `.Net 6` framework.
- The Identity Provider is implemented using the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server).
- The mTLS Gateway has been implemented using `Ocelot`.
- The Repository utilises a `SQL` instance.
- `xUnit` is the framework used for writing and running tests.
- `Microsoft Playwright` is the framework used for Web Testing.

# Testing

A collection of API requests has been made available in [Postman](https://www.postman.com/) in order to test the Mock Data Holder and view the expected interactions.  See the Mock Data Holder [Postman](./Postman/README.md) documentation for more information.

Automated integrated tests have been created as part of this solution. See the [Test Automation Execution Guide](./Help/testing/HELP.md) documentation for more information.

# Contribute
We encourage contributions from the community.  See our [contributing guidelines](./CONTRIBUTING.md).

# Code of Conduct
This project has adopted the **Contributor Covenant**.  For more information see the [code of conduct](./CODE_OF_CONDUCT.md).

# Security Policy
See our [security policy](./SECURITY.md) for information on security controls, reporting a vulnerability and supported versions.

# License
[MIT License](./LICENSE)

# Notes
The Mock Data Holder is provided as a development tool only. It conforms to the Consumer Data Standards.
