# Enhanced Unity Logger System

## What's Included

### Core Enhancements

#### 1. Advanced Format Strings with Color Tokens
- **Color tokens**: `{color.gray}`, `{color.rgb("red")}`, `{color.init}`
- **Named colors**: Supports ~16 web color names
- **Hex shorthand**: Auto-expands 2-6 char hex codes to full RGBA
- **Variable substitution**: Existing `{script}`, `{line}`, `{lvl}`, `{msg}` variables work with colors

**Files**: `ColorParser.cs`, `FormatStringProcessor.cs`

#### 2. Custom Log Levels
- **Registry-based**: ScriptableObject-backed log level registration
- **Runtime registration**: `LogLevel.Register("TRACE", "TRACE", Color.gray)`
- **Full control**: Custom color, bold, italic, line display
- **Fallback system**: Works seamlessly with built-in log levels (INFO, WARNING, ERROR, etc.)

**Files**: `LogLevel.cs` (creates `LogLevelRegistry.asset`)

#### 3. Per-Call Format Overrides
- **Struct-based API**: Type-safe, zero-allocation
- **Override fields**: name, color, padding, brackets
- **Flexible usage**: Apply any combination of overrides to individual log calls
- **Helper method**: `Debug.CreateOverrides()` for clean fluent syntax

**Files**: `Logger_Enhanced.cs`

---

## File Structure

### New Files (Add These)
```
ColorParser.cs                  - Color parsing & conversion utility
FormatStringProcessor.cs        - Format string with color token processing
LogLevel.cs                     - Custom level registry & helpers
Logger_Enhanced.cs              - Main Debug class with overrides support
LoggerPreferences_Enhanced.cs    - Preferences updated for new features
```

### Keep Existing
```
LoggerUtils.cs                  - Utilities (unchanged, fully compatible)
```

### Auto-Created
```
Assets/Resources/LogLevelRegistry.asset  - Created on first custom level use
```

---

## Integration Steps

### Quick Start (5 minutes)

1. **Copy new files** to your Logger directory:
   - `ColorParser.cs`
   - `FormatStringProcessor.cs`
   - `LogLevel.cs`

2. **Replace existing files**:
   - `Logger.cs` → Rename `Logger_Enhanced.cs` to `Logger.cs`
   - `LoggerPreferences.cs` → Rename `LoggerPreferences_Enhanced.cs` to `LoggerPreferences.cs`

3. **Verify compilation** - no errors should occur

4. **Test in Editor** - logs should work as before, with new features available

See **MIGRATION_CHECKLIST.md** for detailed integration steps.

---

## Usage Examples

### Color Tokens in Format String
```csharp
// Set format with color tokens
LoggerPreferences.Format = "[{color.gray}{script}:{line}{color.init}] {msg}";

// Or use full format with level colors
LoggerPreferences.Format = "[{color.gray}{script}{color.init}] [{color.red}{lvl}{color.init}] {msg}";
```

### Custom Log Levels
```csharp
// Register
LogLevel.Register("TRACE", "TRACE", Color.gray, bold: false);

// Use
Debug.LogCustom("TRACE", "Entering function");
```

### Per-Call Overrides
```csharp
// Simple color override
var red = Debug.CreateOverrides(clr: "red");
Debug.LogError("Critical error", null, red);

// Formatted level with padding and brackets
var formatted = Debug.CreateOverrides(
    name: "ERR",
    clr: "red",
    pad: 5,
    padStr: "0",
    bLeft: "<<",
    bRight: ">>"
);
Debug.LogError("Failed to load asset", gameObject, formatted);
// Output: <<ERR00>> Failed to load asset
```

### Combined Usage
```csharp
// Register custom level
LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f));

// Use with override
var perf = Debug.CreateOverrides(clr: "lime");
Debug.LogCustom("PERF", "Loaded 100 objects in 12ms", null, perf);
```

See **USAGE_GUIDE.md** for comprehensive examples.

---

## Design Decisions (Tradeoffs)

### Why ScriptableObject Registry for Custom Levels?
✅ Works in built players (persistent asset)  
✅ No reflection/dynamic code required  
✅ Can extend from Editor UI  
✅ Version control friendly  
✅ Thread-safe  

❌ Requires Resources directory  
❌ Can't unregister from runtime UI (only code)  

**Alternative**: JSON file storage — but loses above benefits. Reflection-based dynamic execution not recommended for safety/compatibility.

### Why Struct for Overrides?
✅ Type-safe API  
✅ Zero-allocation (value type)  
✅ Easy to extend with new fields  
✅ Compile-time checking  

❌ No method chaining fluency  
❌ Can't use keyword args with unnamed overload  

