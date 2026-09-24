import re
import os
import string
from lxml import etree

#-=-=-=-#
# typed wrappers

class Skeleton:
	def __init__(self, model):
		self.model = model

	def save(self, path = None):
		self.model.save(path)

	def optimize(self):
		return self.model.optimize()

	def __repr__(self):
		return self.__class__.__name__

class Skin:
	def __init__(self, model):
		self.model = model

	def save(self, path = None):
		self.model.save(path)

	def optimize(self):
		return self.model.optimize()

	def __repr__(self):
		return self.__class__.__name__

class Object:
	def __init__(self, model):
		self.model = model

	def save(self, path = None):
		self.model.save(path)

	def optimize(self):
		return self.model.optimize()

	def __repr__(self):
		return self.__class__.__name__

#-=-=-=-#
# core

class Model:
	_RULE_SKELETON = {
		"Camera": ("Type")
	}

	_RULE_OBJECT = {
		"NPivot": ("Type")
	}

	_NODES_SCENE = ("Nodes", "Edges", "Figures")
	_ATTRS_NAMED = ("End", "Node", "Edge")
	_ATTRS_EMPTY = ("0", "")

	#-=-=-=-#
	# init

	def __init__(self, file_input: str):
		self.file_input = file_input
		self.tree = etree.parse(file_input)
		self.root = self.tree.getroot()

	#-=-=-=-#
	# cloning (immutability)

	def _clone(self):
		new_model = Model.__new__(Model)
		new_model.file_input = self.file_input
		new_model.tree = etree.fromstring(etree.tostring(self.root))
		new_model.root = new_model.tree

		return new_model

	#-=-=-=-#
	# base converter
	def _convert_numeral(self, n):
		chars = string.ascii_uppercase
		base = len(chars)

		if isinstance(n, str):
			if n.isdigit():
				n = int(n)
			else:
				result = 0
				for char in n.upper():
					if not (chars[0] <= char <= chars[-1]):
						raise ValueError("Input must be {chars[0]}-{chars[-1]}")
					result = result * base + (ord(char) - ord(chars[0]) + 1)
				return result

		if isinstance(n, int):
			if n <= 0:
				raise ValueError("Number must be positive")

			result = []
			while n > 0:
				n -= 1
				result.append(chr(ord(chars[0]) + (n % base)))
				n //= base

			return "".join(reversed(result))

		raise TypeError("Input must be int or str")

	#-=-=-=-#
	# XML transformations

	def _convert_suffix(self, name: str, reverse: bool = True) -> str:
		if reverse:
			return re.sub(
				r"([A-Za-z\-]+?)([A-Z]+)$",
				lambda m: f"{m.group(1)}{self._reverse_numeral(m.group(2))}",
				name
			)

		return re.sub(
			r"(\d+)$",
			lambda m: self._convert_numeral(int(m.group(1))),
			name
		)

	def _convert_ref(self, value: str, group: str, reverse: bool = False) -> str:
		if reverse:
			return re.sub(
				fr"({group})([A-Z]+)\b",
				lambda m: f"{m.group(1)}{self._reverse_numeral(m.group(2))}",
				value
			)

		return re.sub(
			fr"({group})(\d+)\b",
			lambda m: f"{m.group(1)}{self._convert_numeral(int(m.group(2)))}",
			value
		)

	def _process(self):
		remove_empty = getattr(self, "_remove_empty_attributes", False)
		empty_values = getattr(self, "_empty_attributes", ("0", "", None))

		for elem in self.root.iter():
			elem.tag = self._convert_suffix(elem.tag)

			new_attrib = []

			for k, v in elem.attrib.items():
				for prefix in self._ATTRS_NAMED:
					if k.startswith(prefix):
						v = self._convert_ref(v, prefix)

				if remove_empty and v in empty_values:
					continue

				new_attrib.append((k, v))

			elem.attrib.clear()
			for k, v in new_attrib:
				elem.attrib[k] = v

	#-=-=-=-#
	# immutable optimization pipeline

	def _serialize(self) -> str:
		char_indent = getattr(self, "_char_indent", "\t")
		char_newline = getattr(self, "_char_newline", "\n")
		normalize_header = getattr(self, "_normalize_header", True)
		self_close_cleanup = getattr(self, "_self_close_cleanup", True)
		empty_attributes = getattr(self, "_empty_attributes", {"0", ""})
		remove_empty_attributes = getattr(self, "_remove_empty_attributes", True)

		if remove_empty_attributes:
			for elem in self.root.iter():
				new_attrib = {}

				for k, v in elem.attrib.items():
					if v in empty_attributes:
						continue

					new_attrib[k] = v

		elem.attrib.clear()
		elem.attrib.update(new_attrib)

		xml_bytes = etree.tostring(
			self.root,
			pretty_print = True,
			xml_declaration = True,
			encoding = "UTF-8"
		)

		xml_str = xml_bytes.decode("UTF-8")

		if not char_newline.strip():
			xml_str = re.sub(r">\s+<", "><", xml_str)
		
		lines = xml_str.splitlines()

		# replace indentation
		formatted = []
		for line in lines:
			leading_spaces = len(line) - len(line.lstrip(" "))
			level = leading_spaces // 2
			formatted.append(char_indent * level + line.lstrip())

		xml_str = char_newline.join(formatted)

		if normalize_header:
			xml_str = re.sub(
				r"<\?xml[^>]+\?>",
				'<?xml version="1.0"?>',
				xml_str,
				count = 1
			)

		if self_close_cleanup:
			xml_str = re.sub(r"\s*/>", "/>", xml_str)

		return xml_str

	def optimize(
		self,
		char_indent = "\t",
		char_newline = "\n",
		normalize_header = True,
		self_close_cleanup = True,
		empty_attributes = _ATTRS_EMPTY,
		remove_empty_attributes = True,
	):
		new_model = self._clone()

		# store config
		new_model._char_indent = char_indent
		new_model._char_newline = char_newline
		new_model._normalize_header = normalize_header
		new_model._self_close_cleanup = self_close_cleanup
		new_model._remove_empty_attributes = remove_empty_attributes
		new_model._empty_attributes = empty_attributes

		new_model._process()
		return new_model

	def deoptimize(self):
		new_model = self._clone()

		for elem in new_model.root.iter():

			# reverse tag encoding
			elem.tag = new_model._convert_suffix(elem.tag, True)

			new_attrib = []

			for k, v in elem.attrib.items():
				for prefix in new_model._ATTRS_NAMED:
					if k.startswith(prefix):
						v = new_model._convert_ref(v, prefix, True)

				new_attrib.append((k, v))

			elem.attrib.clear()
			for k, v in new_attrib:
				elem.attrib[k] = v

		return new_model

	def save(self, file_output = None):
		target = file_output or self.file_input

		with open(target, "w", encoding = "UTF-8") as f:
			f.write(self._serialize())

	#-=-=-=-#
	# classification

	def is_skeleton(self) -> bool:
		try:
			for node, attrs in self._RULE_SKELETON.items():
				elem = self.root.find(f"./{self._NODES_SCENE[0]}/{node}")

				if elem is None:
					return False

				for attr in attrs:
					if attr not in elem.attrib:
						return False

			return True
		except Exception:
			return False

	def is_object(self) -> bool:
		try:
			for node, attrs in self._RULE_OBJECT.items():
				elem = self.root.find(f"./{self._NODES_SCENE[0]}/{node}")

				if elem is None:
					return False

				for attr in attrs:
					if attr not in elem.attrib:
						return False

			return True
		except Exception:
			return False

	def is_skin(self) -> bool:
		try:
			has_scene = all(
				self.root.find(f"./{node}") is not None
				for node in self._NODES_SCENE
			)

			return self.is_skeleton() or (has_scene and not self.is_object())
		except Exception:
			return False

	def classify(self):
		if self.is_skeleton():
			return Skeleton(self)

		if self.is_skin():
			return Skin(self)

		if self.is_object():
			return Object(self)

		return None

	#-=-=-=-#
	# properties

	@property
	def file(self):
		return self.file_input

	@property
	def skeleton(self):
		return self.is_skeleton()

	@property
	def skin(self):
		return self.is_skin()

	@property
	def object(self):
		return self.is_object()

	@property
	def identity(self):
		return self.classify()

	@property
	def inner(self):
		return etree.tostring(self.root)

#-=-=-=-#
# usage

file_input = os.sys.argv[1]
file_output = "_opt".join(os.path.splitext(file_input)) # change

model = Model(file_input)
old_file = os.path.getsize(file_input)

try:
	print("Model type:", model.identity)
	model_opt = model.optimize(
		char_newline = "",
		char_indent = "\t"
	)
	model_opt.save(file_output)
except Exception as e:
	print(e)

new_file = os.path.getsize(file_output)

reduction = ((old_file - new_file) / old_file) * 100

if reduction:
	print("Optimized [-{:.2f}% size]".format(reduction))
else:
	print("File already optimized")