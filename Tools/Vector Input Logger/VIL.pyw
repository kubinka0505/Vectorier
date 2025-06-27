import os
import time
import random
import datetime
import platform
import threading
import subprocess
import tkinter as tk
from pynput import keyboard
from plyer import notification
from tkinter import filedialog as fd, messagebox as msgbox

#-=-=-=-#
# Metadata

__title__   = "Vector Inputs Logger"
__author__  = "kubinka0505"
__version__ = "1.0"
__date__    = "27th October 2024"

#-=-=-=-#
# Variables

IS_WIN = platform.system() == "Windows"

BASE_DIR = os.path.abspath(os.path.dirname(__file__))
DATE_NOW = datetime.datetime.now().strftime("%d-%m-%Y %H-%M-%S")

DEFAULT_OUTPUT_CONFIG = "Data/._default_path"
DEFAULT_OUTPUT = "_log_outputs"

if os.path.exists(DEFAULT_OUTPUT_CONFIG):
	with open(DEFAULT_OUTPUT_CONFIG, "r") as file:
		DEFAULT_PATH = os.path.join(file.readlines()[0], DEFAULT_OUTPUT)
		DEFAULT_PATH = os.path.abspath(DEFAULT_PATH)
else:
	DEFAULT_PATH = BASE_DIR

DEFAULT_PATH = os.path.abspath(DEFAULT_PATH)

DEFAULT_COUNTDOWN = 3

APP_ICON = "Data/Icon.ico"
GAME_PROCESS_BASENAME = "Vector.exe"

#-=-=-=-#
# Functions
def process_running(process_name: str) -> bool:
	call = "TASKLIST", "/FI", "imagename eq " + process_name
	output = subprocess.check_output(call).decode("latin-1")
	last_line = output.strip().split("\r\n")[-1]
	return last_line.lower().startswith(process_name.lower())

class COLORS:
	LABEL_FG = "#FFF"
	LABEL_BG = "#2E2E2E"
	ENTRY_BG = "#444"
	BUTTON_FG = "#FFF"
	BUTTON_BG = "#555"

