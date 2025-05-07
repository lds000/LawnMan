import os
import time
import json
import threading
from datetime import datetime
from flask import Flask, jsonify

try:
    import RPi.GPIO as GPIO
except ImportError:
    from unittest import mock
    GPIO = mock.MagicMock()

# --- Flask App ---
app = Flask(__name__)

# --- Constants ---
SCHEDULE_FILE = "/home/lds00/sprinkler_schedule.json"
MANUAL_COMMAND_FILE = "/home/lds00/manual_command.json"
STATUS_LED_PIN = 5
LOG_FILE = "/home/lds00/sprinkler_status.log"
HISTORY_LOG_FILE = "/home/lds00/watering_history.log"

RELAY_PINS = {
    "Hanging Pots": 17,
    "Garden": 27,
    "Misters": 22
}

# --- Status Tracking ---
declare_log = []
CURRENT_RUN = {"Running": False, "Set": "", "Time_Remaining_Sec": 0}

# --- GPIO Setup ---
def initialize_gpio():
    print("[SYSTEM] Controller starting up...")
    print("[GPIO] Initializing GPIO pins")
    GPIO.setmode(GPIO.BCM)
    for set_name, pin in RELAY_PINS.items():
        print(f"[GPIO] Setting up pin {pin} for {set_name}")
        GPIO.setup(pin, GPIO.OUT)
        GPIO.output(pin, GPIO.LOW)
    GPIO.setup(STATUS_LED_PIN, GPIO.OUT)
    GPIO.output(STATUS_LED_PIN, GPIO.LOW)
    startup_blink()

# --- Startup Blink ---
def startup_blink():
    for _ in range(3):
        GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
        time.sleep(0.15)
        GPIO.output(STATUS_LED_PIN, GPIO.LOW)
        time.sleep(0.15)

# --- Logging ---
def log(message):
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    entry = f"[{now}] {message}"
    print(entry)
    declare_log.append(entry)
    with open(LOG_FILE, "a") as f:
        f.write(entry + "\n")

def log_watering_history(set_name, start_dt, end_dt, source="SCHEDULED"):
    entry = f"{start_dt.date()} {set_name} {source.upper()} START: {start_dt.strftime('%H:%M:%S')} STOP: {end_dt.strftime('%H:%M:%S')}\n"
    with open(HISTORY_LOG_FILE, "a") as f:
        f.write(entry)

# --- JSON Utilities ---
def load_json(path):
    log(f"[LOAD] Loading JSON from {path}")
    with open(path, 'r') as f:
        data = json.load(f)

    # Normalize start_times to "HH:mm" format
    if "start_times" in data:
        for entry in data["start_times"]:
            try:
                raw_time = entry.get("time", "").strip()
                normalized = datetime.strptime(raw_time, "%H:%M").strftime("%H:%M")
                entry["time"] = normalized
            except Exception as e:
                log(f"[WARN] Could not normalize time '{entry.get('time')}': {e}")

    return data

def get_schedule_day_index():
    base = datetime(2024, 1, 1)
    delta = (datetime.now().date() - base.date()).days
    return delta % 14

def should_run_today(schedule):
    idx = get_schedule_day_index()
    return schedule.get("schedule_days", [False] * 14)[idx]

def is_start_time_enabled(schedule, time_str):
    return any(
        entry.get("time") == time_str and entry.get("isEnabled", False)
        for entry in schedule.get("start_times", [])
    )

def get_mist_flags(schedule, time_str):
    mist = schedule.get("mist", {})
    return mist.get(f"time_{time_str.replace(':', '')}", False)

# --- GPIO Control ---
def turn_on(pin):
    GPIO.output(pin, GPIO.HIGH)
    GPIO.output(STATUS_LED_PIN, GPIO.HIGH)

def turn_off(pin):
    GPIO.output(pin, GPIO.LOW)
    GPIO.output(STATUS_LED_PIN, GPIO.LOW)