**Alternative**: Builder pattern — overkill for this scope.

### Why Process Color Tokens at Log-Time?
✅ Variables only known at log-time (script path, line number)  
✅ Allows complex patterns (`{color.gray}{script}{color.init}`)  
✅ Matches existing architecture  

❌ Slight overhead (regex per log call)  
❌ No pre-validation of format strings  

**Impact**: Negligible (logs not high-frequency).

See **INTEGRATION_NOTES.md** for detailed design documentation.

---

## Feature Comparison

| Feature | Before | After |
|---------|--------|-------|
| Log levels | 6 hardcoded | 6 built-in + unlimited custom |
| Format string | Simple `{var}` | `{var}` + `{color.X}` tokens |
| Per-call formatting | None | Color, padding, brackets, name |
| Color support | Global preferences only | Per-call overrides |
| Custom definitions | EditorPrefs string keys | Scriptable registry |
| Backward compatible | N/A | ✅ 100% |

---

## Backward Compatibility

### ✅ Fully Compatible
All existing code works unchanged:
```csharp
Debug.Log("message");
Debug.LogWarning("message", context);
Debug.LogError("message", context);
Debug.LogCustom("CUSTOM", "message");  // New but simple extension
```

### New Capability (Opt-In)
```csharp
// Format string improvements
LoggerPreferences.Format = "...{color.red}{lvl}{color.init}...";

// Custom levels
LogLevel.Register("TRACE", "TRACE", Color.gray);

// Per-call overrides (new optional parameter)
Debug.LogError("msg", ctx, Debug.CreateOverrides(clr: "red"));
```

### Zero Breaking Changes
- Existing method signatures unchanged
- New methods are overloads
- Preferences keys extended, not modified
- Format string defaults updated (can customize or reset)

---

## Testing

### Phase 1: Basic Logging (Should work as before)
```csharp
Debug.Log("Test");
Debug.LogWarning("Test warning");
Debug.LogError("Test error", gameObject);
```
✅ All appear in Console with expected formatting

### Phase 2: Color Tokens
```csharp
LoggerPreferences.Format = "[{color.gray}{script}{color.init}] {msg}";
```
✅ Script path shows in gray

### Phase 3: Custom Levels
```csharp
LogLevel.Register("TRACE", "TRACE", Color.gray);
Debug.LogCustom("TRACE", "Message");
```
✅ Custom level works with correct color

### Phase 4: Overrides
```csharp
var ovr = Debug.CreateOverrides(clr: "red", pad: 4, padStr: "0");
Debug.LogError("Error", null, ovr);
```
✅ Formatting applied correctly

---

## Known Limitations

1. **Format strings** - No conditional logic, no computed variables
   - *Workaround*: Use per-call overrides for dynamic values

2. **Custom levels** - Registry is global, no per-scene isolation
   - *Workaround*: Clear registry between scenes if needed

3. **Padding** - Right-align only, single-char padding
   - *Workaround*: Use custom brackets for more control

See **INTEGRATION_NOTES.md** §9 for full limitations discussion.

---

## Documentation

- **USAGE_GUIDE.md** - Complete feature examples and patterns
- **INTEGRATION_NOTES.md** - Design decisions, tradeoffs, extensibility
- **MIGRATION_CHECKLIST.md** - Step-by-step integration with testing
- **README.md** - This file

---

## Performance Impact

- **Editor**: Negligible (color parsing, format processing on log-time)
- **Runtime build**: **Zero** (all enhanced code is editor-only with `#if UNITY_EDITOR`)
- **No GC allocation** when using LogOverrides (struct type)

---

## Future Extensions

Ideas for further enhancement:

1. Conditional format strings: `{if(useColor):colored|plain}`
2. Computed variables: `{script.upper}`, `{line.hex}`
3. Filter system: Filter by level/regex
4. Per-scene level registries
5. Level inheritance (extend built-in levels)

---

## Support & Troubleshooting

### Common Issues

**"ColorParser not found"**
→ Ensure all 5 new files are in same directory

**"LogLevel registry not creating"**
→ Create `Assets/Resources/` manually

**"Format colors not showing"**
→ Check `LoggerPreferences.EnableLogger` is `true`

**"Old format string still showing"**
→ Clear prefs: `EditorPrefs.DeleteKey("LoggerEnhancer.FormatString")`

See **MIGRATION_CHECKLIST.md** §Troubleshooting for more.

---

## License & Attribution

These enhancements extend the existing Logger system. Maintain any existing license/attribution from the original implementation.

---

**Ready to integrate?** Start with **MIGRATION_CHECKLIST.md**
