using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace BackyardBoss.Services
{
    public class MqttService
    {
        private IMqttClient _client;
        public event Action<string, JsonElement> MessageReceived;

        public MqttService()
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
            _client.ApplicationMessageReceivedAsync += async e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var json = JsonDocument.Parse(payload).RootElement;
                    MessageReceived?.Invoke(topic, json);
                }
                catch { /* Optionally log/handle error */ }
                await Task.CompletedTask;
            };
        }

        public async Task ConnectAsync(string brokerAddress, int port = 1883)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(brokerAddress, port)
                .WithCleanSession()
                .Build();
            await _client.ConnectAsync(options);
            // Subscribe to topics after connecting
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensors/environment").Build());
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensors/plant").Build());
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensors/sets").Build());
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("status/watering").Build());
        }
    }
}
