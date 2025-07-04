import os
import shutil
import tkinter as tk
from pathlib import Path
from tkinter import filedialog as fd, messagebox as msgbox

#-=-=-=-#
# Setup

root = tk.Tk()
root.withdraw()

#-=-=-=-#
# Default directories

__BaseDir = os.path.abspath(os.path.dirname(__file__))
initialdir_content = "../../Assets/Resources/Textures"
initialdir_meta = "!_Textures_META"

if not os.path.exists(initialdir_content):
	initialdir_content = initialdir_meta

if not os.path.exists(initialdir_content):
	initialdir_content = os.getcwd()

if not os.path.exists(initialdir_meta):
	# Pick the latest modified directory as fallback
	subdirs = [d for d in next(os.walk("."))[1]]
	if subdirs:
		initialdir_meta = sorted(subdirs, key=os.path.getmtime)[-1]
	else:
		initialdir_meta = os.getcwd()

# Final fallback if still invalid
if not os.path.exists(initialdir_content):
	initialdir_content = os.getcwd()

#-=-=-=-#
# Ask for the directory with all meta files

_msg = "Select directory containing files with their existing .meta counterpart files"
print(_msg + "...")

dir_with_meta = fd.askdirectory(
	title=_msg,
	initialdir=initialdir_content
)

if not dir_with_meta:
	print("Directory selection aborted.")
	raise SystemExit()

# Ask for the directory without all meta files
_msg = "Select target .meta files directory"
print(_msg + "...")

dir_without_meta = fd.askdirectory(
	title=_msg,
	initialdir=initialdir_meta
)

if not dir_without_meta:
	print("Directory selection aborted.")
	raise SystemExit()

# Convert paths to absolute paths
dir_with_meta = os.path.abspath(dir_with_meta)
dir_without_meta = os.path.abspath(dir_without_meta)

# Ensure the destination is not inside the source
if os.path.commonpath([dir_with_meta]) == os.path.commonpath([dir_with_meta, dir_without_meta]):
	raise SystemExit("Error: Destination directory must not be inside the source directory.")

print(f"\nSource (with .meta):      {dir_with_meta}")
print(f"Destination (to copy to): {dir_without_meta}")

#-=-=-=-#
# Collect valid .meta files (only those directly associated with base files, not meta-of-meta)

files = []
for file in Path(dir_with_meta).rglob("*.meta"):
	full_path = str(file.resolve())
	if full_path.startswith(dir_without_meta):
		continue  # Avoid destination re-scan
	stem = file.stem  # Remove ".meta" extension
	if stem.endswith(".meta"):
		continue  # Avoid ".meta.meta", ".meta.meta.meta", etc.
	files.append(full_path)

if not files:
	raise SystemExit("No valid .meta files found.")

print(f"Found {len(files)} .meta files.\n")

#-=-=-=-#
# Loop to copy files

for index, old_file in enumerate(files, start=1):
	print(f"[{index}/{len(files)}] Copying file...", end="\r")

	relative_path = os.path.relpath(old_file, dir_with_meta)
	new_file = os.path.join(dir_without_meta, relative_path)

	new_dirname = os.path.dirname(new_file)
	if not os.path.exists(new_dirname):
		os.makedirs(new_dirname, exist_ok=True)

	shutil.copy2(old_file, new_file)

# End of process
msgbox.showinfo(
	title="Info",
	message=f'Success, copied {len(files)} ".meta" files'
)