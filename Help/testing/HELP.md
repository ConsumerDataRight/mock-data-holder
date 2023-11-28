# Test Automation Execution Guide

## Table of Contents
- [Introduction](#introduction)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Build or Pull a Mock Register Image](#build-or-pull-a-mock-register-image)
    - [Build a Mock Register Image](#build-a-mock-register-image)
    - [Pull and Tag the Latest Image from Docker Hub](#pull-and-tag-the-latest-image-from-docker-hub)
  - [Build a Mock Data Holder Image](#build-a-mock-data-holder-image)
- [Test Execution](#test-execution)
  - [Running Tests Using the Integration Tests Docker Container](#running-tests-using-the-integration-tests-docker-container)
  - [Running Tests Using Microsoft Visual Studio](#running-tests-using-microsoft-visual-studio)
    - [Setup Local Machine Environment](#setup-local-machine-environment)
    - [Setup Multi-Container Docker Environment](#setup-multi-container-docker-environment)
    - [Run Tests using Microsoft Visual Studio Test Explorer](#run-tests-using-microsoft-visual-studio-test-explorer)
    - [Debugging Tests](#debugging-tests)

# Introduction

This guide provides the necessary information and instructions on setting up an environment to allow for running Mock Data Holder integration tests using both Microsoft Visual Studio's Test Explorer and Docker. It also provides different options for setting up your environment and running tests to cater for different use cases.

# Prerequisites  

[Docker Desktop](https://www.docker.com/products/docker-desktop/) is installed and running.

[Microsoft Visual Studio](https://visualstudio.microsoft.com/) is installed.


# Getting Started

Before being able to execute any Mock Data Holder automated tests, the following mock solution docker images are required:

- mock-register
- mock-data-holder
- mock-data-holder-energy

This guide explains how these docker images can be either built from scratch using GitHub repositories, or pulled directly from Docker Hub.

## Build or Pull a Mock Register Image
The Mock Register image can be either built from the GitHub [Mock Register](https://github.com/ConsumerDataRight/mock-register) repository, or pulled directly from [Docker Hub](https://hub.docker.com/r/consumerdataright/mock-register). This guide describes both options and their respective use cases.

### Build a Mock Register Image
Building your own Mock Register image may be useful if you want to make changes to any source code in the Mock Register solution. Follow the steps below to build a Mock Register image from scratch:

1. Clone the Mock Register repository using the following command.
```
git clone https://github.com/ConsumerDataRight/mock-register.git
```
2. Run the following command to build the Mock Register docker image from the `mock-register\Source` folder:
```
docker build -f Dockerfile -t mock-register .
```   
The Mock Register docker image should now be available for use in Docker. For further and more detailed documentation regarding the Mock Register, refer to the [Mock Register](https://github.com/ConsumerDataRight/mock-register) GitHub repository.

### Pull and Tag the Latest Image from Docker Hub
Pulling the latest Mock Register image from Docker Hub is a quicker and easier alternative to building your own Mock Register image from scratch. It is recommended for most cases where customisation of the Mock Register code base is not required.

This can be done by simply executing the following docker commands:
```
docker pull consumerdataright/mock-register

docker image tag consumerdataright/mock-register mock-register
```
The Mock Register image should now be available for use in Docker.

## Build a Mock Data Holder Image
By default, building a new docker image for a [Mock Data Holder](https://github.com/ConsumerDataRight/mock-data-holder) is performed by executing any of the integration test compose files as detailed in the Test Execution section below. The following steps are required before any of these compose files can be executed:

1. Clone the Mock Data Holder repository using the following command:
```
git clone https://github.com/ConsumerDataRight/mock-data-holder.git
```

2. The Mock Data Holder image requires the [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) as a prerequisite before it can be successfully built. The [Authorisation Server](https://github.com/ConsumerDataRight/authorisation-server) repository can be cloned using following command:
```
git clone https://github.com/ConsumerDataRight/authorisation-server.git ./cdr-auth-server
```
3. A PowerShell script is available in the mock-data-holder/Source folder that will copy the required Authorisation Server folders required to build a Mock Data Holder image. This can be executed in PowerShell with the command below.
```
.\copy-cdr-auth-server.ps1
```

The Mock Data Holder image is now ready to be built.

# Test Execution
Automated tests can be executed by either using a docker container or by running them directly from Microsoft Visual Studio's Test Explorer. This guide describes both options and their respective use case.
## Running Tests Using the Integration Tests Docker Container
Running tests using a docker container is useful when debugging or stepping through the test's code is not required.

The [Mock Data Holder Banking Integration Tests Compose File](../../Source/DockerCompose/docker-compose.IntegrationTests.Banking.yml) can be executed using the docker compose command to run the tests within a docker container Banking industry:

```
docker compose -f "docker-compose.IntegrationTests.Banking.yml" up -d --build
```
This docker compose command will start the necessary docker containers and automatically run the Mock Data Holder Banking Integration Tests. The following screenshot shows an example of the Banking Mock Data Holder Integration Tests being run:

[<img src="./images/Docker-Compose-Mock-Data-Holder-Banking-Running-Tests.png" width='800' alt="Banking Integration tests running"/>](./images/Docker-Compose-Mock-Data-Holder-Banking-Running-Tests.png)

Similarly, the [Mock Data Holder Energy Integration Tests Compose File](../../Source/DockerCompose/docker-compose.IntegrationTests.Energy.yml) can be executed using the docker compose command to run the tests within a docker container for the Energy industry:

```
docker compose -f "docker-compose.IntegrationTests.Energy.yml" up -d --build
```

Following the execution of the integration tests, a folder named '_temp' will be generated in the 'mock-data-holder/Source/DockerCompose' folder. This will contain test results in TRX format and any other artifacts created by the test automation execution. The TRX test results file can be opened and viewed in Microsoft Visual Studio as per example screenshot below:

[<img src="./images/MS-Visual-Studio-View-Test-Results.png" width='800' alt="Viewing results in Microsoft Visual Studio"/>](./images/MS-Visual-Studio-View-Test-Results.png)


## Running Tests Using Microsoft Visual Studio
Running tests using Microsoft Visual Studio is required when wanting to debug or step through the test's source code.

### Setup Local Machine Environment

The following host names must be registered in the local machine's `hosts` file (located in C:\Windows\System32\drivers\etc).

```
127.0.0.1   mock-register
127.0.0.1   mock-data-holder
127.0.0.1   mock-data-holder-energy
127.0.0.1   mock-data-holder-integration-tests
127.0.0.1   mock-data-holder-energy-integration-tests
127.0.0.1   mssql
```

A Windows Environment variable for `ASPNETCORE_ENVIRONMENT` is required to be added and set to `Release`.

The [Mock CDR CA Certificate](../../CertificateManagement/mtls/ca.pfx) is required to be installed in the local machine's Trusted Root Certification Authorities store. 
Consult the [Certificate Management](https://github.com/ConsumerDataRight/mock-register/blob/main/CertificateManagement/README.md) documentation for more information about how certificates are used in CDR Mock Solutions.

### Setup Multi-Container Docker Environment
Before being able to execute tests using Microsoft Visual Studio, the Mock Data Holder, Mock Register and Microsoft SQL Server docker containers need to be running.
The following docker compose commands will run these containers for a Banking or Energy Mock Data Holder.

To run a multi-container environment for a Banking Mock Data Holder, execute the following docker compose command from the `mock-data-holder\Source\DockerCompose` folder:

```
docker compose -f docker-compose.IntegrationTests.Banking.yml up -d --build mssql mock-register mock-data-holder 
```
To run a multi-container environment for an Energy Mock Data Holder, execute the following docker compose command from the `mock-data-holder\Source\DockerCompose` folder:
```
docker compose -f docker-compose.IntegrationTests.Energy.yml up -d --build mssql mock-register mock-data-holder
```

The following screenshot shows an example of the Banking Mock Data Holder, Mock Register and Microsoft SQL Server docker containers running:

[<img src="./images/Docker-Compose-Mock-Data-Holder-Banking.png" width='800' alt="Mock Data Holder Multi-Container Docker Environment"/>](./images/Docker-Compose-Mock-Data-Holder-Banking.png)

Tests can now be run using Microsoft Visual Studio.

### Run Tests using Microsoft Visual Studio Test Explorer

The following steps detail the process of running tests using Microsoft Visual Studio's Test Explorer:

1. Open the [DataHolder.sln](../../Source/DataHolder.sln) solution file in Microsoft Visual Studio.
2. Build the solution.
3. Open the Test Explorer. If Test Explorer is not visible, choose 'Test' on the Visual Studio menu and then choose 'Test Explorer'.
   
   [<img src="./images/MS-Visual-Studio-Test-Explorer.png" width='400' alt="Microsoft Visual Studio Test Explorer"/>](./images/MS-Visual-Studio-Test-Explorer.png)
4. Right click the test, or group of tests to execute and select 'Run' as per screenshot below:
   
   [<img src="./images/MS-Visual-Studio-Test-Explorer-Run.png" width='400' alt="Run tests in Microsoft Visual Studio"/>](./images/MS-Visual-Studio-Test-Explorer-Run.png)

   The screenshot below is an example of successfully completed Banking integration tests:

      [<img src="./images/MS-Visual-Studio-Test-Explorer-Execution-Completed.png" width='800' alt="Microsoft Visual Studio Test Explorer Completed Tests"/>](./images/MS-Visual-Studio-Test-Explorer-Execution-Completed.png)

### Debugging Tests

The Test Automation projects use the [ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation](https://www.nuget.org/packages/ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation) NuGet package. The source code for this repository is available in the [Mock Solution Test Automation](https://github.com/ConsumerDataRight/mock-solution-test-automation) repository. Cloning this repository to your local machine will allow you to easy debug, step through or ever modify any code that was used to build the NuGet package.

This repository can be cloned using following command:
```
git clone https://github.com/ConsumerDataRight/mock-solution-test-automation.git
```

The [DataHolder_Shared.sln](../../Source/DataHolder_Shared.sln) solution has been created to allow for debugging and stepping through the source code used in Mock Solution Test Automation project. 

   [<img src="./images/MS-Visual-Studio-View-Data-Holder-Shared-Solution.png" width='400' alt="Data Holder Shared Solution in Microsoft Visual Studio"/>](./images/MS-Visual-Studio-View-Data-Holder-Shared-Solution.png)

Select the `Shared` solution configuration in Visual Studio to switch from using the Mock Solution Test Automation NuGet package to the using the `ConsumerDataRight.ParticipantTooling.MockSolution.TestAutomation` project instead:

   [<img src="./images/MS-Visual-Studio-View-Data-Holder-Shared-Configuration.png" alt="Shared Configuration"/>](./images/MS-Visual-Studio-View-Data-Holder-Shared-Configuration.png)


This will allow for debugging, stepping through and modifying the source code that is used to create the NuGet package. Right click the test, or group of tests you'd like to debug and select 'Debug' to begin debugging tests.

   [<img src="./images/MS-Visual-Studio-Test-Explorer-Debug.png" width='400' alt="Debug test in Microsoft Visual Studio"/>](./images/MS-Visual-Studio-Test-Explorer-Debug.png)

For more information on using Microsoft Test Explorer, search for 'Test Explorer' at [Microsoft Learn](https://learn.microsoft.com/).