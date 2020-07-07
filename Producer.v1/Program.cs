using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Producer.v1
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Get producer options
            var config = LoadConfiguration();
            var producerOptions = config
                .GetSection(nameof(ProducerOptions))
                .Get<ProducerOptions>();

            // Prevent the process from terminating
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            // Produce events
            await Run_Producer(producerOptions.Brokers, producerOptions.RawTopic, producerOptions.SchemaRegistryUrl, cts.Token);
        }

        private static async Task Run_Producer(string brokerList, string topicName, string schemaRegistryUrl, CancellationToken cancellationToken)
        {
            var config = new ProducerConfig { BootstrapServers = brokerList };
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            using (var producer = new ProducerBuilder<int, HelloReply>(config)
                // Set value Protobuf serializer using schema registry
                .SetValueSerializer(new ProtobufSerializer<HelloReply>(schemaRegistry))
                .Build())
            {
                Console.WriteLine("\n-----------------------------------------------------------------------");
                Console.WriteLine($"Producer {producer.Name} producing on topic {topicName} to brokers {brokerList}.");
                Console.WriteLine("-----------------------------------------------------------------------");
                Console.WriteLine("To create a kafka message with integer key and string value:");
                Console.WriteLine("> key value<Enter>");
                Console.WriteLine("Ctrl-C then <Enter> to quit.\n");

                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write("> ");

                    string text;
                    try
                    {
                        text = Console.ReadLine();
                    }
                    catch (IOException)
                    {
                        // IO exception is thrown when ConsoleCancelEventArgs.Cancel == true.
                        break;
                    }
                    if (text == null || text.Length == 0)
                    {
                        // Console returned null before 
                        // the CancelKeyPress was treated
                        continue;
                    }

                    int key = 0;
                    var val = new HelloReply
                    {
                        Message = text
                    };

                    // split line if both key and value specified.
                    int index = text.IndexOf(" ");
                    if (index != -1)
                    {
                        key = int.Parse(text.Substring(0, index));
                        val.Message = text.Substring(index + 1);
                    }

                    try
                    {
                        // Note: Awaiting the asynchronous produce request below prevents flow of execution
                        // from proceeding until the acknowledgement from the broker is received (at the 
                        // expense of low throughput).
                        var deliveryReport = await producer.ProduceAsync(
                            topicName, new Message<int, HelloReply> { Key = key, Value = val });

                        Console.WriteLine($"delivered to: {deliveryReport.TopicPartitionOffset}");
                    }
                    catch (ProduceException<int, HelloReply> e)
                    {
                        Console.WriteLine($"failed to deliver message: {e.Message} [{e.Error.Code}]");
                    }
                }

                // Since we are producing synchronously, at this point there will be no messages
                // in-flight and no delivery reports waiting to be acknowledged, so there is no
                // need to call producer.Flush before disposing the producer.
            }
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
