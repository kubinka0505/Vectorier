import os
import json
import shutil

#-=-=-=-#
# Metadata

__author__  = "kubinka0505"
__credits__ = __author__
__date__    = "14th July 2024"

#-=-=-=-#
# Setup

# Load data from the JSON file
tricks   = "tricks.json"
basefile = "Trick_AI_X_.prefab"
outdir   = "generated"

# Max AI number directories to generate
ai_nums_max = 10

#-=-=-=-#
# Main

rev_str = "Reversed"
rev_suffix = "_R"
data = json.load(open(tricks, "r"))

# Iterate through the AI numbers
for ai_num in range(-1, ai_nums_max + 1):
	if ai_num == -1:
		ai_num_str = "All" # "_-1"
	else:
		ai_num_str = str(ai_num)

	newdir = os.path.join(outdir, f"AI_{ai_num_str}")
	newdir_reversed = os.path.join(newdir, rev_str)

	# Remove main directory
	if os.path.exists(newdir):
		shutil.rmtree(newdir)

	# Create directories
	os.makedirs(newdir, exist_ok = True)
	os.makedirs(newdir_reversed, exist_ok = True)

	# Iterate through the key-value pairs in the data
	for key, value in data.items():
		sep = "Start"
		key_display = key.split(sep)[0]
		value_str = str(value)

		# Create the new file path
		newname = basefile.replace("X", ai_num_str)

		newname = key_display.join( os.path.splitext( os.path.basename(newname) ) )
		newname_reversed = rev_suffix.join( os.path.splitext( os.path.basename(newname) ) )
		print(newname_reversed)
		
		newfile_normal = os.path.join(newdir, newname)
		newfile_reversed = os.path.join(newdir_reversed, newname_reversed)

		#-=-=-=-#
		# Non-reversed

		# Copy the base file to the new file path
		shutil.copy2(basefile, newfile_normal)

		# Read the content of the new file
		with open(newfile_normal, encoding = "UTF-8", mode = "r") as file:
			content = file.read()

		# Replace placeholders with actual values
		new_content = content \
			.replace("TRICK_NAME_HERE", key) \
			.replace("TRICK_FRAME_HERE", value_str) \
			.replace("AI-INT-HERE", str(ai_num)) \
			.replace("IS_REVERSED_HERE", "0")

		# Write the new content back to the file
		with open(newfile_normal, encoding = "UTF-8", mode = "w") as file:
			file.write(new_content)

		#-------------------------#

		#-=-=-=-#
		# Reversed

		# Copy the base file to the new file path
		shutil.copy2(basefile, newfile_reversed)

		# Read the content of the new file
		with open(newfile_reversed, encoding = "UTF-8", mode = "r") as file:
			content = file.read()

		# Replace placeholders with actual values
		new_content = content \
			.replace("TRICK_NAME_HERE", key) \
			.replace("TRICK_FRAME_HERE", value_str) \
			.replace("AI-INT-HERE", ai_num_str) \
			.replace("IS_REVERSED_HERE", "1")

		# Write the new content back to the file
		with open(newfile_reversed, encoding = "UTF-8", mode = "w") as file:
			file.write(new_content)

print("\a", end = "\r")