# Copilot Custom Instructions

## Program Purpose

This program is a WPF application for monitoring and controlling a smart sprinkler system. It communicates with Raspberry Pi devices over MQTT and SFTP to:
- Upload and manage watering schedules.
- Monitor real-time sensor data (environment, plant, soil) from the Sensor Pi (Raspberry Pi Zero W).
- Control and receive status updates from the Sprinkler Main Module (Raspberry Pi 4 Model B).
- Provide a user interface for scheduling, manual control, and system status visualization.

## IP Addresses and Their Purposes

- **100.111.141.110** (This machine):
  - This is the developer or client machine running the WPF application and connecting to the MQTT broker and Raspberry Pi devices.

- **100.117.254.20** (Raspberry Pi Zero W, Sensor Pi):
  - This device is responsible for collecting sensor data (e.g., environment, plant, or soil sensors) and publishing it to the MQTT broker.

- **100.116.147.6** (Raspberry Pi 4 Model B, Sprinkler Main Module):
  - This device acts as the main controller for the sprinkler system, receiving commands and schedules, and controlling the watering hardware. It may also serve as the MQTT broker.

## Usage Guidance
- When referencing or connecting to devices in code, use the above IP addresses according to their described roles.
- The WPF application typically connects to the Pi 4 (100.116.147.6) for schedule uploads and control, and may subscribe to MQTT topics for sensor data from the Pi Zero W (100.117.254.20).
- If you need to change broker addresses or endpoints, update them in the relevant service or configuration files.

---

_This file provides context for GitHub Copilot and Copilot Chat about the network topology, device roles, and the purpose of this project._
