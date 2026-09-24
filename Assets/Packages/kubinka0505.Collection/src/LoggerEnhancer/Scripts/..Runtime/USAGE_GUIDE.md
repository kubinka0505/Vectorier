# Enhanced Logger System - Usage Guide

## Overview
This enhanced logging system adds three major features:
1. **Advanced format strings** with color tokens and variable substitution
2. **Custom log levels** via a scriptable registry
3. **Per-call overrides** for dynamic formatting on individual log calls

---

## 1. Advanced Format Strings

### Color Tokens
The format string now supports color tokens in the format `{color.XXX}`:

```csharp
// Named colors
{color.red}      → #FF0000FF
{color.gray}     → #808080FF
{color.blue}     → #0000FFFF
{color.init}     → reset to white

// Hex notation (with auto-expansion)
{color.rgb("FF0000")}    → #FF0000FF (standard 6-char hex)
{color.rgb("C0")}        → #C0C0C0FF (2-char → C0C0C0)
{color.rgb("ABC")}       → #ABCABCFF (3-char → ABCABC)
{color.rgb("ABCD")}      → #ABCDABFF (4-char → ABCDAB)
{color.rgb("ABCDE")}     → #ABCDEAFF (5-char → ABCDEA)

// Named colors via rgb()
{color.rgb("red")}       → #FF0000FF
{color.rgb("lime")}      → #00FF00FF
```

### Standard Variables
```csharp
{script}         → script name or path
{sep}            → separator (default ":")
{line}           → line number
{lvl}            → level display name
{msg}            → message text
```

### Example Format Strings

```csharp
// Simple
"[{script}:{line}] {msg}"

// With colors and brackets
"[{color.gray}{script}{color.init}:{line}] [{color.red}{lvl}{color.init}] {msg}"

// Complex example
"[{color.rgb("4AFF")}{script}{color.gray}{sep}{line}{color.init}] [{lvl}] {color.rgb("ABCDEF")}{msg}{color.init}"
```

---

## 2. Custom Log Levels

### Register Custom Level

```csharp
// Simple registration
LogLevel.Register("TRACE", "TRACE", Color.gray);

// With full styling
LogLevel.Register(
    "CUSTOM",
    "CUSTOM LEVEL",
    new Color(1f, 0.5f, 0f, 1f),  // color
    bold: true,
    italic: false,
    displayLine: true
);
```

### Use Custom Level

```csharp
Debug.LogCustom("TRACE", "This is a trace message");
Debug.LogCustom("CUSTOM", "Custom level message", gameObject);
```

### Check if Level Exists

```csharp
if (LogLevel.Exists("TRACE")) {
    Debug.LogCustom("TRACE", "Tracing...");
}
```

### Unregister Level

```csharp
LogLevel.Unregister("TRACE");
```

---

## 3. Per-Call Overrides

### Override Structure
```csharp
public struct LogOverrides {
    public string name;       // Custom level display name
    public string clr;        // Color override
    public int? pad;          // Padding width
    public string padStr;     // Padding character
    public string bLeft;      // Left bracket
    public string bRight;     // Right bracket
}
```

### Usage Examples

#### Basic Override
```csharp
var overrides = new LogOverrides {
    name = "ERR_POINT",
    clr = "red",
    pad = 4,
    padStr = "0",
    bLeft = "{{",
    bRight = "}}"
};

Debug.LogError("Something failed", null, overrides);
// Output: {{ERR_00}} Something failed
```

#### Fluent Helper
```csharp
var overrides = Debug.CreateOverrides(
    name: "Err_POINT",
    clr: "red",
    pad: 4,
    padStr: "0",
    bLeft: "{{",
    bRight: "}}"
);

Debug.LogError("This is a log", gameObject, overrides);
```

#### Override Only Color
```csharp
var overrides = Debug.CreateOverrides(clr: "lime");
Debug.LogWarning("Important!", null, overrides);
```

#### Override Only Name and Padding
```csharp
var overrides = Debug.CreateOverrides(
    name: "WARN",
    pad: 8,
    padStr: "*"
);
Debug.LogWarning("Watch out!", null, overrides);
// Output: [WARN****] Watch out!
```

### Overload Methods
```csharp
// With context and overrides
Debug.LogError("message", context, overrides);
Debug.LogWarning("message", context, overrides);

// Without context
Debug.Log("message", overrides);
Debug.LogDebug("message", overrides);

// Via LogCustom for custom levels
Debug.LogCustom("CUSTOM", "message", context, overrides);
```

---

## 4. Complete Example

```csharp
public class LoggingExample : MonoBehaviour {
    void Start() {
        // Register custom levels
        LogLevel.Register("TRACE", "TRACE", Color.gray, bold: false);
        LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f, 1f), bold: true);

        // Standard logging
        Debug.Log("Application started");
        Debug.LogWarning("This is a warning");

        // Custom level
        Debug.LogCustom("TRACE", "Entering Start method");

        // Override color
        var redOverride = Debug.CreateOverrides(clr: "red");
        Debug.LogError("Critical error!", gameObject, redOverride);

        // Override with padding and brackets
        var formatted = Debug.CreateOverrides(
            name: "CRITICAL",
            clr: "#FF0000",
            pad: 10,
            padStr: "-",
            bLeft: "<<<",
            bRight: ">>>"
        );
        Debug.LogFatal("System failure", null, formatted);

        // Custom level with override
        var perfOverride = Debug.CreateOverrides(clr: "lime");
        Debug.LogCustom("PERF", "Operation completed in 42ms", null, perfOverride);
    }
}
```

---

## 5. Integration Notes

### File Structure
- `ColorParser.cs` - Color parsing utility (supports web colors, hex, RGBA)
- `FormatStringProcessor.cs` - Advanced format string processing with color tokens
- `LogLevel.cs` - Custom log level registration and registry
- `Logger_Enhanced.cs` - Enhanced Debug class with overrides support
- `LoggerPreferences_Enhanced.cs` - Updated preferences with custom level support

### Default Format String
The new default includes color tokens:
```csharp
"[{color.gray}{script}{color.init}:{line}] [{color.rgb("#FF0000")}{lvl}{color.init}] {msg}"
```

### Backward Compatibility
- All existing `Debug.Log*()` calls work unchanged
- Override parameters are optional
- Custom levels are completely optional
- Color tokens in format strings are opt-in

### Runtime vs Editor
- Per-call overrides only work in editor (where formatting happens)
- Custom log levels work in both editor and runtime
- Format string processing is editor-only (matches original behavior)

---

## 6. Common Patterns

### Error Logging with Stack Highlight
```csharp
Debug.LogError("Critical error", null, 
    Debug.CreateOverrides(clr: "red", bLeft: "[ERROR]", bRight: ""));
```

### Debug Logging with Custom Format
```csharp
Debug.LogDebug("Debug info", null,
    Debug.CreateOverrides(name: "DBG", pad: 3, padStr: ".", clr: "cyan"));
```

### Performance Logging
```csharp
Debug.LogCustom("PERF", $"Loaded {count} items", null,
    Debug.CreateOverrides(name: "⚡", clr: "yellow"));
```

### Conditional Custom Colors
```csharp
string color = value > threshold ? "red" : "green";
Debug.LogWarning($"Value: {value}", null, 
    Debug.CreateOverrides(clr: color));
```
