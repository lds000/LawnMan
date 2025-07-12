using BackyardBoss.Data;
using MQTTnet;
using MQTTnet.Client;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using BackyardBoss.ViewModels;

namespace BackyardBoss.Services
{
    public class MqttService
    {
        private IMqttClient _client;
        public event Action<string, JsonElement> MessageReceived;

        // Add a buffer to store sensor readings for averaging
        private readonly List<(DateTime Timestamp, double PressurePsi, double FlowTotalLiters)> _sensorBuffer = new();
        private readonly object _bufferLock = new();

        // Timer to process the buffer every 5 seconds
        private readonly System.Timers.Timer _averagingTimer;

        public MqttService()
        {
            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();

            // Initialize the averaging timer
            _averagingTimer = new System.Timers.Timer(5000); // 5 seconds
            _averagingTimer.Elapsed += ProcessSensorBuffer;
            _averagingTimer.Start();

            _client.ApplicationMessageReceivedAsync += async e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    // Raise the event for all topics
                    if (MessageReceived != null)
                    {
                        var json = JsonDocument.Parse(payload).RootElement;
                        MessageReceived.Invoke(topic, json);
                    }

                    // Existing buffer logic for sensors/sets
                    if (topic == "sensors/sets")
                    {
                        var json = JsonDocument.Parse(payload).RootElement;
                        var data = JsonSerializer.Deserialize<BackyardBoss.Models.SetsData>(json.GetRawText());
                        if (data != null)
                        {
                            lock (_bufferLock)
                            {
                                _sensorBuffer.Add((data.Timestamp, data.PressurePsi, data.FlowLitres));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error processing MQTT message: {ex.Message}");
                }
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
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("status/misters").Build());
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("sensors/pressure_avg").Build());
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("status/system").Build()); // <-- Added subscription for status/system

                System.Diagnostics.Debug.WriteLine("MQTT client connected and subscribed to topics.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MQTT connection error: {ex.Message}");
            }
        }

        private void ProcessSensorBuffer(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<(DateTime Timestamp, double PressurePsi, double FlowTotalLiters)> bufferCopy;
            lock (_bufferLock)
            {
                bufferCopy = new List<(DateTime, double, double)>(_sensorBuffer);
                _sensorBuffer.Clear();
            }

            if (bufferCopy.Count == 0) return;

            // Calculate averages
            var avgPressure = bufferCopy.Average(x => x.PressurePsi);
            var avgFlowLpm = bufferCopy.Average(x => x.FlowTotalLiters); // Assuming FlowTotalLiters is LPM, else convert
            var timestamp = bufferCopy.Max(x => x.Timestamp); // Use the latest timestamp

            // Determine current zone_id (0 if no set is running)
            int zoneId = 0;
            var vm = ProgramEditorViewModel.Current;
            if (vm != null && vm.CurrentRun != null && !string.IsNullOrEmpty(vm.CurrentRun.Set))
            {
                // Try to map set name to a zone id (index+1), fallback to 0
                var set = vm.Sets.FirstOrDefault(s => s.SetName == vm.CurrentRun.Set);
                if (set != null)
                    zoneId = vm.Sets.IndexOf(set) + 1;
            }



        }
    }
}
