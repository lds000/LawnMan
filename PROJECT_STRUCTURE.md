# BACKYARD BOSS SMART SPRINKLER SYSTEM - MASTER DOCUMENTATION
============================================================

## SYSTEM ARCHITECTURE OVERVIEW

The Backyard Boss system consists of three interconnected devices that work together to provide automated irrigation control with environmental monitoring:

```
┌─────────────────────────┐     ┌─────────────────────────┐     ┌─────────────────────────┐
│   GUI PC (Windows)      │     │  Controller Pi (RPi 4)  │     │  Sensor Pi (RPi Zero W) │
│   100.111.141.110       │     │   100.116.147.6         │     │   100.117.254.20        │
│                         │     │                         │     │                         │
│  • WPF Application      │────►│  • MQTT Broker (1883)   │◄────│  • Environmental Sensors│
│  • Schedule Management  │SFTP │  • Relay Control        │MQTT │  • Plant Health Sensors │
│  • System Monitoring    │     │  • Flask API (5000)     │     │  • Flow/Pressure Monitor│
│  • Manual Control       │◄────│  • Watering Logic       │     │  • Flask API (5001)     │
└─────────────────────────┘MQTT └─────────────────────────┘     └─────────────────────────┘
```

### Network Configuration
- **VPN:** All devices connected via Tailscale for secure communication
- **Primary Protocol:** MQTT for real-time data (broker on Controller Pi)
- **File Transfer:** SFTP for schedule/command uploads (GUI → Controller)
- **API Access:** HTTP REST endpoints on both Pis

---

## DEVICE SPECIFICATIONS

### 1. GUI PC (Windows Mini PC)
- **IP Address:** 100.111.141.110
- **Role:** Primary user interface and control center
- **Location:** Wall-mounted display in control room
- **Software:** WPF application (.NET 8) - "Backyard Boss GUI"

### 2. Controller Pi (Raspberry Pi 4 Model B)
- **IP Address:** 100.116.147.6
- **Role:** Main irrigation controller and MQTT broker
- **Location:** Near valve manifold/control box
- **Services:**
  - Mosquitto MQTT Broker (port 1883)
  - Flask HTTP API (port 5000)
  - SSH/SFTP Server (port 22)
  - Systemd services: main.py, flask_api.py

### 3. Sensor Pi (Raspberry Pi Zero W → upgrading to Pi 4)
- **IP Address:** 100.117.254.20
- **Role:** Environmental data collection node
- **Location:** Outdoor weatherproof enclosure, 50ft from control center
- **Services:**
  - Flask HTTP API (port 5001)
  - MQTT Client (publishes to broker)
  - Systemd services: SensorMonitor.py, system_status_publisher.py, error_reporter.py

---

## DATA FLOW AND COMMUNICATION

### MQTT Topics and Message Flow

```
Sensor Pi ──publishes──► MQTT Broker ◄──subscribes── GUI PC
                         (Controller Pi)
```

#### Published by Sensor Pi:
1. **sensors/environment** (every 1 second)
   ```json
   {
     "timestamp": "2024-01-15T10:30:00.000000",
     "temperature": 23.5,      // Celsius
     "humidity": 45.2,         // Percentage
     "wind_speed": 2.1,        // m/s
     "barometric_pressure": null
   }
   ```

2. **sensors/plant** (every 5 minutes)
   ```json
   {
     "timestamp": "2024-01-15T10:30:00.000000",
     "moisture": 120,          // Raw sensor value
     "lux": 350.5,            // Light level
     "soil_temperature": 22.3  // Celsius
   }
   ```

3. **sensors/sets** (every 1 second)
   ```json
   {
     "timestamp": "2024-01-15T10:30:00.000000",
     "flow_pulses": 12,
     "flow_litres": 0.026,
     "pressure_kpa": 310.5,
     "pressure_psi": 45.0
   }
   ```

4. **status/errors** (on error occurrence)
   - Raw error log lines from error_log.txt

5. **status/system** (every 30 seconds)
   ```json
   {
     "timestamp": "2024-01-15T10:30:00",
     "cpu_temp_c": 45.2,
     "memory_usage_percent": 35.6,
     "disk_usage_percent": 22.1,
     "uptime_hours": 168.5
   }
   ```

