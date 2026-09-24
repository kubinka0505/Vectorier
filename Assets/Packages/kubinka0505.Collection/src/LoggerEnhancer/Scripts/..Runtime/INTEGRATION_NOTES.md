# Integration Notes & Design Decisions

## Overview
This document outlines integration strategy, tradeoffs, and decisions made during enhancement.

---

## 1. Format String System (Color Tokens)

### Design Decision: Runtime Token Processing
**Chosen**: Process color tokens at log-time (in `FormatStringProcessor`)

**Why**:
- Variables are only known at log-time (scriptPath, lineNumber, level name)
- Color tokens need to wrap styled variables, not just be simple replacements
- Allows nested/complex patterns: `{color.gray}{script}{color.init}`

**Alternative Considered**: Pre-compile format strings
- Would require invalidation on level definition changes
- More complex caching logic
- Minimal performance benefit (logs aren't high-frequency)

### Hex Shorthand Expansion Logic
**Examples**:
- `"C0"` → `"C0C0C0"` (replicate across RGB)
- `"ABC"` → `"ABCABC"` (mirror pattern)
- `"ABCD"` → `"ABCDAB"` (first 3 chars for RGB)

This matches common shorthand conventions while remaining predictable.

### Default Format String
```csharp
"[{color.gray}{script}{color.init}:{line}] [{color.rgb("#FF0000")}{lvl}{color.init}] {msg}"
```
- Shows off color capabilities without being garish
- Readable in both styled and unstyled contexts
- Easy to customize via UI

---

## 2. Custom Log Levels

### Design Decision: ScriptableObject Registry
**Chosen**: Asset-based registry (`LogLevelRegistry.asset`)

**Why**:
- ✅ Works in built players (asset reference is persistent)
- ✅ No reflection/dynamic execution needed
- ✅ GUI editor integration possible
- ✅ Easy to back up/version control
- ✅ Thread-safe (no dynamic compilation)

**Alternatives Considered**:
1. **JSON file storage** - Harder to reference from code, requires parsing
2. **Dynamic execution** - Reflection-based `getattr(loglevels, x)` - Unity doesn't support well, security risk
3. **Hardcoded enum extension** - Requires code changes, not user-extensible

### Registry Structure
```csharp
// Get level definition
if (LogLevel.TryGet("CUSTOM", out var definition)) {
    // Use definition.color, .bold, .italic, etc.
}

// Register at runtime
LogLevel.Register("TRACE", "TRACE", Color.gray);
```

### Integration with Preferences
`LoggerPreferences.GetLevelColor(level)` checks:
1. Custom registry first
2. Falls back to EditorPrefs (for built-in level overrides)
3. Falls back to hardcoded defaults

This allows custom levels to coexist seamlessly with built-in ones.

---

## 3. Per-Call Overrides

### Design Decision: Struct-Based Overrides
**Chosen**: `LogOverrides` struct with optional fields

```csharp
public struct LogOverrides {
    public string name;
    public string clr;
    public int? pad;
    public string padStr;
    public string bLeft;
    public string bRight;
}
```

**Why**:
- ✅ Clean, type-safe API
- ✅ Zero-allocation (value type)
- ✅ Easy to extend with new fields
- ✅ Works with nullability (`int?` for optional pad)

**Alternatives Considered**:
1. **Method parameters** - `Log(msg, clr: "red", pad: 4)` - Named parameters are clean but verbose in signatures
2. **Builder pattern** - Fluent but overkill for simple cases
3. **Varargs dictionary** - Dynamic but loses compile-time safety

### Helper Method
```csharp
Debug.CreateOverrides(name: "X", clr: "red", pad: 4, padStr: "0")
```
Makes the common case concise while remaining type-safe.

### Padding Behavior
- `pad: 4, padStr: "0"` → Right-pads level name to 4 chars with "0"
- Only applies if both `pad` and `padStr` are set
- Uses `string.PadRight()` with first char of `padStr`

Example:
```csharp
name: "ERR", pad: 5, padStr: "0" 
→ "ERR00"
```

### Bracket Overrides
Allows wrapping level name with custom brackets:
```csharp
bLeft: "{{", bRight: "}}"  →  {{LEVEL}}
bLeft: "[", bRight: "]"     →  [LEVEL]  (default)
```

---

## 4. Integration Path

### Step 1: Replace Logger.cs
```csharp
// Old files to replace:
Logger.cs  →  Logger_Enhanced.cs

// Also copy:
ColorParser.cs
FormatStringProcessor.cs
LogLevel.cs
```

### Step 2: Update LoggerPreferences.cs
```csharp
// Old:
LoggerPreferences.cs  →  LoggerPreferences_Enhanced.cs
```

Key changes:
- Default format string now uses color tokens
- Added `GetAllLevels()` that includes custom levels
- Updated level getters to check `LogLevel.TryGet()` first

### Step 3: Keep LoggerUtils.cs
No changes needed — it's fully compatible.

### Step 4: Create Resources Directory
```
Assets/Resources/
  └─ LogLevelRegistry.asset  (auto-created on first custom level registration)
```

---

## 5. Backward Compatibility

### ✅ Fully Compatible
```csharp
// All existing calls work unchanged
Debug.Log("message");
Debug.LogWarning("message", context);
Debug.LogError("message", context);
```

### New Capability (Opt-In)
```csharp
// Color tokens in format string (only if user enables)
LoggerPreferences.Format = "...{color.red}{lvl}{color.init}...";

// Per-call overrides (optional)
Debug.LogError("msg", ctx, Debug.CreateOverrides(clr: "red"));

// Custom levels (optional)
LogLevel.Register("TRACE", "TRACE", Color.gray);
Debug.LogCustom("TRACE", "message");
```

### No Breaking Changes
- Delegate signatures unchanged for basic methods
- New methods are overloads
- Preferences structure extended (new keys), not modified

---

## 6. Performance Considerations

### String Parsing (ColorParser)
- Dictionary lookup for named colors: O(1)
- Hex expansion: linear in string length (max 8 chars)
- Called only at log-time (not per-frame usually)

### Format String Processing (FormatStringProcessor)
- Regex matching for color tokens
- One pass through format string
- Only in editor, only when logging

### Custom Level Lookup
- Check registry (small list, linear search)
- Falls back to built-in with O(1) dictionary lookup
- Happens once per log call (acceptable)

**Bottom Line**: Zero performance impact in runtime builds (all editor-only code).

---

## 7. Extensibility

### Adding New Named Colors
```csharp
// In ColorParser.cs, add to WebColors dict:
{ "mycolor", new Color(r, g, b, a) }
```

### Adding New Format Variables
```csharp
// In FormatStringProcessor.ProcessFormat():
variables.Add("custom", "value");
// Now usable as {custom} in format string
```

### Adding New Override Fields
```csharp
// In LogOverrides struct:
public bool uppercase;

// Then in Logger_Enhanced.Formatter():
if (overrides.uppercase) displayName = displayName.ToUpper();
```

---

## 8. Testing Recommendations

### Test Cases
1. **Color parsing**:
   - Named colors: "red", "blue", "gray"
   - Hex shorthand: "C0", "ABC", "ABCD"
   - Hex full: "FF0000", "FF0000FF"

2. **Format strings**:
   - Missing color tokens: `{color.nonexistent}`
   - Nested colors: `{color.red}{color.blue}{lvl}{color.init}`
   - Mixed variables: `{script} {color.gray}:{line}`

3. **Custom levels**:
   - Register/unregister
   - Retrieve with fallback behavior
   - UI integration

4. **Overrides**:
   - Null overrides (LogOverrides.None)
   - Partial overrides (only color, or only padding)
   - Complex padding + brackets

### Edge Cases
- Very long level names with padding
- Unicode in custom level names
- Format strings with no variables
- Calling LogCustom with non-existent level (falls back gracefully)

---

## 9. Known Limitations

### 1. Format String Limitations
- No conditional logic (if/else in format string)
- No computed variables (e.g., script name uppercase)
- Color tokens must be properly nested (can't interleave)

**Workaround**: Use per-call overrides for dynamic values.

### 2. Custom Levels
- Registry is global (can't have per-scene custom levels)
- No unregistration from UI (only programmatic)

**Workaround**: Create multiple registries, or use per-call overrides.

### 3. Padding Limitation
- Only supports right-padding
- Uses first character of `padStr` (no multi-char padding)

**Workaround**: Use custom `bLeft`/`bRight` for more control.

---

## 10. Future Enhancement Ideas

1. **Conditional format strings**: `{if(levelUseColor):colored|plain}` 
2. **Computed variables**: `{script.upper}`, `{line.hex}`
3. **Per-scene level registries**: Different custom levels per scene
4. **Format string validation UI**: Preview invalid tokens
5. **Level inheritance**: Extend built-in levels with custom properties
6. **Filter system**: Log level filtering by key/regex
