"""Zips the built game for sharing (a GitHub Release asset).

    python Tools/Release/make_release_zip.py [version]     # default version: v1.0

Takes Builds/Table (build it first: DEVELOPMENT §7) and writes Builds/RestartedTavern-<version>-win64.zip with a
RestartedTavern/ folder inside. Leaves out your local BugReports/, Playtests/ and Unity's debug-only folders.
"""
import os, sys, zipfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Builds", "Table")
SKIP_DIRS = {"BugReports", "Playtests"}
SKIP_SUFFIX = ("_BurstDebugInformation_DoNotShip", "_BackUpThisFolder_ButDontShipItWithYourGame")

version = sys.argv[1] if len(sys.argv) > 1 else "v1.0"
out = os.path.join(ROOT, "Builds", "RestartedTavern-" + version + "-win64.zip")
if not os.path.exists(os.path.join(SRC, "RestartedTavern.exe")):
    sys.exit("No build in Builds/Table: build the game first.")
count = 0
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    for dirpath, dirnames, filenames in os.walk(SRC):
        rel = os.path.relpath(dirpath, SRC)
        top = rel.split(os.sep)[0]
        if top in SKIP_DIRS or top.endswith(SKIP_SUFFIX):
            dirnames[:] = []
            continue
        for f in filenames:
            path = os.path.join(dirpath, f)
            z.write(path, os.path.join("RestartedTavern", os.path.relpath(path, SRC)))
            count += 1
print("%s: %d files, %.1f MB" % (out, count, os.path.getsize(out) / 1e6))
