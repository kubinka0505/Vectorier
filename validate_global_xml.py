import os
from pathlib import Path
import xml.etree.ElementTree as ET

for file in Path().rglob("*.xml"):
	file = str(file.resolve())
	try:
		ET.parse(file)
	except Exception as e:
		print(file, str(e), sep = "\t")
		print()