using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace ReceiveTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var connectionString = "HostName=processiot.azure-devices.net;DeviceId=dev1;SharedAccessKey=NNE/DuQF0pIvlmcqs2a86jQOc/NypXUTNnkmzI/Q4TI=";
            var transportType = TransportType.Mqtt;
            var deviceClient = DeviceClient.CreateFromConnectionString(connectionString, transportType);
            var i = 0;
            var totalTime = 0.0;
            while (true)
            {
                if (i == 100) break;
                var timeA = DateTimeOffset.Now;
                var message = await deviceClient.ReceiveAsync();
                var timeB = DateTimeOffset.Now;
                if (message == null)
                {
                    WriteLine("NULL");
                    continue;
                }

                var totalMilliseconds = (timeB - timeA).TotalMilliseconds;
                totalTime += totalMilliseconds;
                var averageMilliseconds = totalTime / i;
                var text = Encoding.UTF8.GetString(message.GetBytes());
                WriteLine($"Received {i + 1}: {text}");
                await deviceClient.CompleteAsync(message);
                WriteLine($"Time elapsed: {totalMilliseconds}");
                WriteLine($"Average time: {averageMilliseconds}");
                i++;
            }
            ReadLine();
        }
    }
}
