"""Capture bounded local speaker-output evidence, never microphone input.

Requires SoundCard, CFFI and NumPy in the acceptance Python environment.
SoundCard WASAPI reference: https://soundcard.readthedocs.io/en/latest/
System loopback includes other applications; correlate with game mute controls.
This establishes digital output, not physical audibility or listening quality.
"""

import argparse
import datetime
import hashlib
import json
from pathlib import Path
import sys
import wave


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dependencies", type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--seconds", type=int, default=5, choices=range(1, 16))
    args = parser.parse_args()
    if args.dependencies:
        sys.path.insert(0, str(args.dependencies.resolve()))
    import numpy as np
    import soundcard as sc

    output = args.output.resolve()
    if output.suffix.lower() != ".wav":
        parser.error("output must end in .wav")
    metadata = output.with_suffix(".json")
    if output.exists() or metadata.exists():
        parser.error("choose a fresh output identity; existing evidence is preserved")
    speaker = sc.default_speaker()
    if speaker is None:
        raise RuntimeError("No default speaker is available")
    loopback = sc.get_microphone(id=speaker.id, include_loopback=True)
    if not loopback.isloopback:
        raise RuntimeError("Refusing non-loopback recording")
    rate = 48000
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    print("Capturing speaker loopback; no microphone input", flush=True)
    # Keep both output channels: single-channel WASAPI recording is unreliable.
    with loopback.recorder(samplerate=rate, channels=speaker.channels) as recorder:
        data = recorder.record(numframes=rate * args.seconds)
    output.parent.mkdir(parents=True, exist_ok=True)
    pcm = (np.clip(data, -1.0, 1.0) * 32767).astype("<i2")
    # Exclusive creation guards the evidence even if a second capture races us.
    with output.open("xb") as stream:
        with wave.open(stream, "wb") as wav:
            wav.setnchannels(data.shape[1])
            wav.setsampwidth(2)
            wav.setframerate(rate)
            wav.writeframes(pcm.tobytes())
    report = {
        "startedUtc": started,
        "source": "default-speaker-loopback",
        "sampleRate": rate,
        "frames": len(data),
        "channels": data.shape[1],
        "peak": float(np.max(np.abs(data))),
        "rms": float(np.sqrt(np.mean(np.square(data.astype(np.float64))))),
        "fractionAbove1e-5": float(np.mean(np.abs(data) > 1e-5)),
        "wavSha256": hashlib.sha256(output.read_bytes()).hexdigest(),
        "scope": "System digital output; may include other apps. Not a physical listening judgment.",
    }
    with metadata.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
    print(json.dumps(report))


if __name__ == "__main__":
    main()
