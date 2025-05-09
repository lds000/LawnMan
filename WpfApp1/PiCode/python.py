

================================================================================
📄 Make_AI_Summary.py
================================================================================

import os

output_filename = "combined_sprinkler_snapshot.txt"

with open(output_filename, "w") as outfile:
    for filename in sorted(os.listdir(".")):
        if filename.endswith(".py") and filename != output_filename:
            outfile.write(f"\n\n{'='*80}\n")
            outfile.write(f"📄 {filename}\n")
            outfile.write(f"{'='*80}\n\n")
            with open(filename, "r") as f:
                outfile.write(f.read())

print(f"✅ Combined all .py files into: {output_filename}")


================================================================================
📄 SetEnvVar.py
================================================================================

import os
os.environ["TEST_MODE"] = "0"
os.environ["DEBUG_VERBOSE"] = "0"


================================================================================
📄 flask_api.py
================================================================================

### flask_api.py

from flask import Flask, jsonify
from status import CURRENT_RUN
from gpio_controller import is_test_mode  # ✅ read from file-based check

app = Flask(__name__)
declare_log = []

@app.route("/status")
def status():
    return jsonify({
        "Log": declare_log[-100:],
        "Current_Run": CURRENT_RUN,
        "TestMode": is_test_mode()  # ✅ dynamic read
    })

@app.route("/history-log")
def history_log():
    try:
        with open("/home/lds00/watering_history.log", "r") as f:
            return f.read(), 200, {'Content-Type': 'text/plain'}
    except Exception as e:
        return str(e), 500


================================================================================
📄 gpio_controller.py
================================================================================

### gpio_controller.py

import RPi.GPIO as GPIO
import time
from datetime import datetime
from logger import log

STATUS_LED_PIN = 5
STATUS_LOG = "/home/lds00/status_test_mode.log"
TEST_MODE_FILE = "/home/lds00/sprinkler/test_mode.txt"

_last_states = {}  # Track relay states to suppress duplicate logs

def is_test_mode():
    try:
        with open(TEST_MODE_FILE) as f:
            return f.read().strip() == "1"
    except Exception:
        return False

def initialize_gpio(RELAYS):
    log("[SYSTEM] Controller starting up...")

    if is_test_mode():
        log("[TEST MODE ENABLED] GPIO commands will be logged, not executed.")
    else:
        GPIO.setmode(GPIO.BCM)
        for name, pin in RELAYS.items():
            GPIO.setup(pin, GPIO.OUT)
            GPIO.output(pin, GPIO.LOW)
        GPIO.setup(STATUS_LED_PIN, GPIO.OUT)
        GPIO.output(STATUS_LED_PIN, GPIO.LOW)
        startup_blink()

def startup_blink():
    if is_test_mode():
        log("[TEST] Simulated startup blink")
        return

    for _ in range(3):
        GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
        time.sleep(0.15)
        GPIO.output(STATUS_LED_PIN, GPIO.LOW)
        time.sleep(0.15)

def turn_on(pin, name=None):
    label = name or f"PIN_{pin}"
    if is_test_mode():
        if _last_states.get(pin) != "ON":
            log(f"[TEST MODE] {label} ON")
        _last_states[pin] = "ON"
    else:
        GPIO.output(pin, GPIO.HIGH)
        GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
        _last_states[pin] = "ON"

def turn_off(pin, name=None):
    label = name or f"PIN_{pin}"
    if is_test_mode():
        if _last_states.get(pin) != "OFF":
            log(f"[TEST MODE] {label} OFF")
        _last_states[pin] = "OFF"
    else:
        GPIO.output(pin, GPIO.LOW)
        GPIO.output(STATUS_LED_PIN, GPIO.LOW)
        _last_states[pin] = "OFF"

def status_led_controller(CURRENT_RUN):
    if is_test_mode():
        return

    while True:
        if CURRENT_RUN["Running"]:
            GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
            time.sleep(0.2)
            GPIO.output(STATUS_LED_PIN, GPIO.LOW)
            time.sleep(0.2)
        else:
            GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
            time.sleep(1)
            GPIO.output(STATUS_LED_PIN, GPIO.LOW)
            time.sleep(1)


================================================================================
📄 logger.py
================================================================================

### logger.py

from datetime import datetime

LOG_FILE = "/home/lds00/sprinkler_status.log"
declare_log = []

def log(message):
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    entry = f"[{now}] {message}"
    print(entry, flush=True)
    declare_log.append(entry)
    with open(LOG_FILE, "a") as f:
        f.write(entry + "\n")

