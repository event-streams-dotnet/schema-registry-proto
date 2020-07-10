using GoogleTimestamp = Google.Protobuf.WellKnownTypes.Timestamp;

namespace Protos
{
    public interface IHelloReply
    {
        string Message { get; set; }
    }

    public interface IHelloReply_1 : IHelloReply
    {
    }

    public interface IHelloReply_2 : IHelloReply
    {
        int? TemperatureF { get; set; }
    }

    public interface IHelloReply_3 : IHelloReply
    {
        int? TemperatureF { get; set; }
        public GoogleTimestamp DateTimeStamp { get; set; }
    }

    public interface IHelloReply_4 : IHelloReply
    {
        public GoogleTimestamp DateTimeStamp { get; set; }
    }

    public interface IHelloReply_5 : IHelloReply
    {
        public string DateTimeStamp { get; set; }
    }
}
