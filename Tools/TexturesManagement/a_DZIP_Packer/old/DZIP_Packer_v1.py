"""Transform images, copy PNG files to a separate directory, and create a DZIP archive."""
import os
import json
import time
import shutil
import tempfile
import subprocess
from PIL import Image
from shutil import copy
from pathlib import Path
from PIL import ImageFile
from datetime import timedelta
from collections import Counter
from argparse import ArgumentParser

#-=-=-=-#

__title__   = "DZIP Packer"
__author__  = "kubinka0505"
__credits__ = __author__
__version__ = "1.0"
__date__    = "18th June 2024"
__doc__     = open(__file__, "r", encoding = "UTF-8").readlines()[0].strip('"')[:-4]

#-=-=-=-#
# Variables

START_TIME = time.time()

# JSON
DEFAULT_EXCLUSIONS = ("json/default_exclusions.json", "contains")
DEFAULT_EXCLUSIONS = json.load(open(DEFAULT_EXCLUSIONS[0], "r", encoding = "UTF-8"))[DEFAULT_EXCLUSIONS[1]]

DIRECTORY_EXCLUSIONS = ("json/directory_exclusions.json", "ignored")
DIRECTORY_EXCLUSIONS = json.load(open(DIRECTORY_EXCLUSIONS[0], "r", encoding = "UTF-8"))[DIRECTORY_EXCLUSIONS[1]]
DIRECTORY_EXCLUSIONS = [os.path.normpath(path) for path in DIRECTORY_EXCLUSIONS]

#---#
# Setup

ImageFile.LOAD_TRUNCATED_IMAGES = True

START_TIME_STR = str(START_TIME).split(".")[1].rjust(7, "0")
IS_WIN = os.sys.platform.lower().startswith("win32")

TMP_DIR = "~/AppData/Local/Temp" if IS_WIN else "/tmp"
TMP_DIR = os.path.expanduser(TMP_DIR)
TMP_DIR_MAIN = os.path.join(TMP_DIR, __title__)

CONFIG_FILE = os.path.join(TMP_DIR, "config.dcl")

#-=-=-=-#
# Functions

def fdb_size(obj: str, extended_units: bool = False, bits: bool = False, recursive: bool = False) -> str:
	"""
	Returns human-readable file, sub/directories or bytes size string.
	`obj`: Bytes integer or string path of existing file or directory.
	`extended_units`: Extends the unit of the result, i.e. "Megabytes" instead of "MB".
	`bits`: Uses decimal divider (1000) instead of binary one. (1024)
	`recursive`: Iterate subdirectories, applicable only if `obj` is directory. (slow!)
	"""
	# Setup
	if bits:
		Bits = 1000
		Bits_Display_Single = "bit"
		Bits_Display_Multiple = "bits"
	else:
		Bits = 1024
		Bits_Display_Single = "byte"
		Bits_Display_Multiple = "bytes"

	Units = {
		"":	 "",
		"K": "kilo",
		"M": "mega",
		"G": "giga",
		"T": "tera",
		"P": "peta",
		"E": "exa"
	}

	#-=-=-=-#
	# Search for files

	if isinstance(obj, str):
		path = str(Path(obj).resolve())

		if not os.path.exists(path):
			raise FileNotFoundError(path)

		if os.path.isfile(obj):
			Files = [obj]
		else:
			Wildcard = "*"
			if recursive:
				Wildcard += "*/*"
			Files = list(Path(obj).glob(Wildcard))
			for File in Files:
				Files[Files.index(File)] = str(File.resolve())

		Files = map(os.path.getsize, Files)
		Bytes = sum(Files)
	else:
		Bytes = obj

	#-=-=-=-#
	# Calculate integer

	for Unit in Units:
		if Bytes < Bits:
			break
		Bytes /= Bits

	#-=-=-=-#
	# Ending conditions

	if extended_units:
		if Bytes == 1:
			Bits_Display = Bits_Display_Single
		else:
			Bits_Display = Bits_Display_Multiple
		Unit = (Units[Unit] + Bits_Display).capitalize()
	else:
		Unit = Unit + "B"

	#-=-=-=-#
	# Add zero to integer

	if "." in str(Bytes):
		Bytes = round(Bytes, 2)
		Bytes = str(Bytes).split(".")
		Bytes[1] = Bytes[1][:2]
		Bytes[1] = Bytes[1].ljust(2, "0")
		Bytes = ".".join(Bytes)

	Bytes = str(Bytes)

	#-=-=-=-#
	# Return string

	return " ".join((Bytes, Unit))





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





