# Before & After Examples

## Example 1: Simple Error with Color Override

### Before
```csharp
Debug.LogError("Failed to load asset");
// Output: [MyScript:42] [ERROR] Failed to load asset
// Color: Orange (default error color, can't change per-call)
```

### After
```csharp
Debug.LogError("Failed to load asset", null, Debug.CreateOverrides(clr: "red"));
// Output: [MyScript:42] [ERROR] Failed to load asset
// Color: Pure red (overridden on this call)
```

**Benefit**: Control error severity with color without changing preferences

---

## Example 2: Custom Log Level

### Before
```csharp
// No way to create custom log levels
// Must stick with: Log, LogInfo, LogSuccess, LogDebug, LogWarning, LogError, LogFatal

// Workaround: Use LogWarning for everything
Debug.LogWarning("TRACE: Entering LoadScene()");
Debug.LogWarning("TRACE: Processing item #5");
Debug.LogWarning("TRACE: Exit LoadScene()");

// Output all show WARNING color, hard to distinguish from real warnings
```

### After
```csharp
// Register once
LogLevel.Register("TRACE", "TRACE", Color.gray, bold: false);

// Use cleanly
Debug.LogCustom("TRACE", "Entering LoadScene()");
Debug.LogCustom("TRACE", "Processing item #5");
Debug.LogCustom("TRACE", "Exit LoadScene()");

// Output: [MyScript:12] [TRACE] Entering LoadScene()
// Gray color, clearly distinct from warnings
```

**Benefit**: Unlimited custom log levels with full control over appearance

---

## Example 3: Format String with Color Tokens

### Before
```csharp
// Format string is static, no way to add colors
LoggerPreferences.Format = "[{script}:{line}] [{lvl}] {msg}";

// Output in Console is monochrome or uses default Unity colors
// No way to highlight specific parts (script path, line number)
```

### After
```csharp
// Format with color tokens
LoggerPreferences.Format = "[{color.gray}{script}{color.init}:{line}] [{color.rgb("FF0000")}{lvl}{color.init}] {msg}";

// Or use named colors
LoggerPreferences.Format = "[{color.gray}{script}{color.init}] [{color.red}{lvl}{color.init}] {color.cyan}{msg}{color.init}";

// Output in Console:
// - Script path shows in gray
// - Level shows in red
// - Message shows in cyan
// Much more scannable
```

**Benefit**: Rich formatting with colors directly in format string

---

## Example 4: Formatted Error with Brackets and Padding

### Before
```csharp
Debug.LogError("Item not found in inventory");
// Output: [MyScript:128] [ERROR] Item not found in inventory
// Hard to spot in busy console
```

### After
```csharp
var critical = Debug.CreateOverrides(
    name: "CRITICAL",
    clr: "red",
    pad: 10,
    padStr: "!",
    bLeft: ">>> ",
    bRight: " <<<"
);
Debug.LogError("Item not found in inventory", null, critical);

// Output: >>> CRITICAL!!! <<<<<<<< Item not found in inventory
// Stands out immediately, color is red
```

**Benefit**: Make critical errors impossible to miss

---

## Example 5: Performance Monitoring with Custom Level

### Before
```csharp
// Hack solution: use Debug.Log + manually format
Debug.Log($"[PERF] Level loaded in {loadTimeMs}ms");
Debug.Log($"[PERF] Instantiated {objectCount} objects");
Debug.Log($"[PERF] Mesh data cached: {meshDataSize}KB");

// Looks like normal logs, easy to miss
// No consistent formatting or color
```

### After
```csharp
// Register once at startup
LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f), bold: true);  // Orange, bold

// Log performance metrics
var perfColor = Debug.CreateOverrides(clr: "lime");  // Highlight with bright lime
Debug.LogCustom("PERF", $"Level loaded in {loadTimeMs}ms", null, perfColor);
Debug.LogCustom("PERF", $"Instantiated {objectCount} objects", null, perfColor);
Debug.LogCustom("PERF", $"Mesh data cached: {meshDataSize}KB", null, perfColor);

// Output shows:
// [MyScript:45] [PERF] Level loaded in 234ms        (orange/bold level + lime message)
// [MyScript:46] [PERF] Instantiated 1500 objects    (green message for easy scanning)
// [MyScript:47] [PERF] Mesh data cached: 8192KB     (same)
```

