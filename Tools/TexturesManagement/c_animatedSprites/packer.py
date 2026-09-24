import os
import json
import argparse
from typing import Union, Iterable
from tkinter import filedialog as fd

from data.cli import build_parser

#-=-=-=-#
# Variables

DIRECTORY_INPUTS = os.path.abspath(os.path.join("_inputs"))
DIRECTORY_IMAGES = os.path.abspath(os.path.join("..", "..", "..", "..", "Vectorier", "Assets", "XML", "dzip", "archives", "uncompiled", "track_content_universal"))
DIRECTORY_GAME = os.path.abspath(os.path.join("..", "..", "..", "..", "Vector"))
FILE_PACKER = os.path.abspath(os.path.join("data", "bin", "dzip"))

# Settings
parser = build_parser(DIRECTORY_IMAGES)

args = parser.parse_args()

VALID_IMAGES_DICT = {
	"Portable Network Graphics": "*.png",
	"JPG Image": "*.jpg *.jpeg",
	"GIF Image": "*.gif",
	"All files": "*.*"
}
VALID_IMAGES = {".png", ".jpg", ".jpeg"}

if args.mode == "encode":
	# normalize CLI input
	inputs = args.input_file or []
	inputs = [f for f in inputs if f.strip()]

	# if nothing from CLI then open GUI
	if not inputs:
		dir_path = fd.askdirectory(
			title = "Select directory with frames",
			initialdir = DIRECTORY_INPUTS
		)

		if dir_path:
			inputs = [dir_path]

	# if still nothing then file picker
	if not inputs:
		files = fd.askopenfilenames(
			title = "Select files",
			initialdir = DIRECTORY_INPUTS,
			filetypes = list(VALID_IMAGES_DICT.items())
		)

		if files:
			inputs = list(files)

	if not inputs:
		parser.error("No files inputted")

	if not args.name_atlas:
		if len(inputs) < 2:
			args.name_atlas = os.path.splitext(os.path.basename(inputs[0]))[0]
		else:
			parser.error("No atlas name specified")

	args.input_file = inputs

	fp = 3
	sep = "_"
	sparts = args.name_atlas.split(sep)
	lsp = len(sparts)

	if lsp < fp:
		parser.error(
			f"""Atlas name needs to have {fp} parts separated with "{sep}", currently {lsp}: {", ".join(sparts)}""".strip()
		)

#-=-=-=-#

import shutil
import tempfile
import subprocess
from pathlib import Path
from xml.dom import minidom
from natsort import natsorted
from PIL import Image, ImageSequence

from data import core

#---#

def collect_images(paths: Union[str]) -> Iterable[Image.Image]:
	"""
	Collect images from:
	- files
	- directories
	- GIF images (expanded into frames)

	Returns:
	{
		filename: PIL.Image
	}
	"""

	images = {}

	for path in paths:
		path = Path(path)

		if not path.exists():
			print(f"[missing] {path}")
			continue

		# directory

		if path.is_dir():
			files = []

			for file in path.rglob("*"):
				if file.suffix.lower() in VALID_IMAGES:
					files.append(file)

			files = natsorted(files)

			for file in files:
				try:
					img = Image.open(file).convert("RGBA")

					images[file.name] = img

				except Exception as Error:
					print(Error)

			continue

		# gif
		if path.suffix.lower() == ".gif":
			try:
				gif = Image.open(path)

				for i, frame in enumerate(ImageSequence.Iterator(gif)):
					name = f"{path.stem}_{i}.png"

					images[name] = frame.convert("RGBA")

			except Exception as Error:
				print(Error)

			continue

		# static image
		if path.suffix.lower() in VALID_IMAGES:
			try:
				img = Image.open(path).convert("RGBA")

				images[path.name] = img

			except Exception as Error:
				print(Error)

	return images

def save_json(
	data,
	path,
	pretty: bool = True
):
	with open(path, "w", encoding = "utf-8") as f:
		if pretty:
			json.dump(
				data,
				f,
				indent = "\t",
				ensure_ascii = False
			)

		else:
			json.dump(
				data,
				f,
				ensure_ascii = False
			)

def save_plist(
	tree,
	path,
	pretty: bool = True
):
	tree.write(
		path,
		encoding = "UTF-8",
		xml_declaration = True,
		short_empty_elements = False
	)

	if not pretty:
		return

	with open(path, "rb") as f:
		raw = f.read()

	dom = minidom.parseString(raw)

	pretty_xml = dom.toprettyxml(
		indent = "\t",
		encoding = "UTF-8"
	)

	lines = [
		line
		for line in pretty_xml.decode("UTF-8").splitlines()
		if line.strip()
	]

	xml = "\n".join(lines)

	xml = xml.replace(" />", "/>")

	with open(path, "w", encoding = "utf-8") as f:
		f.write(xml)

def save_metadata(
	data,
	fmt,
	path,
	pretty: bool = True
):
	fmt = fmt.lower().lstrip(".")

	if fmt == "json":
		save_json(
			data,
			path,
			pretty = pretty
		)

	elif fmt == "plist":
		save_plist(
			data,
			path,
			pretty = pretty
		)

	else:
		raise ValueError(
			f"Unsupported metadata format: {fmt}"
		)

#---#