================================================================================
📄 main.py
================================================================================

### main.py

import os
import time
import threading
from datetime import datetime
from scheduler import load_json, should_run_today, is_start_time_enabled, get_mist_flags
from gpio_controller import initialize_gpio, status_led_controller, turn_off
from flask_api import app
from run_manager import run_set
from status import CURRENT_RUN
from logger import log
import logging

# Set werkzeug (Flask) logging level
if os.getenv("DEBUG_VERBOSE", "0") != "1":
    logging.getLogger('werkzeug').setLevel(logging.ERROR)

RELAYS = {
    "Hanging Pots": 17,
    "Garden": 27,
    "Misters": 22
}

SCHEDULE_FILE = "/home/lds00/sprinkler_schedule.json"
MANUAL_COMMAND_FILE = "/home/lds00/manual_command.json"
LOG_FILE = "/home/lds00/watering_history.log"
TEST_MODE_FILE = "/home/lds00/sprinkler/test_mode.txt"

DEBUG_VERBOSE = os.getenv("DEBUG_VERBOSE", "0") == "1"
_last_test_mode = None  # for change detection

# 🔡️ Safety: turn off all relays on startup to ensure known state
def ensure_all_relays_off():
    for name, pin in RELAYS.items():
        try:
            turn_off(pin, name=name)
        except Exception as e:
            log(f"[WARN] Could not turn off pin {pin} at startup: {e}")

def run_manual_command(command, schedule):
    log("[MANUAL] Manual run triggered")
    sets = command.get("manual_run", {}).get("sets", [])
    duration = command.get("manual_run", {}).get("duration_minutes", 1)

    for set_name in sets:
        match = next((s for s in schedule.get("sets", []) if s["set_name"] == set_name), None)
        if match:
            log(f"[MANUAL] Starting {set_name} for {duration} min")
            threading.Thread(
                target=run_set,
                args=(set_name, duration, RELAYS, LOG_FILE),
                kwargs={"source": "MANUAL",
                        "pulse": match.get("pulse_duration_minutes"),
                        "soak": match.get("soak_duration_minutes")},
                daemon=True
            ).start()

def read_test_mode_from_file():
    try:
        with open(TEST_MODE_FILE) as f:
            return f.read().strip() == "1"
    except Exception:
        return False

def main_loop():
    global _last_test_mode
    log("[DEBUG] main_loop has started")
    last_manual_mtime = 0
    _last_test_mode = read_test_mode_from_file()
    log(f"[INFO] TEST_MODE = {_last_test_mode}")

    while True:
        now = datetime.now()
        current_time = now.strftime("%H:%M")
        schedule = load_json(SCHEDULE_FILE)

        if DEBUG_VERBOSE:
            log(f"[TICK] Checking manual run + time = {current_time}")

        if os.path.exists(MANUAL_COMMAND_FILE):
            mtime = os.path.getmtime(MANUAL_COMMAND_FILE)
            if mtime > last_manual_mtime:
                if DEBUG_VERBOSE:
                    log("[CHECK] Manual command file changed — executing run_manual_command()")
                try:
                    data = load_json(MANUAL_COMMAND_FILE)
                    run_manual_command(data, schedule)
                except Exception as e:
                    log(f"[ERROR] Failed to parse or execute manual command: {e}")
                finally:
                    try:
                        os.remove(MANUAL_COMMAND_FILE)
                    except Exception as e:
                        log(f"[WARN] Could not delete manual command file: {e}")
                last_manual_mtime = mtime
        elif DEBUG_VERBOSE:
            log("[CHECK] No manual command file found")

        if should_run_today(schedule):
            if DEBUG_VERBOSE:
                log(f"[DEBUG] Today is active. Checking time {current_time} against schedule...")
            if is_start_time_enabled(schedule, current_time):
                for s in schedule.get("sets", []):
                    if s.get("mode", "scheduled") == "scheduled":
                        log(f"[SCHEDULED] Launching set {s['set_name']} at {current_time}")
                        threading.Thread(
                            target=run_set,
                            args=(s["set_name"], s.get("run_duration_minutes", 1), RELAYS, LOG_FILE),
                            kwargs={"pulse": s.get("pulse_duration_minutes"),
                                    "soak": s.get("soak_duration_minutes")},
                            daemon=True
                        ).start()

        if get_mist_flags(schedule, current_time):
            mist = next((s for s in schedule.get("sets", []) if s["set_name"] == "Misters"), None)
            if mist and mist.get("mode", "scheduled") != "disabled":
                log("[MIST] Triggering mist set")
                threading.Thread(
                    target=run_set,
                    args=("Misters", schedule["mist"]["duration_minutes"], RELAYS, LOG_FILE),
                    kwargs={"pulse": schedule["mist"].get("pulse_duration_minutes"),
                            "soak": schedule["mist"].get("soak_duration_minutes")},
                    daemon=True
                ).start()

        # 🔁 Monitor test mode status from file
        current_test_mode = read_test_mode_from_file()
        if current_test_mode != _last_test_mode:
            log(f"[INFO] TEST_MODE changed to {current_test_mode}")
            _last_test_mode = current_test_mode

        time.sleep(1)

