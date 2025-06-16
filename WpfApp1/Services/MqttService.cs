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
                    // Only parse JSON for known topics
                    if (topic == "sensors/environment" || topic == "sensors/plant" || topic == "sensors/sets" || topic == "status/watering" || topic == "sensors/pressure_avg")
                    {
                        if (!string.IsNullOrWhiteSpace(payload))
                        {
                            try
                            {
                                var json = JsonDocument.Parse(payload).RootElement;
                                MessageReceived?.Invoke(topic, json);
                            }
                            catch (System.Text.Json.JsonException jsonEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"MQTT JSON parse error on topic '{topic}': {jsonEx.Message}");
                                System.Diagnostics.Debug.WriteLine($"Payload: {payload}");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"MQTT message on topic '{topic}' has empty payload.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"MQTT message on topic '{topic}' is not parsed as JSON. Payload: {payload}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"MQTT handler error: {ex.Message}");
                }
                await Task.CompletedTask;
            };
        }

        public async Task ConnectAsync(string brokerAddress = "100.116.147.6", int port = 1883)
        {
            try
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
                // Subscribe to sensors/pressure_avg
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensors/pressure_avg").Build());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT connection error: {ex.Message}");
            }
        }
    }
}
