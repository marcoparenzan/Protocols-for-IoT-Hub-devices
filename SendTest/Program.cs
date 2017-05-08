using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace ProtocolTest
{
    [ProtoContract]
    public class Event
    {
        [ProtoMember(1)]
        public string DeviceId { get; set; }
        [ProtoMember(2)]
        public double Data { get; set; }
        [ProtoMember(3)]
        public int Index { get; set; }
        [ProtoMember(4)]
        public DateTime DateTime { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "HostName=processiot.azure-devices.net;DeviceId=dev1;SharedAccessKey=NNE/DuQF0pIvlmcqs2a86jQOc/NypXUTNnkmzI/Q4TI=";
            var transportType = TransportType.Amqp;
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportType);

            var random = new Random();
            double value = 20 + random.NextDouble();

            var totalTime = 0.0;

            ProtoBuf.Serializer.PrepareSerializer<Event>();
            for (var i = 1; i<=100; i++)
            {
                var eventObject = new Event
                {
                    DeviceId = "dev1",
                    Data = value,
                    Index = i,
                    DateTime = DateTime.Now
                };

                var serializationStream = new MemoryStream();
                ProtoBuf.Serializer.Serialize(serializationStream, eventObject);

                var eventJson = JsonConvert.SerializeObject(eventObject);
                //var eventBytes = Encoding.UTF8.GetBytes(eventJson);
                var eventBytes = serializationStream.ToArray();
                var eventMessage = new Message(eventBytes);
                
                WriteLine($"Sending {i}th event long {eventBytes.Length}: {eventJson}");
                var timeA = DateTimeOffset.Now;
                deviceClient.SendEventAsync(eventMessage).Wait();
                var timeB = DateTimeOffset.Now;
                var totalMilliseconds = (timeB - timeA).TotalMilliseconds;
                totalTime += totalMilliseconds;
                var averageMilliseconds = totalTime / i;
                WriteLine($"Time elapsed: {totalMilliseconds}");
                WriteLine($"Average time: {averageMilliseconds}");
            }

            ReadLine();
        }
    }
}
