using System.Collections.Generic;

namespace Consumer.v1
{
    public class ConsumerOptions
    {
        public string Brokers { get; set; }
        public List<string> TopicsList { get; set; }
    }
}