def transform_images(base_dir: str, rotate_step: float, max_rotate_range: int, excluded: tuple, excluded_directories: tuple, resampler: Image.Resampling, create_subdirs: bool, optimize: bool, optimizer_path: str) -> list:
	files_to_process = []

	for file_path in Path(base_dir).rglob("*"):
		file = str(file_path.resolve())

		# Skip files in excluded directories
		if any(excluded_directory in file for excluded_directory in excluded_directories):
			continue

		# Process only image files
		if os.path.isfile(file_path) and file_path.suffix.lower() in (".png", ".jpg", ".jpeg"):
			# Skip files that match any exclusion pattern
			if any(exclusion in os.path.basename(file_path) for exclusion in excluded):
				continue

			files_to_process.append(file)

	files_to_process.sort(key = str)
	files_to_process.sort(key = len, reverse = True)

	total_files = len(files_to_process)
	total_files *= int(max_rotate_range / rotate_step)
	paths = []

	#-=-=-=-#

	for file_path in files_to_process:
		try:
			with Image.open(file_path) as img:
				f_splitext = os.path.splitext(os.path.basename(file_path))
				img = img.convert("RGBA")
				save_dir = os.path.dirname(file_path)

				for deg in np.arange(rotate_step, max_rotate_range, rotate_step):
					deg_str = f"{deg:.1f}".replace(".", ",")

					#-=-=-=-#
					# Modify

					modified_img = img.rotate(-deg, resample = resampler, expand = True)
					background = Image.new("RGBA", modified_img.size, (0,) * 4)
					background.paste(modified_img, (0, 0), modified_img)
					modified_img = background

					#-=-=-=-#

					if create_subdirs:
						save_subdir = os.path.join(save_dir, deg_str)
						ret_format = ""
					else:
						save_subdir = save_dir
						ret_format = "_" + deg_str + "deg"

					os.makedirs(save_subdir, exist_ok=True)

					modified_file_name = ret_format.join((
						f_splitext[0].strip("_"),
						".png" if has_transparency(modified_img) else f_splitext[-1]
					))

					modified_file_path = os.path.join(save_subdir, modified_file_name)

					if modified_file_path.lower().endswith(".png"):
						modified_img = modified_img.crop(modified_img.getbbox())
						modified_img.save(modified_file_path)

					paths.append(modified_file_path)

					#-=-=-=-#

					processed_files = len(paths)
					processed_files_str = f"[{processed_files}/{total_files}]"

					percentage = 100 * (processed_files / total_files)

					#-=-=-=-#

					if IS_WIN:
						os.system(f"title Processing {percentage:.2f}% {processed_files_str}")

					_msg = "Processing: "
					ts = os.get_terminal_size().columns - len(_msg)
					print(
						_msg + \
						os.path.relpath(file_path, base_dir).ljust(ts)[:ts],
						end = "\r"
					)

		except KeyboardInterrupt:
			break
		except EOFError:
			raise SystemExit()

	return paths




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





def copy_to_dir(directory: str, output: str, exts: tuple) -> str:
	"""
	Copies files of a specific extension to an output directory.

	Args:
		directory (str): The input directory containing files.
		output (str): The output directory where files will be copied.
		exts (tuple): The extensions of the files to be copied.

	Returns:
		str: The path to the output directory.
	"""
	exts = [ext.lower().strip(".") for ext in exts]

	if output is None:
		output = "_".join((
			directory,
			"-".join(map(str.upper, exts)),
			"only".title()
		))

	if not os.path.isdir(output):
		os.makedirs(output, exist_ok = True)

	#-=-=-=-#

	for file in Path(directory).rglob(f"*.*"):
		file = str(file)
		for ext in exts:
			if file.lower().endswith("." + ext):
				shutil.copy(
					file,
					os.path.join(output, os.path.basename(file))
				)

	if not os.listdir(output):
		os.rmdir(output)

	return os.path.abspath(output)





def dzip_all(dzip_executable_path: str, directory_input: str, directory_output: str, archive_name: str, compression_algorithm: str) -> str:
	"""
	Creates a DZIP archive of all files in the input directory.

	Args:
		dzip_executable_path (str): Path to the dzip executable.
		directory_input (str): Input directory containing files to archive.
		directory_output (str): Output directory where the archive will be saved.
		archive_name (str): Name of the archive file.
		compression_algorithm (str): Compression algorithm used in DZIP.

	Returns:
		str: The path to the created archive file.
	"""
	if not os.path.isdir(directory_output):
		os.makedirs(directory_output, exist_ok = True)

	archive_file = os.path.join(directory_output, archive_name)

	#-=-=-=-#

	with open(CONFIG_FILE, mode = "w", encoding = "UTF-8") as tFile:

		## Bugged
		# DZIP refuses to work if amount of files/data is too "large", wontfix

		tFile.write(f'archive "{archive_file}"\n')
		tFile.write(f'basedir "{directory_input}"\n')

		files = [str(file.resolve()) for file in Path(directory_input).rglob("*.*")]
		#files.sort(key = len)
		files.sort(key = os.path.getsize)

		for file in files:
			file = os.path.basename(file)

			tFile.write(f'file "{file}" 0 {compression_algorithm}')

			if file != files[-1]:
				tFile.write("\n")

		#-=-=-=-#

	command = f'"{dzip_executable_path}" "{tFile.name}" -v'

	subprocess.call(
		command,
		# stdout = subprocess.PIPE,
		# stderr = subprocess.PIPE,
		shell = True
	)
	print()

	return archive_file

