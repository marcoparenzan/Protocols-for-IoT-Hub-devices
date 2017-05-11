using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            const int QoS_AT_MOST_ONCE = 1;

            var iotHubName = "protocoliot";
            var hostName = $"{iotHubName}.azure-devices.net";
            var port = 8883;
            var deviceId = "dev1";
            var sharedAccessKey = "MjMB3L8rDRUgpoLy/M0fzghgTS25CRawiE3+de+G9TI=";

            var clientId = deviceId;
            var resourceId = $"{hostName}/devices/{deviceId}";
            var username = $"{hostName}/{deviceId}/api-version=2016-11-14";
            var devicePublishTopic = $"devices/{deviceId}/messages/events/";
            var deviceSubscribeTopic = $"devices/{deviceId}/messages/devicebound/#";
            var password = CreateShareAccessSignature(resourceId, sharedAccessKey);

            var client = new MqttClient(hostName, port, true, MqttSslProtocols.TLSv1_2, null, null);
            client.MqttMsgPublished += (s, e) =>
            {
                WriteLine($"Sent MessageId={e.MessageId}");
            };
            client.MqttMsgPublishReceived += (s, e) =>
            {
                WriteLine($"Topic received {e.Topic}: {Encoding.UTF8.GetString(e.Message)}");
            };
            var id = client.Connect(clientId, username, password);

            var eventObject = new
            {
                DeviceId = deviceId,
                Data = 29,
                Index = 1,
                DateTime = DateTimeOffset.Now.ToString()
            };

            // reference to Bytes to send
            byte[] eventBytes = null;

            // JSON
            //var eventJson = JsonConvert.SerializeObject(eventObject);
            //eventBytes = Encoding.UTF8.GetBytes(eventJson);

            //// Protobuf
            var eventStream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(eventStream, eventObject);
            eventBytes = eventStream.ToArray();

            var result = client.Publish(devicePublishTopic, eventBytes, QoS_AT_MOST_ONCE, false);
            client.Subscribe(new string[] { deviceSubscribeTopic }, new byte[] { QoS_AT_MOST_ONCE });
            ReadLine();
            client.Disconnect();
        }

        private static readonly DateTime EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        private static string CreateShareAccessSignature(string resourceUri, string key, int timeToLive = 86400)
        {
            var sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + timeToLive);
            string text2 = WebUtility.UrlEncode(resourceUri);

            string value;
            using (HMACSHA256 hMACSHA = new HMACSHA256(Convert.FromBase64String(key)))
            {
                value = Convert.ToBase64String(hMACSHA.ComputeHash(Encoding.UTF8.GetBytes($"{text2}\n{expiry}")));
            }

            return $"SharedAccessSignature sr={text2}&sig={WebUtility.UrlEncode(value)}&se={expiry}";
        }
    }
}
