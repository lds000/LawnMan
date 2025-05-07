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

app = Flask(__name__)

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

declare_log = []
CURRENT_RUN = {
    "Running": False,
    "Set": "",
    "Time_Remaining_Sec": 0,
    "Phase": ""
}

def initialize_gpio():
    print("[SYSTEM] Controller starting up...")
    GPIO.setmode(GPIO.BCM)
    for name, pin in RELAY_PINS.items():
        GPIO.setup(pin, GPIO.OUT)
        GPIO.output(pin, GPIO.LOW)
    GPIO.setup(STATUS_LED_PIN, GPIO.OUT)
    GPIO.output(STATUS_LED_PIN, GPIO.LOW)
    startup_blink()

def startup_blink():
    for _ in range(3):
        GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
        time.sleep(0.15)
        GPIO.output(STATUS_LED_PIN, GPIO.LOW)
        time.sleep(0.15)

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

def load_json(path):
    log(f"[LOAD] Loading JSON from {path}")
    with open(path, 'r') as f:
        data = json.load(f)

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

def turn_on(pin):
    GPIO.output(pin, GPIO.HIGH)
    GPIO.output(STATUS_LED_PIN, GPIO.HIGH)

def turn_off(pin):
    GPIO.output(pin, GPIO.LOW)
    GPIO.output(STATUS_LED_PIN, GPIO.LOW)

def run_set(set_name, duration_minutes, source="SCHEDULED", pulse=None, soak=None):
    pin = RELAY_PINS.get(set_name)
    if pin is None:
        log(f"[ERROR] Unknown set name: {set_name}")
        return

    log(f"[SET] Running {set_name} for {duration_minutes} min ({source})")
    start_time = datetime.now()
    total = duration_minutes * 60
    elapsed = 0
    CURRENT_RUN.update({"Running": True, "Set": set_name, "Phase": "Watering"})

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
                for _ in range(soak * 60):
                    if elapsed >= total:
                        break
                    time.sleep(1)
                    elapsed += 1
                    CURRENT_RUN["Time_Remaining_Sec"] = total - elapsed
    else:
        turn_on(pin)
        for i in range(total):
            time.sleep(1)
            CURRENT_RUN.update({
                "Running": True,
                "Set": set_name,
                "Time_Remaining_Sec": total - i,
                "Phase": "Watering"
            })
        turn_off(pin)

    turn_off(pin)
    end_time = datetime.now()
    CURRENT_RUN.update({"Running": False, "Set": "", "Time_Remaining_Sec": 0, "Phase": ""})
    log_watering_history(set_name, start_time, end_time, source)
    log(f"[SET] Completed {set_name}")

def run_manual_command(command):
    log("[MANUAL] Manual run triggered")
    sets = command.get("manual_run", {}).get("sets", [])
    duration = command.get("manual_run", {}).get("duration_minutes", 1)
    schedule = load_json(SCHEDULE_FILE)

    for set_name in sets:
        match = next((s for s in schedule.get("sets", []) if s["set_name"] == set_name), None)
        if match and match.get("mode", "scheduled") != "disabled":
            pulse = match.get("pulse_duration_minutes")
            soak = match.get("soak_duration_minutes")

            threading.Thread(
            target=run_set,
            args=(set_name, duration),
            kwargs={"source": "MANUAL", "pulse": pulse, "soak": soak},
            daemon=True
            ).start()



    if os.path.exists(MANUAL_COMMAND_FILE):
        os.remove(MANUAL_COMMAND_FILE)

@app.route("/status")
def status():
    return jsonify({
        "Log": declare_log[-100:],
        "Current_Run": CURRENT_RUN
    })

@app.route("/history-log")
def history_log():
    try:
        with open(HISTORY_LOG_FILE) as f:
            return f.read(), 200, {'Content-Type': 'text/plain'}
    except Exception as e:
        return str(e), 500

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
            mtime = os.path.getmtime(MANUAL_COMMAND_FILE)
            if mtime > last_manual_mtime:
                try:
                    run_manual_command(load_json(MANUAL_COMMAND_FILE))
                    last_manual_mtime = mtime
                except Exception as e:
                    log(f"[ERROR] Manual run failed: {e}")

        if should_run_today(schedule):
            log(f"[DEBUG] Checking time {current_time} against enabled times "
                f"{[e['time'] for e in schedule.get('start_times', []) if e.get('isEnabled')]}")
            if is_start_time_enabled(schedule, current_time):
                for s in schedule.get("sets", []):
                    if s.get("mode", "scheduled") == "scheduled":
                        run_set(s["set_name"], s.get("run_duration_minutes", 1),
                                pulse=s.get("pulse_duration_minutes"),
                                soak=s.get("soak_duration_minutes"))

        if get_mist_flags(schedule, current_time):
            mist = next((s for s in schedule.get("sets", []) if s["set_name"] == "Misters"), None)
            if mist and mist.get("mode", "scheduled") != "disabled":
                run_set("Misters",
                        schedule.get("mist", {}).get("duration_minutes", 1),
                        pulse=schedule.get("mist", {}).get("pulse_duration_minutes"),
                        soak=schedule.get("mist", {}).get("soak_duration_minutes"))

        time.sleep(1)

def status_led_controller():
    led_on = False
    while True:
        if CURRENT_RUN["Running"]:
            GPIO.output(STATUS_LED_PIN, GPIO.HIGH)
            time.sleep(0.2)
            GPIO.output(STATUS_LED_PIN, GPIO.LOW)
            time.sleep(0.2)
        else:
            led_on = not led_on
            GPIO.output(STATUS_LED_PIN, GPIO.HIGH if led_on else GPIO.LOW)
            time.sleep(1)

if __name__ == "__main__":
    initialize_gpio()
    threading.Thread(target=main_loop, daemon=True).start()
    threading.Thread(target=status_led_controller, daemon=True).start()
    app.run(host="0.0.0.0", port=5000)

