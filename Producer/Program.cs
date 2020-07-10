using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using GoogleTimestamp = Google.Protobuf.WellKnownTypes.Timestamp;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Protos;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Producer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Prevent the process from terminating
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            // Get producer options
            var config = LoadConfiguration();
            var producerOptions = config
                .GetSection(nameof(ProducerOptions))
                .Get<ProducerOptions>();

            // Confirm topic
            Console.WriteLine("Ctrl-C then <Enter> to quit.");
            Console.WriteLine($"\nTopic: {producerOptions.RawTopic}");
            Console.WriteLine("> Confirm: <Enter>, New value<Enter>");
            var topic = Console.ReadLine();
            if (topic.Length == 0)
                topic = producerOptions.RawTopic;

            Console.WriteLine("\n-----------------------------------------------------------------------");
            Console.WriteLine($"Producing on topic {topic} to brokers {producerOptions.Brokers}.");
            Console.WriteLine("-----------------------------------------------------------------------");

            while (!cts.Token.IsCancellationRequested)
            {
                // Get schema version number
                Console.WriteLine("\nEnter schema version number:");
                if (!int.TryParse(Console.ReadLine(), out int version))
                    break;
                Console.WriteLine($"Schema version: {version}");

                Console.WriteLine("\nTo create a kafka message with integer key and string value:");
                Console.WriteLine("> Key value<Enter>");
                Console.Write("> ");

                string input;
                try
                {
                    input = Console.ReadLine();
                }
                catch (IOException)
                {
                    // IO exception is thrown when ConsoleCancelEventArgs.Cancel == true.
                    break;
                }
                if (input == null || input.Length == 0)
                {
                    // Console returned null before 
                    // the CancelKeyPress was treated
                    continue;
                }

                // Split line if both key and value specified.
                int key = 0;
                var text = string.Empty;
                int index = input.IndexOf(" ");
                if (index != -1)
                {
                    key = int.Parse(input.Substring(0, index));
                    text = input.Substring(index + 1);
                }

                // Produce events
                switch (version)
                {
                    case 1:
                        await Run_Producer<Protos.v1.HelloReply>(producerOptions.Brokers, topic, producerOptions.SchemaRegistryUrl, key, text);
                        break;
                    case 2:
                        await Run_Producer<Protos.v2.HelloReply>(producerOptions.Brokers, topic, producerOptions.SchemaRegistryUrl, key, text);
                        break;
                    case 3:
                        await Run_Producer<Protos.v3.HelloReply>(producerOptions.Brokers, topic, producerOptions.SchemaRegistryUrl, key, text);
                        break;
                    case 4:
                        await Run_Producer<Protos.v4.HelloReply>(producerOptions.Brokers, topic, producerOptions.SchemaRegistryUrl, key, text);
                        break;
                    case 5:
                        await Run_Producer<Protos.v5.HelloReply>(producerOptions.Brokers, topic, producerOptions.SchemaRegistryUrl, key, text);
                        break;
                }
            }
        }

        private static async Task Run_Producer<TValue>(string brokerList, string topicName, 
            string schemaRegistryUrl, int key, string text)
            where TValue : class, IMessage<TValue>, new()
        {
            var config = new ProducerConfig { BootstrapServers = brokerList };
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            using (var producer = new ProducerBuilder<int, TValue>(config)
                // Set value Protobuf serializer using schema registry
                .SetValueSerializer(new ProtobufSerializer<TValue>(schemaRegistry))
                .Build())
            {
                // Create message properties
                var val = CreateMessageValue<TValue>(text);
                PrintMessageValue(key, val);

                try
                {
                    // Note: Awaiting the asynchronous produce request below prevents flow of execution
                    // from proceeding until the acknowledgement from the broker is received (at the 
                    // expense of low throughput).
                    var deliveryReport = await producer.ProduceAsync(
                        topicName, new Message<int, TValue> { Key = key, Value = val });

                    Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset} for producer: {producer.Name}");
                }
                catch (ProduceException<int, TValue> e)
                {
                    Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
                }

                // Since we are producing synchronously, at this point there will be no messages
                // in-flight and no delivery reports waiting to be acknowledged, so there is no
                // need to call producer.Flush before disposing the producer.
            }
        }

        private static TValue CreateMessageValue<TValue>(string msg)
            where TValue : class, IMessage<TValue>, new()
        {
            int? tmp = new Random().Next(-32, 100);
            var ts = GoogleTimestamp.FromDateTime(DateTime.UtcNow);
            var val = new TValue();
            if (val is IHelloReply val1)
            {
                val1.Message = msg;
            };
            if (val is IHelloReply_2 val2)
            {
                val2.TemperatureF = tmp;
            };
            if (val is IHelloReply_3 val3)
            {
                val3.TemperatureF = tmp;
                val3.DateTimeStamp = ts;
            };
            if (val is IHelloReply_4 val4)
            {
                val4.DateTimeStamp = ts;
            };
            if (val is IHelloReply_5 val5)
            {
                val5.DateTimeStamp = DateTime.UtcNow.ToLongTimeString();
            };
            return val;
        }

        private static void PrintMessageValue<TValue>(int key, TValue val)
            where TValue : class, IMessage<TValue>, new()
        {
            var msg = string.Empty;
            var tmp = string.Empty;
            GoogleTimestamp ts = null;
            if (val is Protos.v1.HelloReply val1)
            {
                msg = val1.Message;
            }
            if (val is Protos.v2.HelloReply val2)
            {
                msg = val2.Message;
                tmp = val2.TemperatureF != null ? $"at {val2.TemperatureF} degrees" : string.Empty;
            }
            if (val is Protos.v3.HelloReply val3)
            {
                msg = val3.Message;
                tmp = val3.TemperatureF != null ? $"at {val3.TemperatureF} degrees" : string.Empty;
                ts = val3.DateTimeStamp;
            }
            if (val is Protos.v4.HelloReply val4)
            {
                msg = val4.Message;
                ts = val4.DateTimeStamp;
            }
            if (val is Protos.v5.HelloReply val5)
            {
                msg = val5.Message;
                var dt = DateTime.SpecifyKind(DateTime.Parse(val5.DateTimeStamp), DateTimeKind.Utc);
                ts = GoogleTimestamp.FromDateTime(dt);
            }
            Console.WriteLine($"\nMessage value: {key} (key) {msg} {tmp} {ts}");
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            return builder.Build();
        }
    }
}
