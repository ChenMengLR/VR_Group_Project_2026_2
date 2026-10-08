"""Generate the original mono sound assets for the solo station's Week 05 scene.

Only Python's standard library is used. All tones are synthesized here; no
third-party recording, sample, or course attachment is redistributed.
"""

from __future__ import annotations

import math
import random
import wave
from pathlib import Path


SAMPLE_RATE = 44_100
ROOT = Path(__file__).resolve().parents[1] / "Assets" / "07_Sounds"
TAU = 2 * math.pi


def sine(hz: float, time: float, phase: float = 0.0) -> float:
    return math.sin(TAU * hz * time + phase)


def write_mono(name: str, seconds: float, synth) -> None:
    target = ROOT / name
    target.parent.mkdir(parents=True, exist_ok=True)
    count = round(seconds * SAMPLE_RATE)
    rng = random.Random(name)
    samples = bytearray()
    for index in range(count):
        time = index / SAMPLE_RATE
        value = max(-0.95, min(0.95, synth(time, index, count, rng)))
        samples.extend(round(value * 32767).to_bytes(2, "little", signed=True))
    with wave.open(str(target), "wb") as audio:
        audio.setnchannels(1)
        audio.setsampwidth(2)
        audio.setframerate(SAMPLE_RATE)
        audio.writeframes(samples)
    print(f"{target.relative_to(ROOT.parent.parent)}: {seconds:.2f}s, 44.1 kHz mono PCM")


# Every ambient frequency below completes an integer number of cycles in 8 s.
# The loop joins smoothly while its slow modulation suggests ventilation.
AMBIENT_PARTIALS = tuple(
    (hz, phase, level)
    for hz, phase, level in (
        (56.0, 0.1, 0.068),
        (84.0, 1.2, 0.032),
        (112.0, 2.4, 0.020),
        (168.0, 0.7, 0.013),
        (253.0, 1.7, 0.009),
        (371.0, 0.3, 0.007),
        (512.0, 2.0, 0.005),
        (733.0, 1.0, 0.004),
        (997.0, 2.8, 0.003),
    )
)


def airflow(time, _index, _count, _rng):
    swirl = 0.86 + 0.14 * sine(0.25, time, 0.4)
    return swirl * sum(level * sine(hz, time, phase) for hz, phase, level in AMBIENT_PARTIALS)


def grab(time, _index, _count, rng):
    metal = (0.35 * sine(740, time) + 0.17 * sine(1320, time, 0.3)
             + 0.12 * sine(2310, time, 1.1)) * math.exp(-26 * time)
    click = 0.12 * (2 * rng.random() - 1) * math.exp(-310 * time)
    return metal + click


def release(time, _index, _count, rng):
    thunk = (0.48 * sine(205, time) + 0.16 * sine(390, time, 0.5)) * math.exp(-18 * time)
    contact = 0.11 * (2 * rng.random() - 1) * math.exp(-240 * time)
    return thunk + contact


def power_restored(time, _index, _count, _rng):
    notes = ((0.00, 523.25), (0.12, 659.25), (0.24, 783.99))
    value = 0.0
    for start, hz in notes:
        if time >= start:
            age = time - start
            value += 0.17 * math.exp(-5.2 * age) * (sine(hz, age) + 0.22 * sine(2 * hz, age))
    return value * min(1.0, (0.62 - time) * 22)


def mission_complete(time, _index, _count, _rng):
    swell = min(1.0, time * 28) * min(1.0, (0.72 - time) * 12)
    return swell * math.exp(-1.6 * time) * (
        0.13 * sine(523.25, time) + 0.13 * sine(659.25, time)
        + 0.13 * sine(783.99, time) + 0.06 * sine(1046.5, time)
    )


def main() -> None:
    write_mono("AMBIENT/airflow_loop_8s.wav", 8.0, airflow)
    write_mono("SFX/grab_core.wav", 0.22, grab)
    write_mono("SFX/release_core.wav", 0.28, release)
    write_mono("SFX/power_restored.wav", 0.62, power_restored)
    write_mono("SFX/mission_complete.wav", 0.72, mission_complete)


if __name__ == "__main__":
    main()
