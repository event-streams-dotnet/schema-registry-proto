# Schema Registry with Protobuf

Intro to the Confluent Schema Registry with Protobuf

> **References**:
> - [Protocol Buffers/gRPC Integration Into .NET Build](https://github.com/grpc/grpc/blob/master/src/csharp/BUILD-INTEGRATION.md)

## Consumer

1. Create a new .NET Core console app
    ```bash
    dotnet new console -n Consumer.v1
    ```
2. Add **Google.Protobuf**, **Grpc.Core**, **Grpc.Tools** packages.
    ```bash
    dotnet add package Google.Protobuf
    dotnet add package Grpc.Core
    dotnet add package Grpc.Tools
    ```
3. Update **.csproj** to include **.proto** files.
    ```xml
    <ItemGroup>
      <Protobuf Include="**/*.proto" />
    </ItemGroup>
    ```
4. Add **greet.proto** file to **Protos** folder.
    ```protobuf
    syntax = "proto3";

    option csharp_namespace = "Consumer.v1";

    package greet;

    // The response message containing the greetings.
    message HelloReply {
        string message = 1;
    }
    ```
5. Build the **Consumer.v1** project.
   - For VS Code, first generate assets for build and debug.
   - In **obj/Debug/netcoreapp3.1** you will find **Greet.cs**
6. Next