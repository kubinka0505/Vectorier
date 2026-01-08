import os
import shutil
import argparse
import tempfile
import subprocess
import tkinter as tk
from PIL import Image
from pathlib import Path
from random import randint
from xml.dom import minidom
from natsort import natsorted
from collections import Counter
import xml.etree.ElementTree as ET
from tkinter import filedialog as fd, messagebox as msgbox

#-=-=-=-#
# Metadata

__author__  = "kubinka0505"
__credits__ = "elrealsincoma"
__date__    = "24th October 2024"

#-=-=-=-#
# Setup

IS_WIN = os.sys.platform.lower().startswith("win32")

object_string = "object"
trigger_string = "trigger"
tc_string = "track_content"

executable_jpg = "bin", "jpegoptim.exe"
executable_png = "bin", "oxipng.exe"

executable_jpg = os.path.join(*executable_jpg)
executable_png = os.path.join(*executable_png)

INITIALDIR_CONTENT = "_inputs"
OUTDIR = "_outputs"

if not os.path.exists(INITIALDIR_CONTENT):
	INITIALDIR_CONTENT = os.getcwd()

# Find trigger template
trigger_template = ""
for file in os.listdir():
	if file.lower().endswith(".xml"):
		trigger_template = os.path.join(os.getcwd(), file)
		break

if not trigger_template:
	raise FileNotFoundError("No XML trigger template found")

# Set up tkinter root window
root = tk.Tk()
root.withdraw()

#-=-=-=-#
# Arguments

parser = argparse.ArgumentParser(
	description = "Sprite sheets generator for Vector",
	prog = Path(__file__).resolve().name,
	add_help = 0
)

required = parser.add_argument_group("Required arguments")
optional = parser.add_argument_group("Optional arguments")
switch = parser.add_argument_group("Switch arguments")

required.add_argument(
	"-i", "--input",
	type = str,
	default = None,
	help = "Path to directory containing images or animated image file."
)

optional.add_argument(
	"-x", "--fixed-width",
	type = int,
	default = None,
	help = "Fixed width for the sprite. Handles the draw distance, reduces flexibility."
)

optional.add_argument(
	"-y", "--fixed-height",
	type = int,
	default = None, # as above
	help = "Fixed height for the sprite. Handles the draw distance, reduces flexibility."
)

optional.add_argument(
	"-o", "--output-directory",
	type = str,
	default = OUTDIR, 
	help = "Output directory for output files."
)

switch.add_argument(
	"-od", "--open-directory",
	action = "store_true",
	help = "Opens directory with files after processing."
)

switch.add_argument(
	"-h", "--help",
	action = "help",
	help = "Display this message and exit."
)

args = parser.parse_args()

#-=-=-=-#

# Directory containing the images

if not args.input:
	args.input = fd.askdirectory(
		title = "Select directory with sprites with same dimensions",
		initialdir = INITIALDIR_CONTENT
	)

if not args.input:
	args.input = fd.askopenfilename(
		title = "Select animated file with same dimensions",
		initialdir = INITIALDIR_CONTENT,
		filetypes = (
			("GIF image", "*.gif"),
			("All files", "*.*")
		)
	)

	if not args.input:
		msg = "File selection aborted."
		print(msg)

		msgbox.showerror(
			title = "Error",
			message = msg
		)

		raise SystemExit()

if args.input is None:
	parser.error("No input path given.")

# Process GIF
if os.path.isdir(args.input):
	args.input_directory = args.input
else:
	stripextd = os.path.splitext(os.path.basename(args.input))[0].replace(" ", "_")
	args.input_directory = os.path.join(
		INITIALDIR_CONTENT,
		stripextd
	)

	os.makedirs(args.input_directory, exist_ok = True)

	with Image.open(args.input) as img:
		frame_n = 0

		while True:
			frame_path = os.path.join(
				args.input_directory,
				stripextd + f"_{frame_n + 1}.png"
			)
			img.save(frame_path)

			frame_n += 1
			try:
				img.seek(frame_n)
			except EOFError:
				break

dirname = os.path.basename(args.input_directory)

# Output files
args.output_directory = os.path.join(
	args.output_directory,
	dirname + "_" + str(randint(10000, 99999))
)

args.output_directory_tc = os.path.join(
	args.output_directory,
	tc_string
)

args.output_image = os.path.join(args.output_directory_tc, dirname + ".png")
args.output_plist = os.path.join(args.output_directory_tc, dirname + ".plist")
args.output_xml = os.path.join(args.output_directory, dirname + ".xml")

if os.path.exists(args.output_directory):
	pass #shutil.rmtree(args.output_directory)

os.makedirs(args.output_directory, exist_ok = True)
os.makedirs(args.output_directory_tc, exist_ok = True)