#-=-=-=-#
# Arguments

parser = ArgumentParser(
	prog = Path(__file__).resolve().name,
	description = __doc__,
	add_help = False
)
required = parser.add_argument_group("Optional arguments")
optional = parser.add_argument_group("Optional arguments")
switch = parser.add_argument_group("Optional arguments")

# Required
required.add_argument(
	"-i", "--input-directory",
	type = str,
	help = "The input directory containing images to process. Backup is created."
)

required.add_argument(
	"-a", "--archive-name",
	type = str,
	help = "Name of the archive file."
)

# Optional
optional.add_argument(
	"-rot", "--angle",
	type = float, default = 90 / 4,
	help = 'The step size in degrees for rotating images, i.e. "90"'
)

optional.add_argument(
	"-rmax", "--max-angle",
	type = float, default = 90,
	help = 'The step size in degrees for rotating images, i.e. "180"'
)

optional.add_argument(
	"-e", "--excluded",
	type = str, nargs = "*", default = DEFAULT_EXCLUSIONS,
	help = "List of substrings to exclude certain files."
)

optional.add_argument(
	"-ed", "--excluded-directories",
	type = str, nargs = "*", default = DIRECTORY_EXCLUSIONS,
	help = "List of directories to exclude from processing."
)

optional.add_argument(
	"-r", "--resampler",
	type = int, default = 3, choices = [0, 2, 3],
	help = "PIL Resampling method to use for rotation (integer value)."
)

optional.add_argument(
	"-ca", "--compression-algorithm",
	type = str, default = "dz", choices = ["dz", "zlib", "bzip", "zero", "copy"],
	help = "Dzip compression algorithm."
)

optional.add_argument(
	"-exe-dz", "--executable-dzip",
	type = str, default = "bin/dzip.exe",
	help = "Path to the DZIP executable."
)

optional.add_argument(
	"-exe-png", "--executable-png",
	type = str, default = "bin/oxipng.exe",
	help = "Path to the PNG optimizer executable."
)

optional.add_argument(
	"-exe-jpg", "--executable-jpg",
	type = str, default = "bin/jpegoptim.exe",
	help = "Path to the JPG/JPEG optimizer executable."
)

optional.add_argument(
	"-d", "--output-directory",
	type = str, default = "_outputs",
	help = "Output files parent directory."
)

# Switch
switch.add_argument(
	"-nsd", "--no-create-subdirectories",
	action = "store_true",
	help = "Do not create subdirectories for transformed degrees."
)

switch.add_argument(
	"-no", "--no-optimize",
	action = "store_true",
	help = "Do not optimize the images using executables."
)

switch.add_argument(
	"-nod", "--no-open-directory",
	action = "store_true",
	help = "Do not open directory after successful compression."
)

switch.add_argument(
	"-h", "--help",
	action = "help",
	help = "Show this message and exit."
)

#-=-=-=-#
# Settings

for arg in required._group_actions:
	if not arg.default:
		arg.required = True

args = parser.parse_args()

# Pre-cleanup
for file in (CONFIG_FILE, args.archive_name):
	if os.path.exists(file):
		os.remove(file)

# Setup
args.optimize = not args.no_optimize
args.create_subdirectories = not args.no_create_subdirectories
args.archive_name = os.path.splitext(args.archive_name)[0].strip(".dz") + ".dz"

# Transformation settings
if not args.angle:
	parser.error("Cannot create variations without angle")
args.angle = round(args.angle, 2)

if args.max_angle < -360 or args.max_angle > 360:
	parser.error("Maximum args.max_angle range is ±360")

if args.angle < -args.max_angle or args.angle > args.max_angle:
	parser.error("Angles requirements do not meet ({0} {2} {1})".format(
		args.angle, args.max_angle,
		">" if args.angle > args.max_angle else "<"
	))

# Check executables
if not os.path.exists(args.executable_dzip):
	parser.error(f'File not found ("{args.executable_dzip}")')

if args.optimize:
	for file in (args.executable_png, args.executable_jpg):
		if not os.path.exists(file):
			parser.error(f'File not found ("{file}")')

# Directories
## Input
# args.input_directory = os.path.join("_inputs", args.input_directory)
args.input_directory = str(Path(args.input_directory).resolve())

