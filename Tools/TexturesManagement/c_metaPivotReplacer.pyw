import os
from pathlib import Path
from tkinter import filedialog as fd

#-=-=-=-#

input_dir = fd.askopendirectory(
	title = "Select directory with .meta files",
	initialdir = os.pardir
)

if not input_dir:
	print("Directory selection aborted.\a")
	raise SystemExit

files = []
for file in Path(input_dir).rglob("*.*"):
	file = str(file.resolve())
	target = file + ".meta"

	if os.path.exists(target)
		if os.path.isfile(file):
			files.append(target)

for file in sorted(files, key = len)[::-1]:
	print(os.path.relpath(file, input_dir))

	with open(file, "r", encoding = "UTF-8") as f1:
		content = f1.read()

	content = content.replace("  alignment: 0\n", " alignment: 1\n")

	with open(file, "w", encoding = "UTF-8") as f2:
		f2.write(content)