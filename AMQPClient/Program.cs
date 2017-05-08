using Amqp;
using Amqp.Framing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var hostName = $"processiot.azure-devices.net";
            var port = 5671;
            var deviceId = "dev1";
            var sharedAccessKey = "NNE/DuQF0pIvlmcqs2a86jQOc/NypXUTNnkmzI/Q4TI=";

            var clientId = deviceId;
            var resource = $"{hostName}/{deviceId}";
            var username = $"{resource}/api-version=2016-11-14";
            var topic = $"devices/{deviceId}/messages/events/";
            var password = Generate(resource, sharedAccessKey, 3600);
            password = "SharedAccessSignature sr=processiot.azure-devices.net%2Fdevices%2Fdev1&sig=UkH%2BNsqPP%2FGa83WTJmlH%2FqrmPzHr4tF2PB1buPcYJM0%3D&se=1494285321";

            var connection = new Connection(new Address(hostName, port, null, null));
            var session = new Session(connection);

            var receiver = Task.Run(() => {
                ReceiverLink receiveLink = new ReceiverLink(session, "receive-link", $"/devices/{deviceId}/messages/deviceBound");
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
            var eventJson = JsonConvert.SerializeObject(eventObject);
            var eventBytes = Encoding.UTF8.GetBytes(eventJson);
            SenderLink senderLink = new SenderLink(session, "sender-link", $"/devices/{deviceId}/messages/events");
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


        static string Generate(string resourceUri, string signingKey, int expiresInSeconds = 0)
        {
            // Set expiration in seconds
            var expires = Timestamp() + expiresInSeconds;
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
            return (int)Math.Floor(diff.TotalSeconds);
        }
    }
}
