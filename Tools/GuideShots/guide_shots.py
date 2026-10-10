"""Makes the Tavern Guide's screenshots (Assets/Resources/Guide) from the built game.

Build the exe first (Restarted Tavern > Build Windows, or TableBuilder.BuildWindows), then:
    python Tools/GuideShots/guide_shots.py            # all shots
    python Tools/GuideShots/guide_shots.py block1 gold # some shots
Each shot runs the exe with scene flags (-tutorialstep, -place, -stage, -act, ... see CLIENT_DESIGN §2.2),
takes a 1920x1080 screenshot, crops it to the box below and saves a JPEG. Needs Pillow.
"""
import os, subprocess, sys, tempfile
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
EXE = os.path.join(ROOT, "Builds", "Table", "RestartedTavern.exe")
OUT = os.path.join(ROOT, "Assets", "Resources", "Guide")
FULL = (0, 0, 1920, 1080)

# name: (flags, crop box (left, top, right, bottom) in the 1920x1080 shot)
TUT = ["-nocoach", "-tutorialstep"]
SHOTS = {
    "table":    (TUT + ["1"], FULL),
    "coin":     (["-tutorialstep", "0", "-shotat", "2.3"], (360, 0, 1560, 1080)),
    "mulligan": (["-seed", "7", "-settle"], FULL),
    "dweller":  (TUT + ["21", "-zoom", "dweller"], FULL),
    "round":    (TUT + ["8"], FULL),
    "context":  (TUT + ["7"], (1280, 340, 1920, 1080)),
    "mana":     (TUT + ["8"], (1250, 600, 1920, 1040)),
    "gold":     (TUT + ["8", "-gold", "2"], (1300, 330, 1920, 680)),
    "hover":    (TUT + ["3", "-hover", "goober_rascal"], (300, 380, 1620, 1080)),
    "play":     (TUT + ["4"], FULL),
    "attack1":  (TUT + ["5", "-settle"], FULL),
    "attack2":  (TUT + ["5", "-settle", "-stage"], FULL),
    "attack3":  (TUT + ["6"], FULL),
    "block1":   (TUT + ["11", "-settle"], FULL),
    "block2":   (TUT + ["11", "-settle", "-stage"], FULL),
    "block3":   (TUT + ["13"], FULL),
    "phases":   (TUT + ["11", "-settle"], (1100, 300, 1920, 760)),
    "target":   (TUT + ["16", "-autopick"], FULL),
    "chain":    (TUT + ["16", "-act", "spark_snot"], FULL),
    "spider":   (TUT + ["10", "-zoom", "vine_spider"], FULL),
    "boar":     (TUT + ["19", "-zoom", "razorhide_boar"], FULL),
    "wide":     (TUT + ["20", "-stage"], FULL),
    "equip":    (TUT + ["4", "-place", "hover_tank,pulse_blade,marksman_scope"], (680, 540, 1240, 855)),
    "equip2":   (TUT + ["4", "-place", "hover_tank,pulse_blade,marksman_scope", "-zoom", "hover_tank"], FULL),
    "curse":    (TUT + ["4", "-place", "ironbark_grizzly", "-placeopp", "hex_of_withering"], (680, 540, 1240, 855)),
    "curse2":   (TUT + ["4", "-place", "ironbark_grizzly", "-placeopp", "hex_of_withering", "-zoom", "ironbark_grizzly"], FULL),
    "editor":   (["-editor"], FULL),
    "settings": (["-settings", "-seed", "7", "-autoplay", "4"], (480, 200, 1440, 880)),
    "bug":      (["-bugopen", "-seed", "7", "-autoplay", "4"], (560, 280, 1360, 660)),
}


def shoot(name):
    flags, box = SHOTS[name]
    raw = os.path.join(tempfile.gettempdir(), "guide_" + name + ".png")
    if os.path.exists(raw):
        os.remove(raw)
    subprocess.run([EXE, *flags, "-autoshot", raw, "-screen-width", "1920", "-screen-height", "1080",
                    "-screen-fullscreen", "0"], timeout=120, check=False)
    img = Image.open(raw).convert("RGB")
    if img.size != (1920, 1080):
        img = img.resize((1920, 1080), Image.LANCZOS)
    img = img.crop(box)
    img.thumbnail((1280, 1280), Image.LANCZOS)
    img.save(os.path.join(OUT, name + ".jpg"), quality=88)
    print(name, img.size)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for n in sys.argv[1:] or SHOTS:
        shoot(n)
