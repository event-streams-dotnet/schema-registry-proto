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
6. Add NuGet packages.
    ```bash
    dotnet add package Confluent.Kafka
    dotnet add package Confluent.SchemaRegistry.Serdes.Protobuf
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Binder
    dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```
7. Copy [Program.cs](https://github.com/event-streams-dotnet/event-stream-processing/blob/master/samples/EventStreamProcessing.Sample.Consumer/Program.cs) from event-stream-processing sample consumer.
   - Copy **ConsumerOptions.cs**, **appsettings.json**.
   - Change namespace in `ConsumerOptions` to `Consumer.v1`.
   - Set `TopicsList` to `hello-reply` in appsettings.json.
   - When creating a `ConsumerBuilder<int, HelloReply>` set the value deserializer.
    ```csharp
    .SetValueDeserializer(new ProtobufDeserializer<HelloReply>().AsSyncOverAsync())
    ```
8. Test the consumer.
   > **Note**: Configure Docker to use 8 GB of memory.
   - Switch to the **Kafka** directory to start Kafka using Docker.
    ```bash
    cd Kafka
    docker-compose up --build -d
    ```
   - Open the control center at http://localhost:9021/
   - Wait until **controlcenter.cluster** is in a running state.
   - In a new terminal start the consumer app.
    ```bash
    cd Consumer.v1
    dotnet run
    ```

## Producer

1. Create a new .NET Core console app
    ```bash
    dotnet new console -n Producer.v1
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

    option csharp_namespace = "Producer.v1";

    package greet;

    // The response message containing the greetings.
    message HelloReply {
        string message = 1;
    }
    ```
5. Build the **Producer.v1** project.
   - For VS Code, first generate assets for build and debug.
   - In **obj/Debug/netcoreapp3.1** you will find **Greet.cs**
6. Add NuGet packages.
    ```bash
    dotnet add package Confluent.Kafka
    dotnet add package Confluent.SchemaRegistry.Serdes.Protobuf
    dotnet add package Microsoft.Extensions.Configuration
    dotnet add package Microsoft.Extensions.Configuration.Binder
    dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables
    dotnet add package Microsoft.Extensions.Configuration.Json
    ```
7. Copy [Program.cs](https://github.com/event-streams-dotnet/event-stream-processing/blob/master/samples/EventStreamProcessing.Sample.Producer/Program.cs) from event-stream-processing sample producer.
   - Copy **ProducerOptions.cs**, **appsettings.json**.
   - Change namespace in `ProducerOptions` to `Producer.v1`.
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
8. Test the producer.
   > **Note**: Configure Docker to use 8 GB of memory.
   - Make sure Kafka is still running in Docker.
    ```bash
    cd Kafka
    docker-compose ps
    ```
   - Open the control center at http://localhost:9021/
   - Make sure **controlcenter.cluster** is in a running state.
   - In a new terminal start the producer app.
    ```bash
    cd Producer.v1
    dotnet run
    ```
    - Enter: `1 Hello World`
9. View registered schemas.
    - First get list of versions, then get version 1.
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

