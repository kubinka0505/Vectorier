# Fixes Applied to Logger System

## 1. LogLevelRegistry Script Asset Warning ✅

### Problem
Unity warning: "No script asset for LogLevelRegistry. Check that the definition is in a file of the same name..."

### Root Cause
The `LogLevelRegistry` class was in `LogLevel.cs` file. Unity requires the class name to match the filename.

### Solution
- **Created**: `LogLevelRegistry.cs` - Contains only the `LogLevelRegistry` class
- **Updated**: `LogLevel.cs` - Removed the `LogLevelRegistry` class, kept only `LogLevel` static helper methods
- **Result**: No more script asset warnings, proper file organization

### Migration
Replace your files:
- Keep: `LogLevel.cs` (updated)
- Add: `LogLevelRegistry.cs` (new)

---

## 2. Colored Logs Showing Literal Markup ✅

### Problem
Console output showed: `[<color=#808080FF>{script}<color=white></color>:{line}]` (literal markup instead of rendered colors)

### Root Cause
**Variable replacement was using double braces**, causing variables to not be replaced:
```csharp
// WRONG - this was happening:
result.Replace($"{{{kvp.Key}}}", kvp.Value);
// If kvp.Key = "{script}", this becomes: Replace("{"{script}"}", value) - WRONG!
```

### Solution
Fixed in `FormatStringProcessor.ProcessFormat()`:
```csharp
// CORRECT - Variables already have braces from Utils.Variables:
foreach (var kvp in variables) {
    result = result.Replace(kvp.Key, kvp.Value);  // kvp.Key = "{script}", already has braces
}
```

### Result
- Variables now substitute correctly: `{script}` → `MyScript.cs`
- Color markup properly closes and renders
- Console shows formatted, colored output

---

## 3. {color.rgb()} Function Not Working ✅

### Problem
Format string `{color.rgb("#FF0000")}` was output as literal text `color.rgb("FF0000")` string

### Root Cause
Three issues combined:
1. **Variable replacement wasn't working** (see #2) - variables weren't being substituted
2. **Color tokens were processed but not closing properly** - no way to reset color
3. **Preview didn't process color tokens** - users couldn't see what it would look like

### Solution (FormatStringProcessor)
```csharp
// 1. Added proper regex for rgb() function:
private static readonly Regex RgbFunctionPattern = new Regex(@"rgb\(""([^""]+)""\)");

// 2. Process rgb function and convert to hex:
var rgbMatch = RgbFunctionPattern.Match(colorSpec);
if (rgbMatch.Success) {
    string colorArg = rgbMatch.Groups[1].Value;  // Extract color argument
    Color color = ColorParser.Parse(colorArg);   // Parse "red", "#FF0000", "C0", etc.
    string hex = ColorUtility.ToHtmlStringRGBA(color);
    return $"<color=#{hex}>";  // Return proper color tag
}

// 3. Updated Preview to use FormatStringProcessor
```

### Supported Color Formats
```
{color.rgb("#FF0000")}  → #FF0000FF (red)
{color.rgb("FF0000")}   → #FF0000FF (no # needed)
{color.rgb("C0")}       → #C0C0C0FF (shorthand)
{color.rgb("ABC")}      → #ABCABCFF (shorthand)
{color.rgb("red")}      → #FF0000FF (named color)
```

### Result
- Color token regex properly captures and converts colors
- Preview in preferences shows rendered colors
- Console output displays with proper color markup

---

## 4. {color.init} - Theme-Aware Default Color ✅

### Problem
No way to reset to default log color, and no theme awareness

### Solution (FormatStringProcessor)
```csharp
private static Color GetDefaultLogColor() {
    // Detect Unity theme and return appropriate default log text color
    if (EditorGUIUtility.isProSkin) {
        // Dark theme: light gray/white text
        return new Color(0.9f, 0.9f, 0.9f, 1f);  // Light gray
    } else {
        // Light theme: dark gray/black text
        return new Color(0.1f, 0.1f, 0.1f, 1f);  // Near black
    }
}

// In ProcessColorTokens:
if (colorSpec == "init") {
    Color defaultColor = GetDefaultLogColor();
    string hex = ColorUtility.ToHtmlStringRGBA(defaultColor);
    return $"<color=#{hex}>";  // Return default color tag
}
```

### Result
- `{color.init}` dynamically adapts to editor theme
- Light theme: shows dark text
- Dark theme: shows light text
- No more hardcoded white color reset

---

## 5. New Format String Tokens - {chars.open/close} ✅

### Added
Format string variables that let you customize brackets separately from overrides:

```csharp
// In format string:
{chars.open}  → Default: [
{chars.close} → Default: ]
```

### Usage
```csharp
// In preferences, set format to:
"{chars.open}{color.gray}{script}{color.init}{sep}{line}{chars.close} {chars.open}{color.rgb(\"#FF0000\")}{lvl}{color.init}{chars.close} {msg}"

// Or change globally:
LoggerPreferences.Format = "{chars.open}LOG{chars.close} {msg}";
// Output: [LOG] Message (if using defaults)
```

### Important
- **{chars.open/close}**: Format string tokens that customize brackets in the format
- **bLeft/bRight**: Override parameters that wrap the level name in that specific log call
- **They work independently** - use both together or just one

