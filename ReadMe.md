# Confluent Schema Registry with Protobuf

Intro to the Confluent Schema Registry with Protobuf

> **Prerequisites**:
> - [.NET Core SDK](https://dotnet.microsoft.com/download)
> - [Docker Desktop](https://docs.docker.com/desktop/)

> **References**:
> - [Confluent: Schema Management](https://docs.confluent.io/current/schema-registry/index.html)
> - [Confluent: Schema Formats, Serializers, and Deserializers](https://docs.confluent.io/current/schema-registry/serdes-develop/index.html#serializer-and-formatter)
> - [Protobuf C# Generated Code Guide](https://developers.google.com/protocol-buffers/docs/reference/csharp-generated)
> - [Protocol Buffers/gRPC Integration Into .NET Build](https://github.com/grpc/grpc/blob/master/src/csharp/BUILD-INTEGRATION.md)

The purpose of this intro is to demonstrate how a Producer can register schemas with an instance of the [Confluent Schema Registry](https://docs.confluent.io/current/schema-registry/index.html) running locally in a Docker container, together with the Kafka message broker. Schemas are registered using classes that are compiled from proto files using **Grpc.Tools** when the Consumer and Producer projects are built. Generated classes have the `.cs` suffix and can be found in the **obj/Debug** folder of the project.

Producer and Consumer both reference a common library containing proto files. This library can be deployed as a NuGet package for use in deployment pipelines.

## Kafka

> **Note**: Configure Docker to use 8 GB of memory.

1. Open a terminal and navigate to the **Kafka** directory.
   - Switch to the **Kafka** directory to start Kafka using Docker.
    ```bash
    cd Kafka
    docker-compose up --build -d
    ```
2. Open the control center at http://localhost:9021/
   - Wait until **controlcenter.cluster** is in a running state.

## Proto Library

1. Create a new .NET Standard library.
    ```
    dotnet new classlib -n ProtoLibrary
    ```
2. Create a **greet.v1.proto** file in **Protos** folder.

    ```protobuf
    syntax = "proto3";

    option csharp_namespace = "Protos.v1";

    // The response message containing the greetings.
    message HelloReply {
      string message = 1;
    }
    ```

## Producer

1. Create a new .NET Core console app
    ```bash
    dotnet new console -n Producer
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
      <Protobuf Include="../ProtoLibrary/Protos/**/*.proto" />
    </ItemGroup>
    ```
4. Build the **Producer** project.
   > **Note**: If the build fails the **Greet.cs** file will not be generated. Comment out references to `HelloReply` in order to build, then undo comments once the build has succeeded.
   - In **obj/Debug/netcoreapp3.1** you will find **Greet.cs**.
5. Add NuGet packages needed for Configuration, Kafka and Protobuf.
    ```bash
    dotnet add package Confluent.Kafka
    dotnet add package Confluent.SchemaRegistry.Serdes.Protobuf
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Binder
    dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```
6. Copy [Program.cs](https://github.com/event-streams-dotnet/event-stream-processing/blob/master/samples/EventStreamProcessing.Sample.Producer/Program.cs) from event-stream-processing sample producer.
   - Copy **ProducerOptions.cs**, **appsettings.json**.
   - Change namespace in `ProducerOptions` to `Producer`.
   - Set `RawTopic` to `hello-reply` in appsettings.json.
   - In appsettings.json add `SchemaRegistryUrl` set to `http://localhost:8081`.
   - Add `SchemaRegistryUrl` string propert tu ProducerOptions.cs.
   - Add `string schemaRegistryUrl` parameter to `Run_Producer` method, and pass in `producerOptions.SchemaRegistryUrl` to the line calling it in `Program.Main`.
   - In `Run_Producer` configure the producer to use the schema registry.
     - Create a new `SchemaRegistryConfig` setting `Url` to `schemaRegistryUrl`.
        ```csharp
        var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };
        ```
     - Create a new `SchemaRegistry` in a `using` block on top of the first `using` block.
        ```csharp
        using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
        ```
   - When creating a `ProducerBuilder<int, HelloReply>` set the value deserializer.
    ```csharp
    .SetValueDeserializer(new ProtobufDeserializer<HelloReply>().AsSyncOverAsync())
    ```
   - Set `val` to new `HelloReply` with `text`.
        ```csharp
        var val = new HelloReply
        {
            Message = text
        };
        ```
   - Change `Message` and `ProduceException` type arguments to `<int, HelloReply>`.
7. Test the producer.
   - Make sure Kafka is running in Docker.
    ```bash
    cd Kafka
    docker-compose ps
    ```
   - In a new terminal start the producer app.
    ```bash
    cd Producer
    dotnet run
    ```
    - Enter: `1 Hello World`
8. View registered schemas.
    - First get list of versions.
    ```bash
    curl -X GET http://localhost:8081/subjects/hello-reply-value/versions
    ```
    - Then get the schema for version 1.
    ```bash
    curl -X GET http://localhost:8081/subjects/hello-reply-value/versions
    curl -X GET http://localhost:8081/subjects/hello-reply-value/versions/1 | json_pp
    ```
   - You should see the following output.
    ```bash
    {
       "schemaType" : "PROTOBUF",
       "id" : 1,
       "schema" : "syntax = \"proto3\";\npackage greet;\n\nmessage HelloReply {\n  string message = 1;\n}\n",
       "version" : 1,
       "subject" : "hello-reply-value"
    }
    ```

## Consumer

1. Create a new .NET Core console app
    ```bash
    dotnet new console -n Consumer
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
      <Protobuf Include="../ProtoLibrary/Protos/**/*.proto" />
    </ItemGroup>
    ```
4. Build the **Consumer** project.
   - For VS Code, first generate assets for build and debug.
   - In **obj/Debug/netcoreapp3.1** you will find **Greet.cs**
5. Add NuGet packages needed for Configuration, Kafka and Protobuf.
    ```bash
    dotnet add package Confluent.Kafka
    dotnet add package Confluent.SchemaRegistry.Serdes.Protobuf
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Binder
    dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```
6. Copy [Program.cs](https://github.com/event-streams-dotnet/event-stream-processing/blob/master/samples/EventStreamProcessing.Sample.Consumer/Program.cs) from event-stream-processing sample consumer.
   - Copy **ConsumerOptions.cs**, **appsettings.json**.
   - Change namespace in `ConsumerOptions` to `Consumer`.
   - Set `TopicsList` to `hello-reply` in appsettings.json.
   - When creating a `ConsumerBuilder<int, HelloReply>` set the value deserializer.
    ```csharp
    .SetValueDeserializer(new ProtobufDeserializer<HelloReply>().AsSyncOverAsync())
    ```
7. Test the consumer.
   - In a new terminal start the consumer app.
    ```bash
    cd Consumer
    dotnet run
    ```

## Schema Evolution

The purpose of the Confluent Schema Registry is to allow for schema evolution for a specific topic based on compatibility rules.
- Compatible schemas are automatically registered when a message is produced.
- Incompatible schemas are not allowed to be registered, because a serialization error is thrown when attempting to produce a message using a schema that breaks compatibilty with the prior schema.
- A new topic will need to be created to support a schema that is incompatible with a prior version.

> **Note**: The rules for Protobuf backward compatibility are derived from the Protobuf language specification.

> **Protobuf Compatibility Rules**:
> - Fields can be **added**. All fields in Protobuf are optional, by default. If you specify defaults, these will be used for backward compatibility.
> - Fields can be **removed**. A field number can be reused by a new field of the same type. A field number cannot be reused by a new field of a different type.
> - Types int32, uint32, int64, uint64 and bool types are compatible (can be swapped in the same field).
> - Types sint32 and sint64 are compatible (can be swapped in the same field).
> - Types string and bytes are compatible (can be swapped in the same field).
> - Types fixed32 and sfixed32 are compatible (can be swapped in the same field).
> - Types fixed64 and sfixed64 are compatible (can be swapped in the same field).
> - Type enum is compatible with int32, uint32, int64, and uint64 (can be swapped in the same field).
> - Changing a single value into a member of a new oneof is compatible.

1. Create a new version of greet proto in **ProtoLibrary**.
   - Copy **greet.v1.proto** to **Protos** folder, rename to greet.v2.proto.
   - Change `csharp_namespace` to `Protos.v2`.
   - Import `google/protobuf/wrappers.proto`.
   - Add `temperatureF` field.
     - When compiled this will generate a nullable int.

    ```protobuf
    syntax = "proto3";

    option csharp_namespace = "Protos.v2";

    import "google/protobuf/wrappers.proto";

    // The response message containing the greetings.
    message HelloReply {
      string message = 1;
      google.protobuf.Int32Value temperatureF = 2;
    }
    ```
2. Because we are only adding a field to the proto file, consumer will be able to process both versions.
   - Run the Producer.
     - Enter schema versions 1 and 2 when prompted.
   - Run the Consumer, specifying schema version 1.
     - Notice that the `temperatureF` field in the second message is omitted.
   - Run the Consumer again, specifying schema version 2.
     - Notice this time that `temperatureF` is included in the second message.
3. Add **greet.v3.proto** to **ProtoLibrary**.
    ```protobuf
    syntax = "proto3";

    option csharp_namespace = "Protos.v3";

    import "google/protobuf/wrappers.proto";
    import "google/protobuf/timestamp.proto";

    // The response message containing the greetings.
    message HelloReply {
      string message = 1;
      google.protobuf.Int32Value temperatureF = 2;
      google.protobuf.Timestamp dateTimeStamp = 3;
    }
    ```
   - Run the Producer.
     - Enter schema versions 1, 2 and 3 when prompted.
   - Run the Consumer, specifying schema version 1.
   - Run the Consumer again, specifying schema version 2.
   - Run the Consumer again, specifying schema version 3.
      - Notice that new fields are included with newer versions.
4. Add **greet.v4.proto** to **ProtoLibrary**.
    ```protobuf
    syntax = "proto3";

    option csharp_namespace = "Protos.v4";

    import "google/protobuf/timestamp.proto";

    // The response message containing the greetings.
    message HelloReply {
      string message = 1;
      // Keep same field number as v3
      google.protobuf.Timestamp dateTimeStamp = 3;
    }
    ```
   - Run the Producer.
     - Enter schema versions 1, 2, 3 and 4 when prompted.
   - Run the Consumer, specifying schema version 1.
   - Run the Consumer again, specifying schema version 2.
   - Run the Consumer again, specifying schema version 3.
   - Run the Consumer again, specifying schema version 4.
      - Notice that dropped fields are omitted with older versions.
5. Add **greet.v5.proto** to **ProtoLibrary**.
    ```protobuf
    syntax = "proto3";

    option csharp_namespace = "Protos.v5";

    // The response message containing the greetings.
    message HelloReply {
      string message = 1;
      string dateTimeStamp = 3; // Change from Timestamp to string
    }
    ```
     - Change `dateTimeStamp` from `Timestamp` to `string`.
     - Run the Producer, specifying version 5.
       - Run the Consumer with version 5.
       - Notice that the `dateTimeStamp` is set and read properly.
     - Run the Producer, specifying versions 4 then 5.
       - Notice the **serialization error** due to the schema change.