# Branta .NET SDK

Package contains functionality to assist .NET projects with making requests to Branta's server.

## Requirements

 * .NET 8.0 or higher

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Branta
```
Or via Package Manager Console:
```ps
Install-Package Branta
```

## Quick Start
```cs
// Register DI Services
using Branta.V2.Extensions;

services.ConfigureBrantaServices();

// OR with default options set

services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = "",
    DefaultApiKey = ""
});

// Use within your Services
using Branta.V2.Classes;

public class Example
{
    private readonly BrantaClient _brantaClient;

    public Example(BrantaClient brantaClient)
    {
        _brantaClient = brantaClient;
    }

    public async Task ExampleMethod() 
    {
        await _brantaClient.GetPaymentsAsync("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");
    }
}
```

## Release

 - Open .sln file in Visual Studio
 - Update version in `Branta/Branta.csproj`
 - Change Configuration from Debug to Release
 - Run Build
 - Package can be found at `Branta/bin/Release/Branta.X.X.X.nupkg`
 - Upload this file to the new release on nuget.org


## Feature Support

 - [X] Per Environment configuration
 - [X] V2 Get Payment by address
 - [ ] V2 Get Payment by QR Code
 - [X] V2 Get decrypted Zero Knowledge by address and secret
 - [X] V2 Add Payment
 - [ ] V2 Payment by Parent Platform with HMAC
 - [X] V2 Add Zero Knowledge Payment with secret
 - [X] V2 Check API key valid