def write_config(
	path_config: str,

	path_basedir: str,
	path_archive: str = None,

	files: list[str] = None,
	index: int = 0,
	algorithm: str = "dz"
):
	if not files:
		raise Exception("No files to write")

	if not path_archive:
		path_archive = path_basedir + ".dz"

	with open(path_config, "w", encoding = "utf-8") as cfg:
		cfg.write(f'basedir "{path_basedir}"\n')
		cfg.write(f'archive "{path_archive}"\n')

		for file_idx, file_path in enumerate(natsorted(files, key = str)):
			file_rel = os.path.relpath(file_path, path_basedir)

			ae = True
			if os.path.exists(file_path):
				cfg.write(f'file "{file_rel}" {index} {algorithm.lower()}')
			else:
				ae = False
				print('File not found: "{file_rel}"')

			if file_idx < len(files) - 1:
				cfg.write("\n")

		if not ae:
			print("Not all files were written")

	return path_config, path_basedir, path_archive

#-=-=-=-#

# Mode
if args.mode == "encode":
	images = collect_images(args.input_file)

	if not images:
		raise RuntimeError("No valid images found")

	os.makedirs(
		args.output_directory,
		exist_ok = True
	)

	encoder = core.Packer.Encoder(
		images = images,
		atlas_name = args.name_atlas,
		cell_padding = args.padding
	)

	results = encoder.benchmark()

	for result in results:
		print(
			f"{result['algorithm']:24} "
			f"{'ROT' if result['rotation'] else 'NO-ROT':7} "
			f"{result['width']:4}x"
			f"{result['height']:<4} "
			f"area={result['area']:8} "
			f"eff={result['efficiency']:6.2%} "
			f"time={result['time_ms']:8.3f} ms"
		)

	best = results[0]

	print(
		f"\nBest: "
		f"{best['algorithm']} "
		f"({'rotation' if best['rotation'] else 'no rotation'}) "
		f"{best['width']}x{best['height']}"
	)

	atlas, metadata = encoder.encode(
		args.output_format,
		pack_algo=best["pack_algo"],
		rotation=best["rotation"]
	)

	debug = encoder.debug_visualization(
		atlas,
		encoder._pack_images()[1]
	)

	for x in debug.values():
		x.show()

	optimize = not args.no_optimize

	# atlas output

	atlas_path = os.path.join(
		args.output_directory,
		f"{args.name_atlas}.png"
	)

	atlas.save(
		atlas_path,
		optimize = optimize
	)

	# metadata output

	meta_path = os.path.join(
		args.output_directory,
		f"{args.name_atlas}.{args.output_format}"
	)

	atlas_path = os.path.normpath(atlas_path)
	meta_path = os.path.normpath(meta_path)

	if args.output_format == "json":
		save_json(metadata, meta_path)

	if args.output_format == "plist":
		save_plist(metadata, meta_path)

	save_metadata(
		metadata,
		args.output_format,
		meta_path
	)

	print(f"[atlas] {atlas_path}")
	print(f"[meta ] {meta_path}")

	files = Path(DIRECTORY_IMAGES).rglob("*")
	files = [
		str(file.resolve())
		for file in Path(DIRECTORY_IMAGES).rglob("*")
		if file.is_file()
	]

	if not args.no_write:
		with tempfile.NamedTemporaryFile(
			suffix = ".dcl",
			mode = "w",
			delete = False,
			encoding = "UTF-8"
		) as tf:

			config, _, archive = write_config(
				path_config = tf.name,
				path_basedir = DIRECTORY_IMAGES,
				files = files,
				algorithm = "copy"
			)

		# IMPORTANT: wait for packer to finish
		result = subprocess.run(
			[FILE_PACKER, config],
			shell = True,
			capture_output = True,
			text = True
		)

		if result.returncode:
			print(result.stderr)
			raise RuntimeError("Packer failed")

		# wait for file to exist (Windows safety)
		for _ in range(50):
			if os.path.exists(archive):
				break

			time.sleep(0.05)
		else:
			raise FileNotFoundError(f"Archive missing: {archive}")

		archive_game = os.path.join(
			DIRECTORY_GAME,
			os.path.basename(archive)
		)

		# ensure destination clean
		if os.path.exists(archive_game):
			os.remove(archive_game)

		# COPY instead of MOVE
		shutil.copy2(archive, archive_game)

		# OPTIONAL: remove original archive after copy
		os.remove(archive)

		print("written to archive")

		os.remove(tf.name)

if args.mode == "decode":
    os.makedirs(args.output_directory, exist_ok = True)

    optimize = not args.no_optimize

    for file in args.input_file:
        decoder = core.Packer.Decoder(file)

        if args.frame_rate > 0:
            # GIF PIPELINE

            out_path = os.path.join(
                args.output_directory,
                os.path.splitext(os.path.basename(file))[0] + "." + "gif"
            )

            decoder.encode_gif(
                out_path,
                duration = int(1000 / args.frame_rate),
                loop = 0
            )

            print(f"[gif] {out_path}")

        else:
            # FRAME EXPORT PIPELINE
            images = decoder.decode()

            for name, img in images.items():
                out = os.path.join(args.output_directory, name)

                img.save(out, optimize=optimize)

                print(f"[write] {out}")