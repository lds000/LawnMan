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
            var avgFlow = bufferCopy.Average(x => x.FlowTotalLiters);
            var timestamp = bufferCopy.Max(x => x.Timestamp); // Use the latest timestamp

            // Insert averaged values into the database
            var sqliteRepo = new SqliteSensorDataRepository("pressure_data.db");
            sqliteRepo.InsertSensorReadingAsync(timestamp, 0, avgPressure, 0, avgFlow).Wait();

            System.Diagnostics.Debug.WriteLine($"Inserted averaged sensor reading: Timestamp={timestamp}, AvgPressurePsi={avgPressure}, AvgFlowTotalLiters={avgFlow}");

            int numSamples = bufferCopy.Count;
            string version = "1.0"; // Or use null if you don't track version

            var avgData = new PressureAvgData
            {
                Timestamp = timestamp, // your calculated timestamp
                AvgPressurePsi = avgPressure, // your calculated average
                NumSamples = numSamples,      // your sample count
                Version = version            // your version string, if any
            };

            // Add to PressureAvgHistory on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = ProgramEditorViewModel.Current;
                if (vm != null)
                {
                    vm.PressureAvgHistory.Add(avgData);
                    vm.LatestPressurePsi = avgPressure; // Optionally update LatestPressurePsi
                }
            });
        }
    }
}