**Benefit**: Dedicated performance tracking with own level and visual style

---

## Example 6: Rich Console Output

### Before
```csharp
// Typical debug session output
Debug.Log("Application started");
Debug.LogWarning("Missing texture: Assets/Textures/grass.png");
Debug.LogError("Failed to spawn enemy prefab");
Debug.LogError("Exception: NullReferenceException at LoadData()");

// Console shows:
// All different colors (log, warning, error, error)
// Hard to group related messages
// Can't quickly spot specific issues
```

### After
```csharp
// Richer console output with custom levels
LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f));      // Orange
LogLevel.Register("ASSET", "ASSET", new Color(1f, 0.8f, 0f));    // Yellow
LogLevel.Register("CRASH", "CRASH", new Color(1f, 0f, 0f));      // Red

Debug.LogInfo("Application started");
Debug.LogWarning("Starting asset load phase");
Debug.LogCustom("ASSET", "Missing texture: Assets/Textures/grass.png");
Debug.LogError("Failed to spawn enemy prefab");
Debug.LogCustom("CRASH", "Exception: NullReferenceException at LoadData()", null, 
    Debug.CreateOverrides(pad: 6, padStr: "*", bLeft: "!!! ", bRight: " !!!"));

// Console shows:
// [INFO] Application started                    (blue)
// [WARNING] Starting asset load phase           (yellow)
// [ASSET] Missing texture: Assets/...           (gold)
// [ERROR] Failed to spawn enemy prefab          (red)
// !!! CRASH****** !!! Exception: Null...        (red, bold, escaped)
//
// Much easier to scan and categorize issues
```

**Benefit**: Organized, color-coded logging for better debugging workflow

---

## Example 7: Network Debugging

### Before
```csharp
// Network logs mixed with other logs
Debug.LogWarning("Sending request to server");
Debug.LogWarning("Server response received");
Debug.LogError("Request timeout after 30s");

// All appear as warnings/errors, hard to group network issues
// Can't quickly filter by network vs other concerns
```

### After
```csharp
// Register network levels
LogLevel.Register("NET_OUT", "NET→OUT", new Color(0f, 1f, 1f), bold: true);      // Cyan
LogLevel.Register("NET_IN", "NET←IN", new Color(0f, 1f, 1f));                    // Cyan
LogLevel.Register("NET_ERR", "NET_ERR", new Color(1f, 0f, 0f), bold: true);       // Red

// Use them
Debug.LogCustom("NET_OUT", "Sending GetInventory request");
Debug.LogCustom("NET_IN", "Received 500 items");
Debug.LogCustom("NET_ERR", "Request timeout after 30s");

// Console shows:
// [NET→OUT] Sending GetInventory request     (bold cyan) ← Easy to spot outgoing
// [NET←IN] Received 500 items                (cyan)     ← Easy to spot incoming
// [NET_ERR] Request timeout after 30s        (bold red) ← Easy to spot errors
//
// All network traffic in one visual style, separate from other logs
```

**Benefit**: Dedicated visual category for different subsystems

---

## Example 8: Advanced Format String with Multiple Colors

### Before
```csharp
// Limited ability to style different parts
LoggerPreferences.Format = "[{script}] [{lvl}] {msg}";

// Output: All parts use their own preferences, can't mix
// [MyScript] [ERROR] This is an error
```