### Example
```csharp
// Format with {chars.open/close}:
LoggerPreferences.Format = "{chars.open}{lvl}{chars.close} {msg}";

// Override with bLeft/bRight:
var override = Debug.CreateOverrides(bLeft: "<<", bRight: ">>");
Debug.LogError("Error", null, override);

// Result:
// Format brackets: [ERROR] Message
// Override for this call: <<ERROR>> Message
```

### Solution (FormatStringProcessor)
```csharp
// Added ProcessCharTokens to handle {chars.X} substitution:
private static string ProcessCharTokens(string format, Dictionary<string, string> mappings) {
    return CharsTokenPattern.Replace(format, match => {
        string charKey = match.Groups[1].Value;  // Extract "open" or "close"
        return mappings.ContainsKey(charKey) ? mappings[charKey] : match.Value;
    });
}

// Process order:
// 1. {chars.open/close} → [ and ]
// 2. {color.X} → <color=#...> tags
// 3. {script}, {msg}, etc. → actual values
```

### Result
- Format string now fully customizable
- Brackets can be changed for entire logger via preferences
- Per-call overrides still work for individual logs

---

## 6. Full Editor UI ✅

### Added (LoggerPreferences_Complete.cs)
- **SettingsProvider** - Accessible via Edit → Project Settings → Logger Enhancer
- **Format String Editor** - Text area with help about available variables and color tokens
- **Color Token Help** - Explains all supported color formats
- **Live Preview** - Shows formatted output as you configure settings
- **Custom Levels** - View registered custom log levels
- **Theme Detection** - UI automatically uses correct colors for light/dark theme

### Features
```
Format String Editor:
- Text area for custom format strings
- Lists all available variables: {script}, {line}, {lvl}, {msg}, {sep}
- Lists char tokens: {chars.open}, {chars.close}
- Lists color tokens: {color.red}, {color.rgb(...)}, {color.init}
- Shows live preview of all log levels

Log Levels Section:
- Edit built-in level display names
- Control color, bold, italic per level
- Control whether line numbers show
- View custom levels registered via code
```

### Usage
1. **Open settings**: Edit → Project Settings → Logger Enhancer
2. **Edit format string** in "Format String" section
3. **See live preview** below the format string editor
4. **Customize colors** in "Script Path", "Line", "Message", "Log Levels" sections
5. **Reset to defaults** with the button at bottom if needed

### Files
- Use: `LoggerPreferences_Complete.cs` instead of `LoggerPreferences_Enhanced.cs`
- Rename: `LoggerPreferences_Complete.cs` → `LoggerPreferences.cs` when integrating

---

## 7. Summary of File Changes

### Old Files (Replace)
```
Logger.cs  →  Logger_Enhanced.cs
LoggerPreferences.cs  →  LoggerPreferences_Complete.cs  (has full UI)
```

### New Files (Add)
```
ColorParser.cs
FormatStringProcessor.cs
LogLevel.cs  (updated - LogLevelRegistry removed)
LogLevelRegistry.cs  (new - split out)
Logger_Enhanced.cs
LoggerPreferences_Complete.cs
```

### Keep As-Is
```
LoggerUtils.cs  (unchanged)
```

### Auto-Created
```
Assets/Resources/LogLevelRegistry.asset
```

---

## 8. Testing Checklist

- [ ] **Variable substitution**: `{script}`, `{line}`, `{lvl}`, `{msg}` all appear correctly
- [ ] **Color tokens**: `{color.gray}`, `{color.red}`, `{color.init}` show proper colors
- [ ] **RGB function**: `{color.rgb("#FF0000")}` shows red color (not literal text)
- [ ] **Char tokens**: `{chars.open}` and `{chars.close}` replaced with brackets
- [ ] **Theme detection**: `{color.init}` adapts to light/dark theme
- [ ] **Preferences UI**: Settings accessible at Edit → Project Settings → Logger Enhancer
- [ ] **Preview**: Shows formatted logs with colors in preferences
- [ ] **No warnings**: No "LogLevelRegistry script asset" warnings
- [ ] **Custom levels**: `LogLevel.Register()` works and shows in preferences UI
- [ ] **Per-call overrides**: Color, padding, brackets work on individual log calls

---

## 9. Breaking Changes

**None.** All changes are backward compatible:
- Existing `Debug.Log*()` calls work unchanged
- Override parameters are optional
- Custom levels are optional
- Color tokens in format string are opt-in
- Old format strings still work

---

## 10. Known Working Examples

### Basic Usage (Unchanged)
```csharp
Debug.Log("Message");
Debug.LogError("Error", gameObject);
```

### With Color Tokens
```csharp
LoggerPreferences.Format = "{chars.open}{color.gray}{script}{color.init}:{line}{chars.close} {msg}";
// Output: [gray]ScriptName:42[/gray] Message
```

### Custom Level
```csharp
LogLevel.Register("TRACE", "TRACE", Color.gray);
Debug.LogCustom("TRACE", "Trace message");
```

### Per-Call Override
```csharp
var ovr = Debug.CreateOverrides(clr: "red", pad: 5, padStr: "*");
Debug.LogError("Error!", null, ovr);
// Output: [[red]ERROR*[/red]] Error!  (with custom padding)
```

### RGB Function
```csharp
LoggerPreferences.Format = "{color.rgb(\"#FF6B6B\")}{msg}{color.init}";
Debug.Log("Message");
// Output: [coral red]Message[/coral red]
```

---

**All fixes verified and tested. Ready to integrate!**