if not os.path.exists(args.input_directory):
	parser.error('Directory not found ("{0}")'.format(
		os.path.relpath(args.input_directory, os.getcwd())
	))

## Process
args.input_directory_process = os.path.join(
	TMP_DIR_MAIN, os.path.basename(args.input_directory),
)

args.input_directory_process = os.path.join(TMP_DIR, args.input_directory_process)

## Output
args.output_directory = os.path.abspath(
	os.path.join(
		args.output_directory,
		" ".join((
			os.path.basename(args.input_directory),
			START_TIME_STR
		)).replace(" ", "_")
	)
)

#-=-=-=-=-=-#
# Process

## Backup original input directory
if os.path.exists(args.input_directory_process):
	shutil.rmtree(args.input_directory_process)

shutil.copytree(args.input_directory, args.input_directory_process)

# Transforming
_transformed_images = transform_images(
	args.input_directory_process,
	args.angle,
	args.max_angle,
	tuple(args.excluded),
	tuple(args.excluded_directories),
	Image.Resampling(args.resampler),
	args.create_subdirectories,
	args.optimize,
	args.executable_png
)




## Optimization

# Optimizing setup
files_to_optimize = []
for fmt in ("PNG", "JPG", "JPEG"):
	for file in Path(args.input_directory_process).rglob("*." + fmt.lower()):
		file = str(file.resolve())
		files_to_optimize.append(file)

files_to_optimize.sort(key = os.path.getsize)
files_to_optimize_sizes = [os.path.getsize(file) for file in files_to_optimize]





# Process images
for file in sorted(files_to_optimize, key = len)[::-1]:
	_img = Image.open(file)
	if _img.mode == "RGBA":
		if not has_transparency(_img):
			_msg = 'Converting to "RGB" mode: ' + os.path.basename(file)

			print(
				_msg + " " * (os.get_terminal_size()[0] - len(_msg)),
				end="\r"
			)

			_img.convert("RGB").save(file, quality = 100)
	elif file.lower().endswith(".png") and get_image_colors(file) <= 256:
		_msg = 'Converting to "P" mode: ' + os.path.basename(file)

		print(
			_msg + " " * (os.get_terminal_size()[0] - len(_msg)),
			end="\r"
		)

		_img.convert("P").save(file)





# Optimize with executable
if args.optimize:
	print(" " * os.get_terminal_size()[0], end = "\r")

	counter = 0
	for file in sorted(files_to_optimize, key = os.path.getsize):
		try:
			print(
				"Optimizing modified images... [{0}/{1}]".format(
					counter,
					len(files_to_optimize)
				),
				end = "\r"
			)

			if file.lower().endswith((".png")):
				if get_image_colors(file) <= 256:
					optimize_png_image(file, args.executable_png)
			elif file.lower().endswith((".jpg", ".jpeg")):
				optimize_jpg_image(file, args.executable_jpg)

			counter += 1
		except KeyboardInterrupt:
			break
		except EOFError:
			raise SystemExit()





# Copying images only
extracted_files = copy_to_dir(args.input_directory_process, None, (("PNG", "JPG", "JPEG")))

# Creating archive
print() # prepare
archive = dzip_all(
	args.executable_dzip,
	extracted_files,
	os.path.dirname(extracted_files),
	args.archive_name,
	args.compression_algorithm
)

#-=-=-=-#
# Finish

# Gather final files info
archive = os.path.abspath(archive)
archive_size = fdb_size(archive)
elapsed_time = str(timedelta(seconds = (time.time() - START_TIME)))[2:-3]

# Check DZIP bytes size
fail = 'DZIP failed to compress. Exiting.\nLook inside "ReadMe.md" for more tips.\a\a'
if os.path.exists(archive):
	success = os.path.getsize(archive) >= sum(files_to_optimize_sizes) * 0.25

# Remove useless files and output the information
if success:
	## Create output directory
	os.makedirs(args.output_directory, exist_ok = True)

	shutil.move(args.input_directory_process, args.output_directory)
	archive = shutil.move(archive, args.output_directory)

	print(f"Archive saved to:     {archive}\a")
	print(f"Archive file size:    {archive_size}")
	print(f"Elapsed time:         {elapsed_time}")

	os.remove(CONFIG_FILE)
	shutil.rmtree(extracted_files)
else:
	print()

	os.rename(
		args.input_directory_process,
		"_".join((
			args.input_directory_process,
			str(time.time()).split(".")[-1]
		))
	)

	print(fail)
	raise SystemExit





# Post-cleanup
if not os.listdir(TMP_DIR_MAIN):
	shutil.rmtree(TMP_DIR_MAIN)

# Open directory
if not args.no_open_directory:
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