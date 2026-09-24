import os
import re
import shutil
import subprocess
import tkinter as tk
from PIL import Image
import xml.etree.ElementTree as ET
from tkinter import filedialog as fd

#-=-=-=-#
# Metadata

__author__ = "kubinka0505"
__credits__ = "kubinka0505"
__date__ = "30th July 2024"

#-=-=-=-#
# Setup

IS_WIN = os.sys.platform.lower().startswith("win32")

# Set up tkinter root window
root = tk.Tk()
root.withdraw()

initial_dir = (
	os.path.expanduser("~/Downloads"),
	os.path.join(os.getcwd(), "_inputs")
)

initial_dir = [dir for dir in initial_dir if os.path.exists(dir)]
initial_dir = sorted(initial_dir, key = os.path.getmtime)[-1]

# IMG
print("Waiting for image file...")
path_img = fd.askopenfilename(
	title = "Please select image file",
	initialdir = initial_dir,
	filetypes = [
		("Portable Network Graphics", "*.png"),
		("Bitmap", "*.bmp"),
		("All files", "*.*"),
	],
	defaultextension = "*.*"
)

if not path_img:
	print("\nImage selection aborted.")
	raise SystemExit

#---#
# XML

print("Waiting for XML file...")
path_xml = fd.askopenfilename(
	title = "Please select XML file",
	initialdir = initial_dir,
	filetypes = [
		("Extensible Markup Language", "*.xml"),
		("All files", "*.*"),
	],
	defaultextension = "*.*"
)

if not path_xml:
	print("XML selection aborted.")

#-=-=-=-#

# Create output directory if it doesn"t exist
output_dir = os.path.join(
	"_outputs",
	os.path.splitext(os.path.basename(path_img))[0],
)

output_dir = os.path.join(
	os.getcwd(),
	output_dir,
)

os.makedirs(output_dir, exist_ok = True)

#-=-=-=-#

def to_pixel_coords(
		norm_x: float, norm_y: float,
		norm_width: float, norm_height: float,
		img_width: int, img_height: int
	) -> tuple:
	"""
	Converts normalized coordinates to pixel coordinates for a given image size.

	Parameters:
		norm_x (float): Normalized x-coordinate (0 <= norm_x <= 1) relative to the image width.
		norm_y (float): Normalized y-coordinate (0 <= norm_y <= 1) relative to the image height.
		norm_width (float): Normalized width (0 <= norm_width <= 1) relative to the image width.
		norm_height (float): Normalized height (0 <= norm_height <= 1) relative to the image height.
		img_width (int): The width of the image in pixels.
		img_height (int): The height of the image in pixels.

	Returns:
		tuple: A tuple containing the pixel coordinates (x, y) and the pixel dimensions (width, height).
	"""
	x = int(norm_x * img_width)
	y = int(norm_y * img_height)

	width = int(norm_width * img_width)
	height = int(norm_height * img_height)

	return x, y, width, height

#-=-=-=-#

# Function to clean and load XML content
try:
	# Clean and parse the XML file
	with open(path_xml, "r", encoding="utf-8") as file:
		content = file.read()

	content = re.sub(r"[^\x09\x0A\x0D\x20-\x7E\x85\xA0-\uD7FF\uE000-\uFFFD\u10000-\u10FFFF]", "", content)
	
	# Parse the cleaned XML content
	xml_root = ET.fromstring(content)
except ET.ParseError as e:
	print(f"XML parsing error: {e}")

	if IS_WIN:
		os.system("timeout 25")

# Load the source image
img = Image.open(path_img)
img_width, img_height = img.size

# Iterate through each texture entry in the XML
for texture in xml_root.findall("Texture"):
	name = texture.get("Name")
	frame = texture.find("Frame")

	norm_x = float(frame.get("X"))
	norm_y = float(frame.get("Y"))

	norm_width = float(frame.get("Width"))
	norm_height = float(frame.get("Height"))
	
	# Convert normalized coordinates to pixel coordinates
	x, y, width, height = to_pixel_coords(
		norm_x, norm_y,
		norm_width, norm_height,
		img_width, img_height
	)
	
	# Crop the image
	cropped_image = img.crop((
		x, y,
		x + width,
		y + height
	))
	
	# Save the cropped image
	output_path = os.path.join(output_dir, f"{name}.png")
	cropped_image.save(output_path)

print("\nDone!\a")

#-=-=-=-#

if IS_WIN:
	cmd = r"start /max C:\Windows\explorer.exe"
elif os.sys.platform.startswith("linux"):
	cmd = "nautilus"
else:
	cmd = "open"

# Null device
if IS_WIN:
	dev_null = ""
else:
	dev_null = ">/dev/null 2>&1"

#-=-=-=-#

cmd += ' "{0}" {1}'.format(output_dir, dev_null)
subprocess.call(cmd, shell = True)