class InputsLoggerApp:
	def __init__(self, root):
		self.root = root
		self.root.title(__title__)
		self.root.geometry("460x148")
		self.root.resizable(False, False)
		self.root.configure(bg = COLORS.LABEL_BG)

		# Set custom icon
		try:
			self.root.iconbitmap(APP_ICON)
		except Exception as Error:
			print(f"Could not load icon: {Error}")

		# Initialize variables
		self.output_dir = tk.StringVar(value = DEFAULT_PATH)
		self.countdown = tk.IntVar(value = DEFAULT_COUNTDOWN)
		self.start_time = None
		self.no_game_log = False
		self.log_data = []
		self.logging = False

		# Layout setup
		self.create_layout()

		# Set up keyboard listener
		self.listener = None

	def create_layout(self):
		# Top frame for "Output file directory" section
		top_frame = tk.Frame(self.root, bg = COLORS.LABEL_BG, padx = 10, pady = 5)
		top_frame.grid(row = 0, column = 0, sticky = "ew", padx = 5, pady = 5)
		
		tk.Label(top_frame, text = "Output file directory\t", bg = COLORS.LABEL_BG, fg = COLORS.LABEL_FG).grid(row = 0, column = 0, sticky = "w")
		tk.Entry(top_frame, textvariable = self.output_dir, width = 32, bg = COLORS.ENTRY_BG, fg = COLORS.LABEL_FG).grid(row = 0, column = 1, padx = 5)
		tk.Button(top_frame, text = "Browse", command = self.browse_directory, width = 8, bg = COLORS.BUTTON_BG, fg = COLORS.BUTTON_FG).grid(row = 0, column = 2, padx = 5)

		# Middle frame for "Countdown" section
		middle_frame = tk.Frame(self.root, bg = COLORS.LABEL_BG, padx = 10, pady = 5)
		middle_frame.grid(row = 1, column = 0, sticky = "ew", padx = 5, pady = 5)
		
		tk.Label(middle_frame, text = "Countdown (seconds)\t", bg = COLORS.LABEL_BG, fg = COLORS.LABEL_FG).grid(row = 0, column = 0, sticky = "w")
		tk.Entry(middle_frame, textvariable = self.countdown, width = 32, bg = COLORS.ENTRY_BG, fg = COLORS.LABEL_FG).grid(row = 0, column = 1, padx = 5)

		# Add increment/decrement buttons for countdown
		tk.Button(middle_frame, text = "-", command = self.decrement_countdown, width = 3, bg = COLORS.BUTTON_BG, fg = COLORS.BUTTON_FG).grid(row = 0, column = 2, padx = (5, 0))
		tk.Button(middle_frame, text = "+", command = self.increment_countdown, width = 3, bg = COLORS.BUTTON_BG, fg = COLORS.BUTTON_FG).grid(row = 0, column = 3, padx = 5)

		# Bottom frame for START/STOP buttons
		bottom_frame = tk.Frame(self.root, bg = COLORS.LABEL_BG, padx = 10, pady = 10)
		bottom_frame.grid(row = 2, column = 0, sticky = "ew", padx = 5, pady = (10, 5))
		
		tk.Button(bottom_frame, text = "Start logging", command = self.start_logging, width = 28, bg = COLORS.BUTTON_BG, fg = COLORS.BUTTON_FG).grid(row = 0, column = 0, padx = 5)
		tk.Button(bottom_frame, text = "Stop logging", command = self.stop_logging, width = 28, bg = COLORS.BUTTON_BG, fg = COLORS.BUTTON_FG).grid(row = 0, column = 1, padx = 5)

	def browse_directory(self):
		directory = fd.askdirectory(
			initialdir = DEFAULT_PATH,
			title = "Select output directory"
		)

		if directory:
			directory = directory.replace("/", os.sep)

			with open(DEFAULT_OUTPUT_CONFIG, "w") as file:
				file.write(directory)

			directory = os.path.join(directory, DEFAULT_OUTPUT)
			self.output_dir.set(directory)

	def increment_countdown(self):
		cd = self.countdown

		if cd.get() < 1:
			cd.set(1)
		elif cd.get() > 0:
			cd.set(cd.get() + 1)

	def decrement_countdown(self):
		cd = self.countdown

		if cd.get() < 1:
			cd.set(1)
		elif cd.get() > 0:
			val = cd.get() - 1
			if val == 0:
				cd.set(1)
			else:
				cd.set(cd.get() - 1)

	def start_logging(self):
		if not process_running(GAME_PROCESS_BASENAME):
			_msg = "Game is not open."

			print(_msg, end = "\r")

			msgbox.showerror(
				"Error",
				"Game is not open."
			)

			self.no_game_log = True
			return

		if self.no_game_log:
			print()

		countdown = self.countdown.get()
		threading.Thread(target = self.run_countdown, args = (countdown,)).start()
	
	def run_countdown(self, countdown):
		# Countdown
		for i in range(countdown, 0, -1):
			print("\r", end = f"Starting in {i - 1} seconds...")
			time.sleep(1)
		print()

		# Notify user
		if IS_WIN:
			notification.notify(
				title = __title__,
				app_name = os.path.basename(__file__),
				app_icon = APP_ICON,
				message = "Key logging started!",
				ticker = "Press arrow keys.",
				timeout = 5
			)
		else:
			print("\a")

		# Start timing and logging
		self.start_time = time.time()
		self.log_data = []
		self.logging = True

		print("Logging started. Press arrow keys.")
		print()

		# Start global listener
		self.listener = keyboard.Listener(on_press = self.log_key)
		self.listener.start()
		
	def log_key(self, key):
		if not self.logging or self.start_time is None:
			# Do nothing if logging hasn't started or is stopped
			return
		
		# Map only arrow keys to readable names
		try:
			if key == keyboard.Key.up:
				self.record_key("Up")
			elif key == keyboard.Key.down:
				self.record_key("Down")
			elif key == keyboard.Key.left:
				self.record_key("Left")
			elif key == keyboard.Key.right:
				self.record_key("Right")
		except AttributeError:
			pass
	
	def record_key(self, key):
		timestamp = time.time() - self.start_time
		self.log_data.append(f"{key}: {timestamp:.4f}")
		print(f"{key}: {timestamp:.4f}")

	def stop_logging(self):
		if self.logging:
			self.logging = False

			if self.listener:
				# Stop the listener
				self.listener.stop()

			self.save_log()
			print("Logging stopped and data saved.")

	def save_log(self):
		if not self.log_data:
			print("No data to save.")
			return

		# Generate random filename
		random_number = random.randint(1000, 9999)
		filename = " ".join(("Log", str(DATE_NOW), str(random_number))) + ".txt"
		filename = filename.replace(" ", "_")

		# Save log data to the specified output directory
		output_dir = os.path.abspath(self.output_dir.get())
		output_path = os.path.join(output_dir, filename)

		os.makedirs(os.path.dirname(output_path), exist_ok = True)

		self.final_output_file = output_path

		with open(output_path, "w") as file:
			for line in self.log_data:
				file.write(line)

				if line != self.log_data[-1]:
					file.write("\n")

		print(f'Log saved to: "{output_path}"')

	def on_close(self):
		self.stop_logging()
		self.root.destroy()

	def cleanup(self):
		"""Cleanup on KeyboardInterrupt."""
		self.stop_logging()
		print("Application closed due to Keyboard Interrupt.")

		return 1

# Run the application
root = tk.Tk()
app = InputsLoggerApp(root)

# Ensure the application saves logs on close
root.protocol("WM_DELETE_WINDOW", app.on_close)

# Catch KeyboardInterrupt
try:
	root.mainloop()
except KeyboardInterrupt:
	app.cleanup()

raise SystemExit()