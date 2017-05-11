using Amqp;
using Amqp.Framing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AMQPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        // https://paolopatierno.wordpress.com/2015/10/24/connecting-to-the-azure-iot-hub-using-an-the-amqp-stack/
        static async Task MainAsync(string[] args)
        {
            var iotHubName = "protocoliot";
            var hostName = $"{iotHubName}.azure-devices.net";
            var port = 8883;
            var deviceId = "dev1";
            var sharedAccessKey = "MjMB3L8rDRUgpoLy/M0fzghgTS25CRawiE3+de+G9TI=";

            var clientId = deviceId;
            var resourceId = $"{hostName}/devices/{deviceId}";
            var username = $"{hostName}/{deviceId}/api-version=2016-11-14";
            var senderLinkPath = $"/devices/{deviceId}/messages/events";
            var receiveLinkPath = $"/devices/{deviceId}/messages/deviceBound";
            var password = CreateShareAccessSignature(resourceId, sharedAccessKey);

            var connection = new Connection(new Address(hostName, port, null, null));
            var session = new Session(connection);

            var receiver = Task.Run(() => {
                ReceiverLink receiveLink = new ReceiverLink(session, "receive-link", receiveLinkPath);
                while (true)
                {
                    Message received = receiveLink.Receive();
                    if (received != null)
                    {
                        // do something
                        receiveLink.Accept(received);
                    }
                }
                receiveLink.Close();
            });
            receiver.Start();

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
            var eventJson = JsonConvert.SerializeObject(eventObject);
            eventBytes = Encoding.UTF8.GetBytes(eventJson);

            //// Protobuf
            //var eventStream = new MemoryStream();
            //ProtoBuf.Serializer.Serialize(eventStream, eventObject);
            //eventBytes = eventStream.ToArray();

            SenderLink senderLink = new SenderLink(session, "sender-link", senderLinkPath);
            Message message = new Message()
            {
                BodySection = new Data() { Binary = eventBytes }
            };

            senderLink.Send(message);
            senderLink.Close();

            await session.CloseAsync();
        }

        private static void OnMessage(ReceiverLink receiver, Message message)
        {
            //AmqpTrace.WriteLine(TraceLevel.Information, "received command from controller");
            //int button = (int)message.ApplicationProperties["button"];
            //this.OnAction(button);
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
