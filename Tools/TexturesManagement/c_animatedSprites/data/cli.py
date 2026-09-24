import os
import argparse

#-=-=-=-#

def build_parser(
	directory_images_output: str
) -> argparse.ArgumentParser:
	parser = argparse.ArgumentParser(
		prog = "packer",
		description = "Texture atlas encoder/decoder"
	)

	sub = parser.add_subparsers(
		dest = "mode",
		required = True
	)

	# ENCODE
	encode = sub.add_parser(
		"encode",
		help = "Encode (Images -> Atlas)",
		add_help = False
	)

	req = encode.add_argument_group("Required arguments")
	opt = encode.add_argument_group("Optional arguments")
	sw  = encode.add_argument_group("Switch arguments")

	req.add_argument(
		"-i", "--input-file",
		nargs = "+",
		type = str,
		metavar = str,
		help = "Input files - accepts multiple files incl. directories with frames and GIF images."
	)
	req.add_argument(
		"-n", "--name-atlas",
		type = str,
		metavar = str,
		help = "Name for atlas."
	)

	req.add_argument(
		"-f", "--output-format",
		choices = ["plist", "json"],
		default = "plist",
		required = False,
		help = "Output mapping format."
	)

	opt.add_argument(
		"-o", "--output-directory",
		type = str,
		metavar = str,
		default = directory_images_output,
		help = "Output directory for encoded atlas file and its mapping file."
	)

	opt.add_argument(
		"-p", "--padding",
		type = int,
		metavar = int,
		default = 2,
		help = "Safe images padding in pixels."
	)

	sw.add_argument(
		"-nw", "--no-write",
		action = "store_true",
		help = "Doesn't write files."
	)
	sw.add_argument(
		"-no", "--no-optimize",
		action = "store_true",
		help = "Doesn't optimize output atlas image and doesn't prettify its mapping."
	)

	sw.add_argument(
		"-h", "--help",
		action = "help",
		help = "Display this message and exit."
	)

	# ---------------- DECODE ----------------
	decode = sub.add_parser(
		"decode",
		help = "Decode (Atlas -> Images)",
		add_help = False
	)

	req = decode.add_argument_group("Required arguments")
	opt = decode.add_argument_group("Optional arguments")
	sw  = decode.add_argument_group("Switch arguments")

	req.add_argument(
		"-i", "--input-file",
		nargs = "+",
		type = str,
		metavar = str,
		help = "Input file. Accepts both mapping file or an atlas file, if its map counterpart exists in the same directory."
	)

	opt.add_argument(
		"-o", "--output-directory",
		type = str,
		metavar = str,
		default = "_decoded",
		help = f"Output parent directory for decoded images."
	)
	opt.add_argument(
		"-d", "--frame-rate",
		type = int,
		metavar = int,
		default = 0,
		help = "If above 0, outputs GIF image instead of frames with this framerate to --output-directory."
	)

	sw.add_argument(
		"-no", "--no-optimize",
		action = "store_true",
		help = "Disable image optimization."
	)
	sw.add_argument(
		"-h", "--help",
		action = "help",
		help = "Display this message and exit."
	)

	for group in [encode, decode]:
		for action in group._actions:
			if getattr(action, "required", False):
				action.required = True

	return parser