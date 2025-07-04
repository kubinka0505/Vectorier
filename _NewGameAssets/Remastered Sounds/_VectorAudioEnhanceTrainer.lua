-- Variable for the game process name
local resTbl = {}
local resindex = 0

-- # little endian hex = int

-- 22 56 00 00 = 22050
-- 40 9C 00 00 = 40000
-- 44 AC 00 00 = 44100
-- FE A6 00 00 = 42750 <- may sound close to the original (PC only; assuming files samplerate are 44 100)
-- 80 BB 00 00 = 48000
local targetValue = "44 AC 00 00"

--==--==--==--

-- Function to show a waiting dialog
local function showWaitingDialog()
	waitingDialog = createForm(false)
	waitingDialog.BorderStyle = "bsDialog"
	waitingDialog.Position = "poScreenCenter"
	waitingDialog.Width = 300
	waitingDialog.Height = 100
	waitingDialog.Caption = "Waiting for game to launch..."
	waitingDialog.Show()
end

-- Function to hide the waiting dialog
local function hideWaitingDialog()
	if waitingDialog ~= nil then
		waitingDialog.Close()
		waitingDialog = nil
	end
end

-- Function to replace bytes in memory
local function replaceBytes(address, from, to)
	autoAssemble(address .. ":\ndb " .. to)
end

-- Function to perform multiple replacements
local function multiReplace(processName, from, to)
	-- Clear the result table
	resTbl = {}
	local processID = getProcessIDFromProcessName(processName)

	if processID == nil then
		showWaitingDialog()
		-- Wait for the game process to be detected
		while processID == nil do
			processID = getProcessIDFromProcessName(processName)
			-- Wait for 1 second
			sleep(1000)
		end
		hideWaitingDialog()
	end

	-- Wait for additional time after game launch (adjust as needed)
	-- 10 seconds delay after game process detected
	sleep(10000)

	-- Open the game process
	openProcess(processName)

	-- Perform memory scan and replace
	local aob = AOBScan(from, "+W")
	if aob == nil or aob.Count == 0 then
		-- Perform another check after waiting
		-- Wait an additional 5 seconds (adjust as needed)
		sleep(5000)
		aob = AOBScan(from, "+W")
		if aob == nil or aob.Count == 0 then
			showMessage("Code pattern not found!")
			return
		end
	end

	-- Perform replacement
	for i = 0, aob.Count - 1 do
		local address = aob[i]
		replaceBytes(address, from, to)
	end

	aob.Destroy()
end

-- Example usage
multiReplace("Vector.exe", "22 56 00 00", targetValue)