### After
```csharp
// Rich multi-color format
LoggerPreferences.Format = "{color.gray}[{script}:{line}]{color.init} {color.rgb("FF6B6B")}{lvl}{color.init} {color.rgb("4ECDC4")}{msg}{color.init}";

// Output in Console:
// [gray]MyScript:42[/gray] [red-pink]ERROR[/red] [teal]This is an error[/teal]
//
// Each element in its own color, much richer visual hierarchy
// Easy to parse at a glance:
// - Gray shows location (file:line)
// - Red shows severity
// - Teal shows actual problem
```

**Benefit**: Professional-looking, scannable console output

---

## Example 9: Conditional Logging with Overrides

### Before
```csharp
void OnItemPickup(Item item) {
    if (isDebug) {
        Debug.Log($"[DEBUG] Picked up: {item.name}");
    }
    if (item.rarity == Rarity.Legendary) {
        Debug.LogWarning($"Legendary item found: {item.name}");
    }
}

// Hard to distinguish legendary from normal warnings
```

### After
```csharp
void OnItemPickup(Item item) {
    if (isDebug) {
        Debug.LogCustom("TRACE", $"Picked up: {item.name}");
    }
    
    if (item.rarity == Rarity.Legendary) {
        var legendary = Debug.CreateOverrides(
            name: "LEGENDARY!",
            clr: "#FFD700",  // Gold
            bLeft: "✨ ",
            bRight: " ✨"
        );
        Debug.LogWarning($"{item.name}", null, legendary);
    }
}

// Output:
// [TRACE] Picked up: Longsword              (gray, easy to filter)
// ✨ LEGENDARY! ✨ Excalibur                (gold, immediately stands out)
```

**Benefit**: Different message types get appropriate visual treatment

---

## Example 10: Real-World Game Loop Logging

### Before
```csharp
public class GameManager : MonoBehaviour {
    void Update() {
        // Debug logs get spammy
        Debug.Log("Update frame");  // Too verbose
        
        // Can't distinguish from errors
        if (health < 20) {
            Debug.LogError("Low health!");  // Treated like error, clutters console
        }
        
        // Performance tracking unclear
        Debug.Log($"FPS: {currentFPS}");
    }
}

// Console becomes unreadable, hard to find actual errors
```

### After
```csharp
public class GameManager : MonoBehaviour {
    void Start() {
        // Register custom levels once
        LogLevel.Register("TRACE", "TRACE", Color.gray, bold: false);
        LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f), bold: true);
    }
    
    void Update() {
        if (isDebug) {
            Debug.LogCustom("TRACE", "Update frame");  // Subtle, gray
        }
        
        if (health < 20) {
            var warning = Debug.CreateOverrides(clr: "red", pad: 6, padStr: "!");
            Debug.LogWarning($"Health at {health}%", null, warning);  // Orange with emphasis
        }
        
        if (Time.deltaTime > 0.016f) {  // Slower than 60 FPS
            var perfOverride = Debug.CreateOverrides(clr: "yellow");
            Debug.LogCustom("PERF", $"Frame drop: {currentFPS:F1} FPS", null, perfOverride);
        }
    }
}

// Console output:
// [TRACE] Update frame                       (gray, easy to ignore)
// !!!WARNING!!! Health at 15%                (orange, attention-grabbing)
// [PERF] Frame drop: 45.3 FPS                (yellow, highlights performance issues)
//
// Clear hierarchy: trace → normal → warning → error
// Easy to spot actual problems
```

**Benefit**: Professional logging that scales from debug to production

---

## Summary of Value

| Scenario | Before | After |
|----------|--------|-------|
| Error highlighting | Only global color | Per-call color override |
| Log organization | 6 fixed levels | 6 built-in + unlimited custom |
| Format control | Simple variables | Colors + formatting tokens |
| Output readability | Monochrome-ish | Rich, color-coded |
| Performance tracking | Mixed with logs | Dedicated level |
| Network debugging | Mixed with logs | Dedicated levels |
| Console filtering | By level only | By level + custom color |
| Time debugging | Inconsistent | Custom levels + colors |

All while maintaining **100% backward compatibility** with existing code.
