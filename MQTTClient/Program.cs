using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using static System.Console;

namespace MQTTClient
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var hostName = $"processiot.azure-devices.net";
            var deviceId = "dev1";
            var sharedAccessKey = "NNE/DuQF0pIvlmcqs2a86jQOc/NypXUTNnkmzI/Q4TI=";

            var clientId = deviceId;
            var resource = $"{hostName}/{deviceId}";
            var username = $"{resource}/api-version=2016-11-14";
            var topic = $"/devices/{deviceId}/messages/events";
            var password = Generate(resource, sharedAccessKey, 3600);
            password = "SharedAccessSignature sr=processiot.azure-devices.net%2Fdevices%2Fdev1&sig=UkH%2BNsqPP%2FGa83WTJmlH%2FqrmPzHr4tF2PB1buPcYJM0%3D&se=1494285321";
            var client = new MqttClient(hostName, 8883, true, MqttSslProtocols.TLSv1_2,
                null,
                null
            );
            client.MqttMsgPublished += (s, e) =>
            {
                WriteLine($"Hello {e.MessageId}");
            };
            client.MqttMsgPublishReceived += (s, e) =>
            {
                WriteLine($"Hello {e.Message}");
            };
            var id = client.Connect(clientId, username, password);

            var eventObject = new
            {
                DeviceId = deviceId,
                Data = 29,
                Index = 1,
                DateTime = DateTimeOffset.Now.ToString()
            };
            var eventJson = JsonConvert.SerializeObject(eventObject);
            var eventBytes = Encoding.UTF8.GetBytes(eventJson);
            var result = client.Publish(topic, eventBytes);
            client.Disconnect();
        }


        static string Generate(string resourceUri, string signingKey, int expiresInSeconds = 0)
        {
            // Set expiration in seconds
            var expires = Timestamp()+ expiresInSeconds;
            var toSign = $"{Uri.EscapeDataString(resourceUri)}\n{expires}";
            byte[] bytesToSign = Encoding.UTF8.GetBytes(toSign);

            // Use crypto
            var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
            var hash = hmacsha256.ComputeHash(bytesToSign);
            var base64UriEncoded = Uri.EscapeDataString(Convert.ToBase64String(hash));

            // Construct autorization string
            var token = $"SharedAccessSignature sr={Uri.EscapeDataString(resourceUri)}&sig={base64UriEncoded}&se={expires}";
            return token;
        }

        public static long Timestamp()
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = DateTime.Now.ToUniversalTime() - origin;
            return (int) Math.Floor(diff.TotalSeconds);
        }
    }
}