#### Published by Controller Pi:
1. **status/watering** (every 2 seconds)
   ```json
   {
     "system_status": "All Systems Nominal",
     "test_mode": false,
     "led_colors": {"system": "green", "zone1": "blue"},
     "zones": [{"name": "Garden", "status": "Watering"}],
     "current_run": {
       "set": "Garden",
       "start_time": "2024-01-15T10:25:00",
       "duration_minutes": 15.0,
       "phase": "Running",
       "time_remaining_sec": 300
     },
     "next_run": {...},
     "last_completed_run": {...},
     "upcoming_runs": [...],
     "today_is_watering_day": true
   }
   ```

2. **status/misters** (on change)
   ```json
   {
     "is_misting": true,
     "current_temp": 92.5,
     "duration_remaining_sec": 60,
     "next_check_sec": 1200
   }
   ```

### HTTP API Endpoints

#### Controller Pi (100.116.147.6:5000):
- `GET /status` - System status and current runs
- `POST /stop-all` - Emergency stop all watering
- `POST /set-test-mode` - Enable/disable test mode
- `GET /mist-status` - Misting system status
- `GET /history` - Watering history
- `POST /env-data` - Accept sensor data (backup to MQTT)

#### Sensor Pi (100.117.254.20:5001):
- `GET /pressure-avg-latest?n=100` - Recent pressure readings
- `GET /flow-avg-latest?n=100` - Recent flow readings
- `GET /temperature-avg-latest?n=100` - Recent temperature
- `GET /wind-avg-latest?n=100` - Recent wind speeds
- `GET /moisture-avg-latest?n=100` - Recent moisture levels
- `GET /soil-temperature-avg-latest2?n=100` - Soil temperature

### File Transfer (SFTP)

GUI PC uploads to Controller Pi via SFTP:
- `/home/lds00/sprinkler/sprinkler_schedule.json` - Main schedule
- `/home/lds00/sprinkler/manual_command.json` - Manual run commands
- `/home/lds00/sprinkler/stop_all_command.json` - Emergency stop
- `/home/lds00/sprinkler/test_mode.txt` - Test mode flag

---

## HARDWARE CONFIGURATION

### Controller Pi GPIO Assignments (BCM Numbering)
```
Relay Control (Water Valves):
- GPIO 17: Hanging Pots Zone
- GPIO 27: Garden Zone  
- GPIO 22: Misters

Status LEDs:
- GPIO 5: System Status
- GPIO 6: Hanging Pots LED
- GPIO 13: Garden LED
- GPIO 19: Misters LED
```

### Sensor Pi Hardware Configuration
```
I2C Devices:
- 0x29: TCS34725 Color Sensor (plant health)
- 0x48: ADS1115 ADC (future soil moisture)
- 0x49: ADS1115 ADC (pressure, wind direction)

1-Wire Sensors:
- 28-00000053ca7e: Soil Temperature (DS18B20)
- 28-000000523788: Environment Temperature (DS18B20)

GPIO Sensors:
- GPIO 25: Flow Sensor (YF-S201, 450 pulses/L)
- GPIO 13: Wind Speed (reed switch anemometer)
- GPIO 17: Status LED
```

---

## SOFTWARE ARCHITECTURE

### Controller Pi Software Structure
```
/home/lds00/sprinkler/
├── main.py                 # Main controller service
├── flask_api.py           # HTTP API service
├── src/                   # Core modules
│   ├── gpio_controller.py # Hardware control
│   ├── run_manager.py     # Watering logic
│   ├── scheduler.py       # Schedule management
│   ├── mqtt_handler.py    # MQTT publishing
│   └── config.py          # Pin assignments
├── config/                # Service files
├── tests/                 # Test suite
└── logs/                  # Runtime logs
```

### Sensor Pi Software Structure
```
/home/lds00/ColorSensorTest/
├── SensorMonitor.py       # Main sensor service
├── services/
│   ├── mqtt_publisher.py  # MQTT client
│   └── log_manager.py     # Logging
├── config/
│   ├── config.json        # Sensor toggles
│   └── calibration.json   # Calibration values
└── logs/                  # Sensor data logs
```

### GUI PC Software Components
- **Main Application:** BackyardBoss.exe
- **Configuration Files:**
  - sprinkler_schedule.json (local cache)
  - myMap.json (zone visualization)
- **Key Modules:**
  - MQTT Client (subscriber only)
  - SFTP Client (schedule upload)
  - Data Visualization (OxyPlot)
  - Schedule Editor
  - Manual Control Interface

---

