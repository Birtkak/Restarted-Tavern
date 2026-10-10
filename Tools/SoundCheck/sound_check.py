"""Check the game's synthesized sounds by eye and by numbers (made so an AI that can't hear can review them too).

Export the sounds first (Unity: Restarted Tavern > Export Sound Preview, or headless:
-executeMethod RestartedTavern.Client.Editor.SfxPreview.Export), then:
    python Tools/SoundCheck/sound_check.py [out_dir]
Writes <out_dir>/sound_sheet.png (a spectrogram per sound on a log-frequency axis, 50 Hz at the bottom to 12 kHz at the
top; the yellow line is the loudness envelope; kinds across, factions down) and prints a table: length, peak, loudness
(RMS dB), pitch (Hz), brightness (spectral centroid, Hz; flagged over 3.5 kHz: smooth sounds sit lower), harshness (share of energy above 5 kHz), attack time (ms), the largest
sample-to-sample jump (clicks) and the level of the last 10 ms (a cut-off tail). Problems are flagged. Needs numpy, Pillow.
"""
import glob
import os
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SFX = os.path.join(ROOT, "Builds", "sfx")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Builds")
KINDS = ["Creature", "Instant", "Sorcery", "Equipment", "Relic", "Curse", "Ability", "Power", "Death", "Trigger"]
FACTIONS = ["goobers", "evergrowing_wild", "glitterworld", "sensationalists", "shadow_money_wizards", "neutral"]
SHARED = ["Pass", "RoundStart", "Attack", "Block", "Equip", "Countered"]


def load(path):
    with wave.open(path) as w:
        sr = w.getframerate()
        x = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64) / 32768
    return x, sr


def measure(x, sr):
    peak = float(np.max(np.abs(x)))
    rms = float(np.sqrt(np.mean(x ** 2))) + 1e-9
    spec = np.abs(np.fft.rfft(x * np.hanning(len(x))))
    freqs = np.fft.rfftfreq(len(x), 1 / sr)
    centroid = float(np.sum(freqs * spec) / (np.sum(spec) + 1e-9))
    harsh = float(np.sum(spec[freqs > 5000] ** 2) / (np.sum(spec ** 2) + 1e-9))
    # Pitch: autocorrelation over the loudest 60 ms.
    i = int(np.argmax(np.convolve(x ** 2, np.ones(512), "same")))
    seg = x[max(0, i - int(0.03 * sr)): i + int(0.03 * sr)]
    seg = seg - seg.mean()
    ac = np.correlate(seg, seg, "full")[len(seg) - 1:]
    lo, hi = int(sr / 2000), int(sr / 50)
    pitch = 0.0
    if len(ac) > hi and ac[0] > 0:
        lag = lo + int(np.argmax(ac[lo:hi]))
        if ac[lag] > 0.3 * ac[0]:
            pitch = sr / lag
    jump = float(np.max(np.abs(np.diff(x))))
    # Attack: ms from 10% to 90% of the first envelope peak (smooth sounds swell in; arcade blips snap on).
    env = np.convolve(np.abs(x), np.ones(64) / 64, "same")
    first = env[: int(0.25 * sr)]
    pk = float(first.max()) if len(first) else 0.0
    a10 = int(np.argmax(first >= 0.1 * pk)) if pk > 0 else 0
    a90 = int(np.argmax(first >= 0.9 * pk)) if pk > 0 else 0
    attack_ms = 1000 * max(0, a90 - a10) / sr
    tail = float(np.max(np.abs(x[-int(0.01 * sr):])))
    return dict(len=len(x) / sr, peak=peak, rms_db=20 * np.log10(rms), pitch=pitch, bright=centroid, harsh=harsh, jump=jump, tail=tail, attack=attack_ms)


def problems(m):
    checks = [("clips", m["peak"] > 0.99), ("click?", m["jump"] > 0.5), ("cut tail", m["tail"] > 0.08),
              ("harsh", m["harsh"] > 0.15), ("quiet", m["rms_db"] < -30), ("bright", m["bright"] > 3500)]
    return [name for name, bad in checks if bad]


def spectrogram(x, sr, w, h):
    n = 1024
    hop = max(1, (len(x) - n) // w)
    cols = []
    for k in range(w):
        seg = x[k * hop: k * hop + n]
        if len(seg) < n:
            seg = np.pad(seg, (0, n - len(seg)))
        cols.append(np.abs(np.fft.rfft(seg * np.hanning(n))))
    s = np.array(cols).T
    freqs = np.fft.rfftfreq(n, 1 / sr)
    rows = np.geomspace(50, 12000, h)
    idx = np.searchsorted(freqs, rows).clip(0, len(freqs) - 1)
    s = 20 * np.log10(s[idx] + 1e-6)
    s = np.clip((s + 60) / 60, 0, 1)[::-1]
    rgb = np.stack([s * 255, s * 120 + 40 * (1 - s), (1 - s) * 90], -1).astype(np.uint8)
    img = Image.fromarray(rgb)
    d = ImageDraw.Draw(img)
    env = []
    for k in range(w):
        a, b = k * len(x) // w, (k + 1) * len(x) // w
        env.append(float(np.max(np.abs(x[a:b]))) if b > a else 0.0)
    d.line([(k, h - 1 - e * (h - 2)) for k, e in enumerate(env)], fill=(255, 230, 80), width=1)
    return img


def main():
    files = {os.path.basename(p)[:-4]: p for p in glob.glob(os.path.join(SFX, "*.wav"))}
    if not files:
        sys.exit("No sounds in Builds/sfx: run SfxPreview.Export first.")
    cw, ch, lw, top = 230, 140, 190, 30
    sheet = Image.new("RGB", (lw + cw * len(KINDS), top + ch * (len(FACTIONS) + 1)), (14, 16, 24))
    d = ImageDraw.Draw(sheet)
    for c, k in enumerate(KINDS):
        d.text((lw + c * cw + 6, 8), k, fill=(255, 210, 120))
    print("%-34s %5s %5s %6s %6s %6s %6s %6s %6s %6s" % ("sound", "len", "peak", "rmsdB", "pitch", "bright", "harsh", "jump", "tail", "atkms"))
    rows = [(f, KINDS) for f in FACTIONS] + [("shared", SHARED)]
    for r, (f, kinds) in enumerate(rows):
        d.text((6, top + r * ch + ch // 2), f, fill=(230, 230, 230))
        for c, k in enumerate(kinds):
            name = k + "_" + ("neutral" if f == "shared" else f)
            if name not in files:
                continue
            x, sr = load(files[name])
            m = measure(x, sr)
            bad = problems(m)
            print("%-34s %5.2f %5.2f %6.1f %6.0f %6.0f %6.3f %6.3f %6.3f %6.1f%s" % (
                name, m["len"], m["peak"], m["rms_db"], m["pitch"], m["bright"], m["harsh"], m["jump"], m["tail"], m["attack"],
                "  <-- " + ", ".join(bad) if bad else ""))
            sheet.paste(spectrogram(x, sr, cw - 10, ch - 26), (lw + c * cw, top + r * ch))
            d.text((lw + c * cw + 4, top + r * ch + ch - 24), "%.0fHz %.0fdB b%.0f" % (m["pitch"], m["rms_db"], m["bright"]), fill=(200, 200, 200))
    out = os.path.join(OUT, "sound_sheet.png")
    sheet.save(out)
    print("sheet:", out)


if __name__ == "__main__":
    main()
