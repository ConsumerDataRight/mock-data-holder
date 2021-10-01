![Consumer Data Right Logo](https://raw.githubusercontent.com/ConsumerDataRight/mock-data-holder/main/cdr-logo.png) 

[![Consumer Data Standards 1.11.0](https://img.shields.io/badge/Consumer%20Data%20Standards-v1.11.0-blue.svg)](https://consumerdatastandardsaustralia.github.io/standards/includes/releasenotes/releasenotes.1.11.0.html#v1-11-0-release-notes)
[![Conformance Test Suite 3.2](https://img.shields.io/badge/Conformance%20Test%20Suite-v3.2-darkblue.svg)](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-holders)
[![made-with-dotnet](https://img.shields.io/badge/Made%20with-.NET-1f425Ff.svg)](https://dotnet.microsoft.com/)
[![made-with-csharp](https://img.shields.io/badge/Made%20with-C%23-1f425Ff.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![MIT License](https://img.shields.io/github/license/ConsumerDataRight/mock-data-holder)](./LICENSE)
[![Pull Requests Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](./CONTRIBUTING.md)

# Consumer Data Right - Mock Data Holder
This project includes source code, documentation and instructions for a Consumer Data Right (CDR) Mock Data Holder.

This repository contains a mock implementation of a Mock Data Holder and is offered to help the community in the development and testing of their CDR solutions.

## Mock Data Holder - Alignment
The Mock Data Holder aligns to [v1.11.0](https://consumerdatastandardsaustralia.github.io/standards/includes/releasenotes/releasenotes.1.11.0.html#v1-11-0-release-notes) of the [Consumer Data Standards](https://consumerdatastandardsaustralia.github.io/standards).
The Mock Data Holder passed v3.2 of the [Conformance Test Suite for Data Holders](https://www.cdr.gov.au/for-providers/conformance-test-suite-data-holders).

## Getting Started
The Mock Data Holder was built using the [Mock Register](https://github.com/ConsumerDataRight/mock-register) and the [Mock Data Recipient](https://github.com/ConsumerDataRight/mock-data-recipient). You can swap out any of the Mock Data Holder, Mock Data Register and Mock Data Recipient solutions with a solution of your own.

There are a number of ways that the artefacts within this project can be used:
1. Build and deploy the source code
2. Use the pre-built image
3. Use the docker compose file to run a multi-container mock CDR Ecosystem

### Build and deploy the source code

To get started, clone the source code.
```
git clone https://github.com/ConsumerDataRight/mock-data-holder.git
```

Use the [Start-DataHolder.bat file](Source/Start-DataHolder.bat) to build and start all of theprojects needed to run the Data Holder.  Conversely, update the start up projects for the solution to match the [Start-DataHolder.bat file](Source/Start-DataHolder.bat).

If you would like to contribute features or fixes back to the Mock Data Holder repository, consult the [contributing guidelines](CONTRIBUTING.md).

### Use the pre-built image

A version of the Mock Data Holder is built into a single Docker image that is made available via [docker hub](https://hub.docker.com/r/consumerdataright/mock-data-holder).

The container can simply be run by pulling and running the latest image using the following Docker commands:

#### Pull the latest image

```
docker pull consumerdataright/mock-data-holder
```

#### Run the Mock Data Holder container

```
docker run -d -h mock-cdr-data-holder -p 8000:8000 -p 8001:8001 -p 8002:8002 -p 8005:8005 --name mock-cdr-data-holder consumerdataright/mock-data-holder
```

#### Try it out

Once the Mock Data Holder container is running, you can use the provided [Mock Data Holder Postman API collection](Postman/README.md) to try it out.

#### Certificate Management

Consult the [Certificate Management](CertificateManagement/README.md) documentation for more information about how certificates are used for the Mock Data Holder.

#### Loading your own data

When the Mock Data Holder container first starts it will load data from the included `seed-data.json` file that is included in the `CDR.DataHolder.Manage.API` project.  This file includes dummy banking data (customers, accounts, transactions) as well as data recipient metadata that can be obtained from the Register.  When calls are made to the Resource API the dummy banking data is returned.  The data recipient metadata is used within the internal workings of the Mock Data Holder to check their status before responding to data sharing requests. 

There are a couple of ways to load your own data into the container instance:
1. Provide your own `seed-data.json` file within the Mock Data Holder container
  - Within the `/app/manage/Data` folder of the container, make a copy of the `seed-data.json` file, renaming to a name of your choice, e.g. `my-seed-data.json`.
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
GET https://localhost:8005/manage/metadata
```

The response will be metadata in JSON format that conforms to the same structure of the `seed-data.json` file.  This payload structure is also the same structure that is used to load new metadata via the Manage API.

To re-load the repository with metadata make the following request to the Manage API:

**Note: calling this API will delete all existing metadata and re-load with the provided metadata** 

```
POST https://localhost:8005/manage/metadata

{
    <JSON metadata - as per the GET /manage/metadata response or seed-data.json file>
}
```

**Note:** there is currently no authentication/authorisation applied to the Manage API endpoints as these are seen to be under the control of the container owner.  This can be added if there is community feedback to that effect or if a pull request is submitted.

### Use the docker compose file to run a multi-container mock CDR Ecosystem

The [docker compose file](Source/DockerCompose/docker-compose.yml) can be used to run multiple containers from the Mock CDR Ecosystem.
1. Add the following to your hosts file, eg C:\Windows\System32\drivers\etc\hosts
````
127.0.0.1 mock-data-holder
127.0.0.1 mock-data-recipient
127.0.0.1 mock-register
````
2. Flush the DNS cache, on Windows use: 
````
ipconfig /flushdns
````
3. Run the [docker compose file](Source/DockerCompose/docker-compose.yml)
````
docker-compose up
````

Update the docker compose file if you would like to swap out one of the mock solutions with a solution of your own.

## Mock Data Holder - Architecture
The following diagram outlines the high level architecture of the Mock Data Holder:

![Mock Data Holder - Architecture](https://raw.githubusercontent.com/ConsumerDataRight/mock-data-holder/main/mock-data-holder-architecture.png)

## Mock Data Holder - Components
The Mock Data Holder contains the following components:

- Public API
  - Hosted at `https://localhost:8000`
  - Contains the public discovery APIs - `Get Status` and `Get Outages`. 
  - Accessed directly on `port 8000`.
- Identity Provider
  - Hosted at `https://localhost:8001`
  - Mock Data Holder identity provider implementation utilising `Identity Server 4`
  - Accessed directly (TLS only) as well as the mTLS Gateway, depending on the target endpoint.
- mTLS Gateway
  - Hosted at `https://localhost:8002`
  - Provides the base URL endpoint for mTLS communications, including Infosec, Resource and Admin APIs.
  - Performs certificate validation.
- Resource API
  - Hosted at `https://localhost:8003`
  - Currently includes the `Get Customer`, `Get Accounts` and `Get Transactions` endpoints.
  - Accessed via the mTLS Gateway.
- Admin API
  - Hosted at `https://localhost:8004`
  - Contains the `Get Metrics` endpoint.  *To be implemented*
  - Accessed via the mTLS Gateway.
- Manage API
  - Hosted at `https://localhost:8005`
  - Not part of the Consumer Data Standards, but allows for the maintenance of data in the Mock Data Holder repository.
  - Also includes trigger points to refresh the Data Recipient, Data Recipient Status and Software Product Status from the Mock Register.
  - A user interface may be added at some time in the future to provide user friendly access to the repository data.
- Repository
  - A SQLite database containing Mock Data Holder data.

## Technology Stack

The following technologies have been used to build the Mock Data Holder:
- The source code has been written in `C#` using the `.NET 5` framework.
- The Identity Provider is implemented using `Identity Server 4`.
- The mTLS Gateway has been implemented using `Ocelot`.
- The Repository utilises a `SQLite` instance.

# Testing

A collection of API requests has been made available in [Postman](https://www.postman.com/) in order to test the Mock Data Holder and view the expected interactions.  See the Mock Data Holder [Postman](Postman/README.md) documentation for more information.

# Contribute
We encourage contributions from the community.  See our [contributing guidelines](CONTRIBUTING.md).

# Code of Conduct
This project has adopted the **Contributor Covenant**.  For more information see the [code of conduct](CODE_OF_CONDUCT.md).

# License
[MIT License](./LICENSE)

# Notes
The Mock Data Holder is provided as a development tool only.  It conforms to the Consumer Data Standards and Register Design.
