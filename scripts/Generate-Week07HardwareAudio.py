"""Original mono mechanical airlock cue. No external samples."""
import math
import random
import struct
import wave
from pathlib import Path

rate = 44100
duration = 2.8
rng = random.Random(707)
samples = []
for n in range(round(rate * duration)):
    t = n / rate
    noise = rng.uniform(-1, 1)
    charge = 0.09 * math.sin(2 * math.pi * (180 * t + 110 * t * t)) * max(0, 1 - t / .55)
    latch = sum(.19 * noise * math.exp(-max(0, t - start) * 45) if t >= start else 0
                for start in (.45, .65, 2.64))
    motor_phase = max(0, min(1, (t - .85) / 1.8))
    envelope = math.sin(math.pi * motor_phase) ** .6
    motor = envelope * (.055 * math.sin(2 * math.pi * 74 * t) +
                        .04 * math.sin(2 * math.pi * 147 * t) + .038 * noise)
    samples.append(max(-.8, min(.8, charge + latch + motor)))
path = Path(__file__).resolve().parents[1] / "Assets/07_Sounds/SFX/hatch_mechanism.wav"
path.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(path), "wb") as out:
    out.setnchannels(1)
    out.setsampwidth(2)
    out.setframerate(rate)
    out.writeframes(b"".join(struct.pack("<h", round(s * 32767)) for s in samples))
print(f"{path.name}: {duration}s, mono, {rate} Hz")
