using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace CloudTest
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }
        static async Task MainAsync(string[] args)
        {
            var connectionString = "HostName=processiot.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=rPkHPRJwThs8fub0lahpayn0ThLhCBtFecEVFHSJFAs=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            var random = new Random();
            double value = 20 + random.NextDouble();

            for (var i = 0; i < 100; i++)
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
                await serviceClient.SendAsync(eventObject.DeviceId, eventMessage);
                WriteLine($"[{i}] {eventJson}");
                await Task.Delay(5000);
            }
        }
    }
}