## OPERATIONAL WORKFLOWS

### 1. Normal Scheduled Watering
```
1. Controller Pi checks schedule (sprinkler_schedule.json)
2. At scheduled time, activates relay for zone
3. Publishes status to MQTT (status/watering)
4. GUI PC receives status and updates display
5. Sensor Pi monitors flow/pressure during watering
6. Controller logs completion to watering_history.log
```

### 2. Manual Watering
```
1. User clicks "Run Once" in GUI PC
2. GUI creates manual_command.json
3. GUI uploads file via SFTP to Controller Pi
4. Controller detects file and starts manual run
5. Status updates flow via MQTT to GUI
```

### 3. Environmental Monitoring
```
1. Sensor Pi reads all sensors every second
2. Averages data over configured intervals
3. Publishes to MQTT topics
4. GUI PC displays real-time graphs
5. Controller Pi can use data for smart decisions
```

### 4. Emergency Stop
```
1. User clicks "Stop All" in GUI
2. GUI creates stop_all_command.json
3. Uploads via SFTP to Controller
4. Controller immediately stops all zones
5. Status update sent via MQTT
```

---

## AUTHENTICATION & SECURITY

### SSH/SFTP Credentials
- Host: 100.116.147.6
- Port: 22
- Username: lds00
- Password: Celica1!

### MQTT Configuration
- No authentication (local network only)
- Future: Add username/password authentication
- All communication over Tailscale VPN

---

## LOGGING AND DIAGNOSTICS

### Controller Pi Logs
- `/home/lds00/sprinkler/error_log.txt` - System errors
- `/home/lds00/sprinkler/watering_history.log` - Run history
- `journalctl -u main.service` - Service logs

### Sensor Pi Logs
- `/home/lds00/ColorSensorTest/logs/error_log.txt` - Errors
- `/home/lds00/ColorSensorTest/logs/avg_*.txt` - Sensor data
- `journalctl -u SensorMonitor.service` - Service logs

### GUI PC Diagnostics
- Debug console output
- Connection status indicators
- MQTT message monitor

---

## MAINTENANCE PROCEDURES

### Updating Controller Pi
```bash
ssh lds00@100.116.147.6
cd /home/lds00/sprinkler
git pull
sudo systemctl restart main.service flask_api.service
```

### Updating Sensor Pi
```bash
ssh lds00@100.117.254.20
cd /home/lds00/ColorSensorTest
git pull
./restart  # Restarts all services
```

### Monitoring System Health
- Check MQTT: `mosquitto_sub -h 100.116.147.6 -t '#' -v`
- View logs: `tail -f /path/to/error_log.txt`
- Service status: `sudo systemctl status <service-name>`

---

## FUTURE ENHANCEMENTS

### Planned Upgrades
1. Sensor Pi hardware upgrade (Zero W → Pi 4)
2. Add soil moisture sensor (ADS1115 0x48)
3. Weather-based scheduling
4. Water usage tracking and reporting
5. Mobile app companion
6. Multi-zone parallel watering
7. Leak detection system

### Integration Opportunities
- Home Assistant integration
- Weather API for predictive watering
- Historical data analytics
- Remote access web portal

---

## TROUBLESHOOTING QUICK REFERENCE

### Common Issues and Solutions

1. **No MQTT Data**
   - Check Mosquitto service: `sudo systemctl status mosquitto`
   - Verify Tailscale connection: `tailscale status`
   - Check firewall rules

2. **Schedule Not Updating**
   - Verify SFTP credentials
   - Check file permissions on Pi
   - Look for errors in GUI debug output

3. **Sensors Not Reading**
   - Run `i2cdetect -y 1` to check I2C devices
   - Verify GPIO connections
   - Check sensor power supply

4. **GUI Connection Failed**
   - Ping Controller Pi: `ping 100.116.147.6`
   - Check MQTT broker accessibility
   - Verify Tailscale is running on PC

---

## SYSTEM REQUIREMENTS

### Minimum Hardware
- Controller Pi: Raspberry Pi 4B (2GB+ RAM)
- Sensor Pi: Raspberry Pi Zero W (upgrading to Pi 4)
- GUI PC: Windows 10/11 with .NET 8 Runtime

### Network Requirements
- Stable WiFi/Ethernet for all devices
- Tailscale VPN installed and configured
- Port 1883 (MQTT) open between devices
- Port 22 (SSH) accessible for maintenance