if __name__ == "__main__":
    initialize_gpio(RELAYS)
    ensure_all_relays_off()  # 🔡 Kill switch for safety
    threading.Thread(target=main_loop, daemon=True).start()
    threading.Thread(target=status_led_controller, args=(CURRENT_RUN,), daemon=True).start()
    app.run(host="0.0.0.0", port=5000)


================================================================================
📄 run_manager.py
================================================================================

### run_manager.py

import time
from datetime import datetime
from gpio_controller import turn_on, turn_off
from status import CURRENT_RUN
from logger import log

def log_watering_history(log_file, set_name, start_dt, end_dt, source="SCHEDULED"):
    entry = f"{start_dt.date()} {set_name} {source.upper()} START: {start_dt.strftime('%H:%M:%S')} STOP: {end_dt.strftime('%H:%M:%S')}\n"
    with open(log_file, "a") as f:
        f.write(entry)

def run_set(set_name, duration_minutes, RELAYS, log_file, source="SCHEDULED", pulse=None, soak=None):
    pin = RELAYS.get(set_name)
    if pin is None:
        log(f"[ERROR] Unknown set name: {set_name}")
        return

    log(f"[SET] Running {set_name} for {duration_minutes} min ({source})")
    start_time = datetime.now()
    total = duration_minutes * 60
    elapsed = 0
    CURRENT_RUN.update({
        "Running": True,
        "Set": set_name,
        "Phase": "Watering",
        "Time_Remaining_Sec": total,
        "Soak_Remaining_Sec": 0
    })

    if pulse and soak:
        while elapsed < total:
            CURRENT_RUN["Phase"] = "Watering"
            turn_on(pin)
            for _ in range(pulse * 60):
                if elapsed >= total:
                    break
                time.sleep(1)
                elapsed += 1
                CURRENT_RUN["Time_Remaining_Sec"] = total - elapsed
            turn_off(pin)
            if elapsed < total:
                CURRENT_RUN["Phase"] = "Soaking"
                CURRENT_RUN["Soak_Remaining_Sec"] = soak * 60
                for i in range(soak * 60):
                    time.sleep(1)
                    CURRENT_RUN["Soak_Remaining_Sec"] = soak * 60 - (i + 1)
    else:
        turn_on(pin)
        for i in range(total):
            time.sleep(1)
            CURRENT_RUN.update({
                "Running": True,
                "Set": set_name,
                "Time_Remaining_Sec": total - i,
                "Phase": "Watering",
                "Soak_Remaining_Sec": 0
            })
        turn_off(pin)

    turn_off(pin)
    end_time = datetime.now()
    CURRENT_RUN.update({
        "Running": False,
        "Set": "",
        "Time_Remaining_Sec": 0,
        "Soak_Remaining_Sec": 0,
        "Phase": ""
    })
    log_watering_history(log_file, set_name, start_time, end_time, source)
    log(f"[SET] Completed {set_name}")


================================================================================
📄 scheduler.py
================================================================================

from datetime import datetime
import json

def load_json(path):
    with open(path, 'r') as f:
        return json.load(f)

def get_schedule_day_index():
    base = datetime(2024, 1, 1)
    return (datetime.now().date() - base.date()).days % 14

def should_run_today(schedule):
    idx = get_schedule_day_index()
    return schedule.get("schedule_days", [False] * 14)[idx]

def is_start_time_enabled(schedule, time_str):
    return any(entry.get("time") == time_str and entry.get("isEnabled", False)
               for entry in schedule.get("start_times", []))

def get_mist_flags(schedule, time_str):
    mist = schedule.get("mist", {})
    return mist.get(f"time_{time_str.replace(':', '')}", False)


================================================================================
📄 status.py
================================================================================

import os

CURRENT_RUN = {
    "Running": False,
    "Set": "",
    "Time_Remaining_Sec": 0,
    "Soak_Remaining_Sec": 0,
    "Phase": ""
}
