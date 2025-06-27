import os

template = r"template.prefab"
zoom_text = "ZOOM_VALUE"
new_file_name = "TriggerZoom{value}.prefab"

for x in os.listdir():
	if x.lower().startswith("triggerzoom"):
		if not ".meta" in x.lower():
			os.remove(x)

with open(template, "r", encoding = "U8") as old_file:
	content = old_file.read()

for counter in range(120 + 1):
	if counter not in (0, 65, 80, 100, 110):
		with open(new_file_name.format(value = str(counter)), "w", encoding = "u8") as new_file:
			new_content = content.replace(zoom_text, str(round(counter / 100, 3)))
			new_file.write(new_content)