# Check dimensions
_msg = 'Not all images inside "{input_directory_path}" directory have the same dimensions!'.format(
	input_directory_path = os.path.basename(args.input_directory)
)

dimensions = None
for file_path in natsorted(Path(args.input_directory).iterdir()):
	if file_path.suffix.lower() in (".png", ".jpg", ".jpeg", ".bmp"):
		with Image.open(file_path) as img:
			if dimensions is None:
				dimensions = img.size
			elif img.size != dimensions:
				print(msg)

				msgbox.showerror(
					title = "Error",
					message = msg
				)

				raise SystemExit()






#-=-=-=-#
# Functions

def has_transparency(obj: str) -> bool:
	"""
	Checks if image has transparency.

	Args:
		obj (string or PIL.Image): Absolute path of existing image or Image object.

	Returns:
		bool: True if `obj` object contains transparency, otherwise False.
	"""
	if isinstance(obj, str):
		obj = Image.open(obj)

	image = obj
	retval = False

	if image.info.get("transparency", None) is not None:
		retval = True
	if image.mode == "P":
		transparent = image.info.get("transparency", -1)
		for _, index in image.getcolors():
			if index == transparent:
				retval = True
	elif image.mode == "RGBA":
		extrema = image.getextrema()
		if extrema[3][0] < 255:
			retval = True

	return retval





def get_image_colors(path: str) -> int:
	"""Determines how much colors is in the image.

	Args:
		path: (string): Image object that will be analyzed.

	Returns:
		integer: Amount of colors given image has.
	"""
	img = Image.open(path)

	return len(Counter(img.getdata()))






def optimize_png_image(input_file_path: str, optimizer_path: str) -> str:
	"""
	Optimizes a PNG image file.

	Args:
		input_file_path (str): Path to image to be optimized.
		optimizer_path (str): Path to optimizing utility.

	Returns:
		str: Path to the optimized image.
	"""
	# Optimize the original image
	command = f'"{optimizer_path}" "{input_file_path}" -a -i 1 -o max -p --strip all'

	with tempfile.TemporaryDirectory() as temp_dir:
		temp_file_path = os.path.join(temp_dir, "temp_image.png")
		shutil.copy2(input_file_path, temp_file_path)

		subprocess.call(
			command,
			stdout = subprocess.PIPE,
			stderr = subprocess.PIPE,
			shell = True
		)

		original_size = os.path.getsize(temp_file_path)
		optimized_size = os.path.getsize(input_file_path)

		if optimized_size > original_size:
			shutil.copy2(temp_file_path, input_file_path)
	
	return input_file_path






def optimize_jpg_image(input_file_path: str, optimizer_path: str) -> str:
	"""
	Optimizes an JPG image file.

	Args:
		input_file_path (str): Path to image to be optimized.
		optimizer_path (str): Path to optimizing utility.

	Returns:
		str: Optimized image path.
	"""
	command = f'"{optimizer_path}" -o -m 100 --all-progressive --strip-all "{input_file_path}" -q'

	output = subprocess.call(
		command,
		stdout = subprocess.PIPE,
		stderr = subprocess.PIPE,
		shell = True
	)

	return input_file_path





