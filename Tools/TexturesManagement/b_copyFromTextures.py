import os
import shutil
from pathlib import Path

current = "!_Textures_ALL_PICTURES"
to_copy = "../Assets/Resources/Textures"

shutil.rmtree(current)
shutil.copytree(to_copy, current)

for file in Path(current).rglob("*.meta"):
	os.remove( str(file.resolve()) )