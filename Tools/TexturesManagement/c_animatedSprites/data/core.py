import os
import json

from PIL import Image
from PIL import ImageDraw

from natsort import natsorted

from rectpack import *

from xml.dom import minidom
from xml.etree import ElementTree as ET

#-=-=-=-#

__author__ = "kubinka0505"
__credits__ = [__author__, "gaoyangyiqiao"]
__date__ = "10th May 2026"

#-=-=-=-#

class Packer:
	class Decoder:
		def __init__(self, image_path: str):
			self.image_path = image_path
			self.base, _ = os.path.splitext(image_path)

			self.data_path = None
			self.data_type = None

			self._detect_data_file()

		# -------------------------
		# PUBLIC API
		# -------------------------

		def decode(self) -> dict[str, Image.Image]:
			"""
			Returns:
			{
				filename: PIL.Image
			}
			"""
			big_image = Image.open(self.image_path).convert("RGBA")

			frames = self._load_frames()

			if not frames:
				return {}

			result = {}

			for name, frame in frames.items():
				img = self._build_image(big_image, frame)

				clean_name = name

				if not clean_name.endswith(".png"):
					clean_name += ".png"

				result[clean_name] = img

			return result

		# -------------------------
		# FILE DETECTION
		# -------------------------

		def _detect_data_file(self):
			plist = self.base + ".plist"
			js = self.base + ".json"

			if os.path.exists(plist):
				self.data_path = plist
				self.data_type = "plist"
			elif os.path.exists(js):
				self.data_path = js
				self.data_type = "json"
			else:
				raise FileNotFoundError(f"No .plist or .json found for: {self.base}")

		# -------------------------
		# LOADERS
		# -------------------------

		def _load_frames(self):
			if self.data_type == "plist":
				return self._parse_plist()

			return self._parse_json()

		# -------------------------
		# PLIST PARSER
		# -------------------------

		def _parse_plist(self):
			with open(self.data_path, "r", encoding = "UTF-8") as f:
				root = ET.fromstring(f.read())

			data = self._plist_to_dict(root[0])
			frames = data.get("frames", {})

			if not frames:
				return {}

			out = {}

			for name, frame in frames.items():
				atlas = self._parse_frame_string(frame["frame"])

				x, y, w, h = atlas

				src = self._parse_list(frame["sourceSize"])

				source_w = int(src[0])
				source_h = int(src[1])

				off = self._parse_list(frame["offset"])

				offset_x = int(off[0])
				offset_y = int(off[1])

				out[name] = {
					"atlas": (
						x,
						y,
						w,
						h
					),

					"offset_x": offset_x,
					"offset_y": offset_y,

					"source_w": source_w,
					"source_h": source_h,

					"rotated": frame.get("rotated", False),
				}

			return out

		# -------------------------
		# JSON PARSER
		# -------------------------

		def _parse_json(self):
			with open(
				self.data_path, "r", encoding = "UTF-8") as f:
				data = json.load(f)

			out = {}

			for fdata in data.get("frames", []):
				frame = fdata["frame"]

				x = int(frame["x"])
				y = int(frame["y"])
				w = int(frame["w"])
				h = int(frame["h"])

				src = fdata.get("sourceSize", {})

				source_w = int(src.get("w", w))
				source_h = int(src.get("h", h))

				off = fdata.get("offset", {})
				offset_x = int(off.get("x", 0))
				offset_y = int(off.get("y", 0))

				out[fdata["filename"]] = {
					"atlas": (x, y, w, h),

					"offset_x": offset_x,
					"offset_y": offset_y,

					"source_w": source_w,
					"source_h": source_h,

					"rotated": fdata.get("rotated", False),
				}

			return out

		# -------------------------
		# FRAME BUILDER
		# -------------------------

		def _build_image(
			self,
			big_image,
			frame
		):
			"""
			Reconstruct sprite from atlas crop.

			The encoder rotates sprites 90 degrees
			counter-clockwise when placing them into
			the atlas.

			Therefore the decoder rotates them 90
			degrees clockwise to restore the original.
			"""
			atlas_x, atlas_y, crop_w, crop_h = frame["atlas"]

			crop = big_image.crop(
				(
					atlas_x,
					atlas_y,
					atlas_x + crop_w,
					atlas_y + crop_h,
				)
			)

			# Undo atlas rotation.
			if frame.get("rotated", False):
				crop = crop.rotate(270, expand = True)

			result = Image.new(
				"RGBA",
				(
					frame["source_w"],
					frame["source_h"]
				),
				(0, 0, 0, 0)
			)

			result.paste(
				crop,
				(
					frame["offset_x"],
					frame["offset_y"]
				)
			)

			return result

		# -------------------------
		# FRAME STRING PARSER
		# -------------------------

		def _parse_frame_string(
			self,
			s: str
		):
			parts = (
				s
				.replace("{", "")
				.replace("}", "")
				.split(",")
			)

			x, y, w, h = map(int, parts)

			return x, y, w, h

		# -------------------------
		# LIST PARSER
		# -------------------------

		def _parse_list(self, s):
			return [
				float(x.strip("{} "))
				for x in s.split(",")
				if x.strip()
			]

		# -------------------------
		# PLIST HELPERS
		# -------------------------

		def _plist_to_dict(
			self,
			node
		):
			result = {}

			children = list(node)

			i = 0

			while i < len(children):
				key = children[i]

				val = (
					children[i + 1]
					if i + 1 < len(children)
					else None
				)

				if (
					key.tag == "key"
					and val is not None
				):
					result[key.text] = (
						self._plist_value(val)
					)
					i += 2
				else:
					i += 1

			return result

		def _plist_value(
			self,
			node
		):
			tag = node.tag

			if tag == "string":
				return node.text

			if tag == "true":
				return True

			if tag == "false":
				return False

			if tag == "integer":
				return int(node.text)

			if tag == "dict":
				return self._plist_to_dict(node)

			return None

		# -------------------------
		# GIF EXPORT
		# -------------------------

		def encode_gif(
			self,
			output_path: str,
			duration: int = 80,
			loop: int = 0,
			sort: bool = True,
		):
			frames = self.decode()

			if not frames:
				raise ValueError(
					"No frames decoded"
				)

			items = (
				natsorted(
					frames.items(),
					key = lambda x: x[0]
				)
				if sort
				else frames.items()
			)

			images = []

			for _, img in items:
				img = img.convert(
					"RGBA"
				)

				background = Image.new(
					"RGBA",
					img.size,
					(255, 255, 255, 255)
				)

				img = Image.alpha_composite(
					background,
					img
				)

				img = img.convert(
					"RGBA"
				)

				images.append(img)

			first, *rest = images

			first.save(
				output_path,
				save_all = True,
				append_images = rest,
				duration = duration,
				loop = loop,
				optimize = False,
				disposal = 1,
			)

			return output_path

	#-=-=-=-#
	# ENCODER

	class Encoder:

		def __init__(
			self,
			images: dict[str, Image.Image],
			atlas_name: str,
			cell_padding: int = 1
		):
			if not atlas_name:
				raise ValueError(
					"Atlas name not set"
				)

			self.images = images
			self.atlas_name = atlas_name
			self.base = os.path.splitext(atlas_name)[0]
			self.padding = cell_padding
			self.atlas_size = (0, 0)

		#-=-=-=-#
		# PUBLIC API

		def encode(
			self,
			fmt: str = "plist"
		):

			"""
			Returns:
			(
				PIL.Image,
				dict | ElementTree
			)
			"""
			fmt = fmt.lstrip(".").lower()

			atlas, frames = self._pack_images()

			self.atlas_size = atlas.size

			if fmt == "json":
				data = self._to_json(
					frames
				)
			elif fmt == "plist":
				data = self._to_plist(
					frames
				)
			else:
				raise ValueError(
					f"Unsupported metadata format: {fmt}"
				)

			return atlas, data

		#-=-=-=-#
		# IMAGE PACKING

		def _pack_images(self):
			images = list(self.images.items())

			if not images:
				raise ValueError(
					"Encoder received empty image set"
				)

			# IMPORTANT:

			packer = newPacker(
				pack_algo = MaxRectsBssf,
				rotation = True
			)

			processed = {}

			# -------------------------
			# PREPROCESS
			# -------------------------

			for name, img in images:
				img = img.convert(
					"RGBA"
				)

				orig_w, orig_h = img.size

				bbox = img.getbbox()

				if bbox is None:

					cropped = Image.new(
						"RGBA",
						(1, 1),
						(0, 0, 0, 0)
					)

					paste_x = 0
					paste_y = 0

					crop_w = 1
					crop_h = 1

				else:

					cropped = img.crop(bbox)

					paste_x = bbox[0]
					paste_y = bbox[1]

					crop_w, crop_h = cropped.size

				processed[name] = {
					"image": cropped,

					"crop_w": crop_w,
					"crop_h": crop_h,

					"paste_x": paste_x,
					"paste_y": paste_y,

					"source_w": orig_w,
					"source_h": orig_h,
				}

				packer.add_rect(
					crop_w + self.padding,
					crop_h + self.padding,
					rid = name
				)

			# -------------------------
			# ESTIMATE SIZE
			# -------------------------

			total_area = sum(
				d["crop_w"] * d["crop_h"]
				for d in processed.values()
			)

			size = 64

			while (
				size * size
				< total_area * 1.25
			):
				size *= 2

			packer.add_bin(size, size)

			packer.pack()

			# -------------------------
			# BUILD ATLAS
			# -------------------------
			atlas = Image.new("RGBA", (size, size), (0, 0, 0, 0))

			frames = {}

			max_x = 0
			max_y = 0

			for rect in packer.rect_list():
				_, x, y, w, h, name = rect

				data = processed[name]

				img = data["image"]

				original_w = data["crop_w"]
				original_h = data["crop_h"]

				# rectpack includes the padding in the
				# packed rectangle dimensions.
				#
				# If width/height have been exchanged,
				# rectpack rotated the rectangle.
				rotated = all((
					w == original_h + self.padding,
					h == original_w + self.padding
				))

				# Square images are technically ambiguous,
				# but rotating a square produces the same
				# dimensions and does not affect packing.
				if original_w == original_h:
					rotated = False

				if rotated:
					# Store the image rotated 90 degrees
					# counter-clockwise.
					img = img.rotate(90, expand = True)

				img_w, img_h = img.size

				atlas.paste(img, (x, y))

				max_x = max(max_x, x + img_w)
				max_y = max(max_y, y + img_h)

				frames[name] = {
					"atlas_x": x,
					"atlas_y": y,

					# Dimensions as stored in the atlas.
					"crop_w": img_w,
					"crop_h": img_h,

					# Original image reconstruction data.
					"paste_x": data["paste_x"],
					"paste_y": data["paste_y"],

					"source_w": data["source_w"],
					"source_h": data["source_h"],

					"rotated": rotated,
				}

			# -------------------------
			# SAFE ATLAS OPTIMIZATION
			# -------------------------

			if max_x <= 0:
				max_x = 1

			if max_y <= 0:
				max_y = 1

			atlas = atlas.crop(
				(
					0,
					0,
					max_x,
					max_y
				)
			)

			self.atlas_size = atlas.size

			return atlas, frames

		#-=-=-=-#
		# JSON SERIALIZATION

		def _to_json(self, frames):
			return {
				"frames": [
					{
						"filename": name,

						"frame": {
							"x": frame["atlas_x"],
							"y": frame["atlas_y"],
							"w": frame["crop_w"],
							"h": frame["crop_h"],
						},

						"rotated": frame["rotated"],

						"offset": {
							"x": frame["paste_x"],
							"y": frame["paste_y"],
						},

						"sourceSize": {
							"w": frame["source_w"],
							"h": frame["source_h"],
						},
					}

					for name, frame
					in frames.items()
				],

				"metadata": {
					"size": {
						"w": self.atlas_size[0],
						"h": self.atlas_size[1],
					}
				}
			}

		#-=-=-=-#
		# PLIST SERIALIZATION

		def _to_plist(
			self,
			frames
		):
			root = ET.Element("plist")
			dict_root = ET.SubElement(root, "dict")

			ET.SubElement(dict_root, "key").text = "frames"

			frames_dict = ET.SubElement(dict_root, "dict")

			atlas_base = os.path.splitext(self.atlas_name)[0]

			for index, name in enumerate(
				natsorted(frames),
				start = 1
			):
				frame = frames[name]
				frame_name = f"{atlas_base}_{index}.png"

				ET.SubElement(frames_dict, "key").text = frame_name

				frame_dict = ET.SubElement(frames_dict, "dict")

				# -------------------------
				# FRAME
				# -------------------------

				self._plist_write(
					frame_dict,
					"frame",
					f"{{{{"
					f"{frame['atlas_x']},"
					f"{frame['atlas_y']}"
					f"}},{{"
					f"{frame['crop_w']},"
					f"{frame['crop_h']}"
					f"}}}}"
				)

				# -------------------------
				# OFFSET
				# -------------------------

				self._plist_write(
					frame_dict,
					"offset",
					f"{{"
					f"{frame['paste_x']},"
					f"{frame['paste_y']}"
					f"}}"
				)

				# -------------------------
				# ROTATION
				# -------------------------

				self._plist_write(
					frame_dict,
					"rotated",
					frame.get(
						"rotated",
						False
					),
					is_bool = True
				)

				# -------------------------
				# SOURCE COLOR RECT
				# -------------------------

				# This describes the ORIGINAL
				# unrotated crop dimensions.
				#
				# Do not use atlas crop_w/crop_h
				# here when rotated.
				original_crop_w = (
					frame["crop_h"]
					if frame.get("rotated", False)
					else frame["crop_w"]
				)

				original_crop_h = (
					frame["crop_w"]
					if frame.get("rotated", False)
					else frame["crop_h"]
				)

				self._plist_write(
					frame_dict,
					"sourceColorRect",
					f"{{{{"
					f"{frame['paste_x']},"
					f"{frame['paste_y']}"
					f"}},{{"
					f"{original_crop_w},"
					f"{original_crop_h}"
					f"}}}}"
				)

				# -------------------------
				# SOURCE SIZE
				# -------------------------

				self._plist_write(
					frame_dict,
					"sourceSize",
					f"{{"
					f"{frame['source_w']},"
					f"{frame['source_h']}"
					f"}}"
				)

			# -------------------------
			# METADATA
			# -------------------------

			ET.SubElement(dict_root, "key").text = "metadata"

			w, h = self.atlas_size

			meta = ET.SubElement(dict_root, "dict")

			self._plist_write(meta, "format", 2)
			self._plist_write(meta, "textureFileName", self.base + ".png")
			self._plist_write(meta, "realTextureFileName", self.base + ".png")
			self._plist_write(meta, "size", f"{{{w},{h}}}")

			return ET.ElementTree(root)

		#-=-=-=-#
		# DEBUG VISUALIZATION

		def debug_visualization(
			self,
			atlas,
			frames,
			cell_padding: int = 8,
			bg = (40, 40, 40, 255)
		):
			# ------------------------------------------
			# DEBUG 1: ATLAS VIEW
			# ------------------------------------------

			debug_atlas = atlas.copy()

			draw = ImageDraw.Draw(
				debug_atlas
			)

			for name, frame in frames.items():

				x = frame["atlas_x"]
				y = frame["atlas_y"]

				w = frame["crop_w"]
				h = frame["crop_h"]

				draw.rectangle(
					(
						x,
						y,
						x + w - 1,
						y + h - 1
					),
					outline = (255, 0, 0, 255),
					width = 1
				)

				draw.ellipse(
					(
						x - 2,
						y - 2,
						x + 2,
						y + 2
					),
					fill = (0, 255, 0, 255)
				)

				label = name

				if frame.get(
					"rotated",
					False
				):

					label += " [R]"

				draw.text(
					(
						x + 3,
						y + 3
					),
					label,
					fill = (255, 255, 0, 255)
				)

			# ------------------------------------------
			# DEBUG 2: RECONSTRUCTED VIEW
			# ------------------------------------------

			if not frames:
				return {
					"atlas": debug_atlas,
					"reconstructed": Image.new(
						"RGBA",
						(1, 1),
						bg
					)
				}

			max_w = max(
				f["source_w"]
				for f in frames.values()
			)

			max_h = max(
				f["source_h"]
				for f in frames.values()
			)

			count = len(frames)

			cols = int(count ** 0.5)

			if cols * cols < count:
				cols += 1

			rows = (count + cols - 1) // cols

			cell_w = (max_w + cell_padding * 2)
			cell_h = (max_h + cell_padding * 2)

			canvas = Image.new(
				"RGBA",
				(
					cols * cell_w,
					rows * cell_h
				),
				bg
			)

			draw2 = ImageDraw.Draw(canvas)

			for index, (name, frame) in enumerate(
				frames.items()
			):
				col = index % cols
				row = index // cols

				cell_x = (col * cell_w)
				cell_y = (row * cell_h)

				# -------------------------
				# CELL BORDER
				# -------------------------

				draw2.rectangle(
					(
						cell_x,
						cell_y,
						cell_x + cell_w - 1,
						cell_y + cell_h - 1
					),
					outline = (80, 80, 80, 255)
				)

				# -------------------------
				# RECONSTRUCTED IMAGE
				# -------------------------

				reconstructed = Image.new(
					"RGBA",
					(
						frame["source_w"],
						frame["source_h"]
					),
					(0, 0, 0, 0)
				)

				crop = atlas.crop(
					(
						frame["atlas_x"],
						frame["atlas_y"],
						frame["atlas_x"] + frame["crop_w"],
						frame["atlas_y"] + frame["crop_h"]
					)
				)

				# Undo atlas rotation for preview.
				if frame.get(
					"rotated",
					False
				):
					crop = crop.rotate(270, expand = True)

				reconstructed.paste(
					crop,
					(
						frame["paste_x"],
						frame["paste_y"]
					)
				)

				# -------------------------
				# CENTER PREVIEW
				# -------------------------

				px = (
					cell_x
					+ cell_padding
					+ (max_w
						- frame["source_w"]
					) // 2
				)

				py = (
					cell_y
					+ cell_padding
					+ (
						max_h
						- frame["source_h"]
					) // 2
				)

				canvas.alpha_composite(reconstructed, (px, py))

				# -------------------------
				# ORIGINAL BOUNDS
				# -------------------------

				draw2.rectangle(
					(
						px,
						py,
						px + frame["source_w"] - 1,
						py + frame["source_h"] - 1
					),
					outline = (0, 255, 0, 255)
				)

				# -------------------------
				# CROP PLACEMENT
				# -------------------------

				preview_w = frame["crop_w"]
				preview_h = frame["crop_h"]

				if frame.get(
					"rotated",
					False
				):
					preview_w, preview_h = (preview_h, preview_w)

				draw2.rectangle(
					(
						px + frame["paste_x"],
						py + frame["paste_y"],
						px + frame["paste_x"] + preview_w - 1,
						py + frame["paste_y"] + preview_h - 1
					),
					outline = (255, 0, 0, 255)
				)

				# -------------------------
				# PIVOT / ORIGIN
				# -------------------------

				origin_x = (px + frame["source_w"] // 2)
				origin_y = (py + frame["source_h"] // 2)

				draw2.line(
					(
						origin_x - 5,
						origin_y,
						origin_x + 5,
						origin_y
					),
					fill = (0, 255, 255, 255)
				)

				draw2.line(
					(
						origin_x,
						origin_y - 5,
						origin_x,
						origin_y + 5
					),
					fill = (0, 255, 255, 255)
				)

				label = name

				if frame.get("rotated", False):

					label += " [ROTATED]"

				draw2.text(
					(
						cell_x + 4,
						cell_y + 4
					),
					label,
					fill = (255, 255, 255, 255)
				)

			return {
				"atlas": debug_atlas,
				"reconstructed": canvas
			}

		#-=-=-=-#
		# XML HELPERS

		def plist_to_string(
			self,
			tree: ET.ElementTree,
			pretty: bool = True
		):
			raw = ET.tostring(
				tree.getroot(),
				encoding = "UTF-8"
			)

			if not pretty:
				return raw.decode("UTF-8")

			dom = minidom.parseString(raw)

			pretty_xml = dom.toprettyxml(indent = "\t")

			lines = [
				line
				for line in pretty_xml.splitlines()
				if line.strip()
			]

			return "\n".join(lines)

		def _plist_write(
			self,
			parent,
			key,
			value,
			is_bool = False
		):
			ET.SubElement(parent, "key").text = key

			if is_bool:
				ET.SubElement(parent, "true" if value else "false")
				return

			tag = (
				"integer"
				if isinstance(value, int)
				else "string"
			)

			elem = ET.SubElement(parent, tag)

			elem.text = str(value)