def create_sprite_sheet(images_directory: str, output_image: str, output_plist: str, fixed_width: int = None, fixed_height: int = None) -> tuple:
	"""
	Creates a sprite sheet and corresponding plist file from a directory of images.

	This function processes all images in the specified directory and compiles them into 
	a single sprite sheet. It generates a plist file that preserves the original image metadata
	and coordinates for compatibility with external software. Each image is positioned with a 
	common baseline to ensure no shakiness in animation.

	Args:
		images_directory (str): The path to the directory containing the images to be processed.
		output_image (str): The path where the generated sprite sheet image will be saved.
		output_plist (str): The path where the generated plist file will be saved.
		fixed_width (int): Width of canvas that image will be pasted onto. If None, retrieves biggest dimension of found images.
		fixed_height (int): Height of canvas that images will be pasted onto. If None, retrieves biggest dimension of found images.

	Returns:
		tuple: (images_directory, output_image, output_plist).
	"""
	images = []
	original_sizes = []

	for filename in natsorted(os.listdir(images_directory)):
		if filename.lower().endswith((".png", ".jpg", ".jpeg")):
			img_path = os.path.join(images_directory, filename)
			img = Image.open(img_path)
			images.append(img)
			original_sizes.append(img.size)

	if not images:
		raise ValueError("No images found in the specified directory.")

	if not fixed_width:
		fixed_width = max(img.width for img in images)
	if not fixed_height:
		fixed_height = max(img.height for img in images)

	total_height = fixed_height * len(images)
	sprite_sheet = Image.new("RGBA", (fixed_width, total_height))

	y_offset = 0
	coordinates = []

	for img in images:
		x_offset = (fixed_width - img.width) // 2
		y_offset_frame = (fixed_height - img.height) // 2
		sprite_sheet.paste(img, (x_offset, y_offset + y_offset_frame))
		coordinates.append((x_offset, y_offset, img.width, img.height))
		y_offset += fixed_height

	sprite_sheet.save(output_image)

	# Create the plist file
	plist = ET.Element("plist")
	plist.set("version", "1.0")
	dict_element = ET.SubElement(plist, "dict")

	ET.SubElement(dict_element, "key").text = "frames"
	frames_dict = ET.SubElement(dict_element, "dict")

	for i, (x, y, w, h) in enumerate(coordinates):
		filename = natsorted(os.listdir(images_directory))[i]
		ET.SubElement(frames_dict, "key").text = filename  # Correctly placing the filename key
		frame_dict = ET.SubElement(frames_dict, "dict")

		ET.SubElement(frame_dict, "key").text = "frame"
		ET.SubElement(frame_dict, "string").text = f"{{{{{x},{y}}},{{{w},{h}}}}}"
		ET.SubElement(frame_dict, "key").text = "offset"
		ET.SubElement(frame_dict, "string").text = "{0,0}"
		ET.SubElement(frame_dict, "key").text = "rotated"
		ET.SubElement(frame_dict, "false")
		ET.SubElement(frame_dict, "key").text = "sourceColorRect"
		ET.SubElement(frame_dict, "string").text = f"{{{{0,0}},{{{w},{h}}}}}"
		ET.SubElement(frame_dict, "key").text = "sourceSize"
		ET.SubElement(frame_dict, "string").text = f"{{{fixed_width},{fixed_height}}}"

	ET.SubElement(dict_element, "key").text = "metadata"
	metadata_dict = ET.SubElement(dict_element, "dict")
	ET.SubElement(metadata_dict, "key").text = "format"
	ET.SubElement(metadata_dict, "integer").text = "2"
	ET.SubElement(metadata_dict, "key").text = "realTextureFileName"
	ET.SubElement(metadata_dict, "string").text = os.path.basename(output_image)
	ET.SubElement(metadata_dict, "key").text = "size"
	ET.SubElement(metadata_dict, "string").text = f"{{{sprite_sheet.width},{sprite_sheet.height}}}"
	ET.SubElement(metadata_dict, "key").text = "textureFileName"
	ET.SubElement(metadata_dict, "string").text = os.path.basename(output_image)

	xml_str = ET.tostring(plist, encoding = "UTF-8")
	dom = minidom.parseString(xml_str)
	pretty_xml_as_string = dom.toprettyxml(indent = "\t", encoding = "UTF-8").decode("UTF-8")

	with open(output_plist, "w", encoding = "UTF-8") as file:
		file.write(pretty_xml_as_string)

	return (images_directory, output_image, output_plist)



#-=-=-=-#



## Call the function !!!
_ = create_sprite_sheet(
	args.input_directory,
	args.output_image, args.output_plist,
	args.fixed_width, args.fixed_height
)

if os.path.exists(trigger_template):
	shutil.copy2(trigger_template, args.output_xml)

	#-=-=-=-#

	# Get content
	with open(args.output_xml, "r", encoding = "UTF-8") as file:
		content = file.read()

	content = content.replace("OBJECT_NAME", f"{object_string}_{dirname}")
	content = content.replace("TRIGGER_NAME", f"{trigger_string}_{dirname}")
	content = content.replace("SPRITESHEET_NAME", os.path.splitext(os.path.basename(args.output_image))[0])

	# Overwrite
	with open(args.output_xml, "w", encoding = "UTF-8") as file:
		file.write(content)



#-=-=-=-#
# Optimize

_img = Image.open(args.output_image)

if _img.mode == "RGBA":
	if not has_transparency(_img):
		_msg = 'Converting to "RGB" mode: ' + os.path.basename(args.output_image)
		print(_msg, end = "\r")
		_img.convert("RGB").save(args.output_image, quality = 100)
elif args.output_image.lower().endswith(".png") and get_image_colors(args.output_image) <= 256:
	_msg = 'Converting to "P" mode: ' + os.path.basename(args.output_image)
	print(_msg, end = "\r")
	_img.convert("P").save(args.output_image)

try:
	if args.output_image.lower().endswith((".png")):
		if get_image_colors(args.output_image) <= 256:
			optimize_png_image(args.output_image, executable_png)
	elif args.output_image.lower().endswith((".jpg", ".jpeg")):
		optimize_jpg_image(args.output_image, executable_jpg)
except EOFError:
	raise SystemExit()

#-=-=-=-#
# Open directory

if args.open_directory:
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

	cmd += ' "{0}" {1}'.format(args.output_directory, dev_null)
	subprocess.call(cmd, shell = True)
else:
	print(args.output_directory)

print("\a", end = "\r")