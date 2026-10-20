# Station sound assets

The original five Week 05 WAV files in `AMBIENT/` and `SFX/` are deterministic PCM audio
synthesized by `scripts/Generate-Week05Audio.py` with Python's standard library.
They contain no third-party recordings, course attachments, or licensed samples.
They may be used and modified with this course project. The script regenerates
the same five files at 44.1 kHz, 16-bit, mono.

| File | Scene use |
| --- | --- |
| `AMBIENT/airflow_loop_8s.wav` | Eight-second seamless socket ventilation loop |
| `SFX/grab_core.wav` | Energy core selected / grabbed |
| `SFX/release_core.wav` | Energy core unselected / released |
| `SFX/power_restored.wav` | Core accepted by socket and hatch powered |
| `SFX/mission_complete.wav` | Player reaches the exit |

The Week 05 course page also provides example grab and release WAVs. This
project uses its own sounds so the complete source and usage rights are clear.

Week 07 adds `SFX/hatch_mechanism.wav`: an original 2.8-second mono mechanism cue
at 44.1 kHz / 16 bit. Rebuild it with
`python scripts/Generate-Week07HardwareAudio.py`. Charge, lock clicks and a servo
slide accompany the 2.65-second opening sequence. This clip contains no third-party
samples. The hatch's AudioSource is spatialized; perceived direction, loudness and
synchronization on Quest still require a headset test.
