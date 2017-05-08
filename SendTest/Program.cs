using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace ProtocolTest
{
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
            
            for (var i = 1; i<=100; i++)
            {
                var eventObject = new
                {
                    DeviceId = "dev1",
                    Data = value,
                    Index = i,
                    DateTime = DateTimeOffset.Now.ToString()
                };
                var eventJson = JsonConvert.SerializeObject(eventObject);
                var eventBytes = Encoding.UTF8.GetBytes(eventJson);
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