def run_set(set_name, duration_minutes, source="SCHEDULED"):
    pin = RELAY_PINS.get(set_name)
    if pin is None:
        log(f"[ERROR] Unknown set name: {set_name}")
        return

    log(f"[SET] Running {set_name} for {duration_minutes} min ({source})")
    start_time = datetime.now()
    turn_on(pin)

    for i in range(duration_minutes * 60):
        time.sleep(1)
        CURRENT_RUN.update({
            "Running": True,
            "Set": set_name,
            "Time_Remaining_Sec": duration_minutes * 60 - i
        })

    turn_off(pin)
    end_time = datetime.now()
    log_watering_history(set_name, start_time, end_time, source)
    CURRENT_RUN.update({"Running": False, "Set": "", "Time_Remaining_Sec": 0})
    log(f"[SET] Completed {set_name}")

def run_manual_command(command):
    log("[MANUAL] Manual run triggered")
    sets = command.get("manual_run", {}).get("sets", [])
    duration = command.get("manual_run", {}).get("duration_minutes", 1)
    schedule = load_json(SCHEDULE_FILE)

    for set_name in sets:
        matching = next((s for s in schedule.get("sets", []) if s["set_name"] == set_name), None)
        if matching and matching.get("mode", "scheduled") != "disabled":
            run_set(set_name, duration, source="MANUAL")

    if os.path.exists(MANUAL_COMMAND_FILE):
        os.remove(MANUAL_COMMAND_FILE)

# --- Flask Endpoint ---
@app.route("/status")
def status():
    return jsonify({
        "Log": declare_log[-100:],
        "Current_Run": CURRENT_RUN
    })

@app.route("/history-log")
def history_log():
    try:
        with open(HISTORY_LOG_FILE, "r") as f:
            return f.read(), 200, {'Content-Type': 'text/plain'}
    except Exception as e:
        return str(e), 500

# --- Main Controller ---
def main_loop():
    last_manual_mtime = 0

    if os.path.exists(MANUAL_COMMAND_FILE):
        try:
            os.remove(MANUAL_COMMAND_FILE)
            log("[SYSTEM] Cleared stale manual command file on startup")
        except Exception as e:
            log(f"[ERROR] Couldn't remove stale manual file: {e}")

    while True:
        now = datetime.now()
        current_time = now.strftime("%H:%M")
        schedule = load_json(SCHEDULE_FILE)

        if os.path.exists(MANUAL_COMMAND_FILE):
            manual_mtime = os.path.getmtime(MANUAL_COMMAND_FILE)
            if manual_mtime > last_manual_mtime:
                log("[SYSTEM] New manual command detected")
                try:
                    run_manual_command(load_json(MANUAL_COMMAND_FILE))
                    last_manual_mtime = manual_mtime
                except Exception as e:
                    log(f"[ERROR] Failed manual run: {e}")

        if should_run_today(schedule):
            log(f"[DEBUG] Checking time {current_time} against enabled times "
                f"{[e['time'] for e in schedule.get('start_times', []) if e.get('isEnabled')]}")

            if is_start_time_enabled(schedule, current_time):
                for s in schedule.get("sets", []):
                    if s.get("mode", "scheduled") == "scheduled":
                        run_set(s.get("set_name"), s.get("run_duration_minutes", 1), source="SCHEDULED")

        if get_mist_flags(schedule, current_time):
            mist_set = next((s for s in schedule.get("sets", []) if s["set_name"] == "Misters"), None)
            if mist_set and mist_set.get("mode", "scheduled") != "disabled":
                run_set(
            "Misters",
            schedule.get("mist", {}).get("duration_minutes", 1),
            source="SCHEDULED",
            pulse=schedule.get("mist", {}).get("pulse_duration_minutes"),
            soak=schedule.get("mist", {}).get("soak_duration_minutes")
        )


        time.sleep(1)

# --- LED Activity Indicator ---
def status_led_controller():
    led_on = False
    while True:
        if CURRENT_RUN.get("Running"):
            GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
            time.sleep(0.2)
            GPIO.output(STATUS_LED_PIN, GPIO.LOW)
            time.sleep(0.2)
        else:
            led_on = not led_on
            GPIO.output(STATUS_LED_PIN, GPIO.HIGH if led_on else GPIO.LOW)
            time.sleep(1.0)

# --- Entry Point ---
if __name__ == "__main__":
    initialize_gpio()
    threading.Thread(target=main_loop, daemon=True).start()
    threading.Thread(target=status_led_controller, daemon=True).start()
    app.run(host="0.0.0.0", port=5000)

