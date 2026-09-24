# Migration Checklist

Complete these steps to integrate the enhanced logger system into your project.

---

## Phase 1: Backup & Preparation

- [ ] Backup current Logger system files
  - [ ] Logger.cs
  - [ ] LoggerPreferences.cs
  - [ ] LoggerUtils.cs

- [ ] Review current format string usage
  - [ ] Search codebase: `LoggerPreferences.Format`
  - [ ] Document any custom format strings

- [ ] Document current custom configurations
  - [ ] Screenshot Logger settings from Editor preferences
  - [ ] Note any EditorPrefs customizations

---

## Phase 2: File Integration

### Step 1: Add New Utility Files
Copy these new files to your Logger directory:
- [ ] ColorParser.cs
- [ ] FormatStringProcessor.cs
- [ ] LogLevel.cs

### Step 2: Replace Core Files
Replace your existing files:
- [ ] Logger.cs → Replace with Logger_Enhanced.cs (rename to Logger.cs)
- [ ] LoggerPreferences.cs → Replace with LoggerPreferences_Enhanced.cs (rename to LoggerPreferences.cs)

### Step 3: Keep Existing File
- [ ] LoggerUtils.cs → No changes needed, keep as-is

### Step 4: Verify Imports
In the new Logger.cs, verify:
- [ ] Using statements compile
- [ ] PathE namespace is available
- [ ] No missing dependencies

---

## Phase 3: Directory Structure

Create Resources directory if it doesn't exist:
```
Assets/
├─ Resources/
│  └─ [LogLevelRegistry.asset will be created here automatically]
```

- [ ] Create `Assets/Resources/` directory
- [ ] First use of custom levels will auto-create LogLevelRegistry.asset

---

## Phase 4: Testing

### 4.1 Basic Logging (Should work unchanged)
```csharp
Debug.Log("Test message");
Debug.LogWarning("Test warning");
Debug.LogError("Test error", gameObject);
```
- [ ] Open Console
- [ ] Verify all logs appear
- [ ] Verify colors/formatting work

### 4.2 Color Tokens in Format String
```csharp
// In Editor, change format string to:
LoggerPreferences.Format = "[{color.gray}{script}:{line}{color.init}] {msg}";
```
- [ ] Logs should show gray script path
- [ ] Formatting should parse without errors

### 4.3 Custom Log Levels
```csharp
// Test registration
LogLevel.Register("TRACE", "TRACE", Color.gray);
Debug.LogCustom("TRACE", "Test trace message");
```
- [ ] Custom level appears in Console
- [ ] Uses correct color
- [ ] No errors in Console

### 4.4 Per-Call Overrides
```csharp
var overrides = Debug.CreateOverrides(clr: "red", pad: 5, padStr: "*");
Debug.LogError("Test error", null, overrides);
```
- [ ] Level name shows with padding
- [ ] Color override applied
- [ ] No errors

### 4.5 Full Integration Test
```csharp
// Register custom level
LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f, 1f), bold: true);

// Use custom level with overrides
var perf = Debug.CreateOverrides(clr: "lime", bLeft: "◆", bRight: "◆");
Debug.LogCustom("PERF", "Operation completed", null, perf);
```
- [ ] Custom level registered
- [ ] Override applied (color, brackets)
- [ ] Console shows correctly styled output

---

## Phase 5: Configuration Update

### 5.1 Update Format String (Optional)
The default format now uses color tokens:
```
[{color.gray}{script}{color.init}:{line}] [{color.rgb("#FF0000")}{lvl}{color.init}] {msg}
```

Choose one:
- [ ] Keep default format (recommended for first use)
- [ ] Convert existing custom format to use color tokens
  - Example: `[{script}:{line}]` → `[{color.gray}{script}{color.init}:{line}]`
- [ ] Reset to defaults: `LoggerPreferences.ResetToDefaults()`

### 5.2 Register Custom Levels (Optional)
If your project uses custom log levels:
```csharp
// Add to a startup script or Awake():
LogLevel.Register("TRACE", "TRACE", Color.gray, bold: false);
LogLevel.Register("PERF", "PERF", new Color(1f, 0.5f, 0f), bold: true);
```
- [ ] Add level registrations to startup code
- [ ] Verify levels persist in saved scenes

---

## Phase 6: Code Updates (If Needed)

### 6.1 Search & Replace (Optional Modernization)
If you want to use new features:

**Find**: `Debug.LogError("msg", context);`
**Replace with**: `Debug.LogError("msg", context, Debug.CreateOverrides(clr: "red"));`

- [ ] Identify high-priority error logs
- [ ] Update to use color overrides (optional)

### 6.2 Custom Level Migration
If you had custom log types before:
- [ ] Convert to LogLevel.Register() calls
- [ ] Remove any custom log method wrappers
- [ ] Update callsites to use Debug.LogCustom()

---

## Phase 7: Documentation & Training

- [ ] Share USAGE_GUIDE.md with team
- [ ] Document any custom format strings used
- [ ] Add examples to project wiki
- [ ] Update code style guide for logger usage

---

## Phase 8: Validation

### 8.1 Build Test
- [ ] Clean build (File → Build Settings → Build)
- [ ] Verify no compilation errors
- [ ] Check that logs still work in built game

### 8.2 Scenes Test
- [ ] Open main scene
- [ ] Play game
- [ ] Verify logs appear in Console
- [ ] Check formatting looks correct

### 8.3 Edge Cases
- [ ] Very long log messages
- [ ] Messages with special characters
- [ ] Null context (should work)
- [ ] Multiple logs in quick succession

### 8.4 Team Testing
- [ ] Share updated files with team members
- [ ] Get feedback on new features
- [ ] Document any custom extensions

---

## Rollback Plan (If Issues)

If you encounter problems:

1. **Revert files** to backed-up versions:
   ```
   Logger.cs (old version)
   LoggerPreferences.cs (old version)
   ```

2. **Delete new files**:
   ```
   ColorParser.cs
   FormatStringProcessor.cs
   LogLevel.cs
   Logger_Enhanced.cs (if you kept it)
   LoggerPreferences_Enhanced.cs (if you kept it)
   ```

3. **Clear EditorPrefs** (if format string issues):
   ```csharp
   EditorPrefs.DeleteKey("LoggerEnhancer.FormatString");
   // Then reset in Editor preferences
   ```

4. **Delete Registry** (if custom level issues):
   ```
   Assets/Resources/LogLevelRegistry.asset
   ```

---

## Checklist Summary

**Phase 1 (Backup)**: 3 items
**Phase 2 (Files)**: 4 items  
**Phase 3 (Structure)**: 1 item
**Phase 4 (Testing)**: 12 items
**Phase 5 (Config)**: 3 items
**Phase 6 (Code)**: 2 items
**Phase 7 (Docs)**: 4 items
**Phase 8 (Validation)**: 5 items

**Total**: 34 checkboxes to ensure smooth integration

---

## Troubleshooting

### Issue: "ColorParser not found"
**Solution**: Verify ColorParser.cs is in same directory as Logger.cs

### Issue: "LogLevel registry not creating"
**Solution**: Create `Assets/Resources/` directory manually

### Issue: "Format string not applying colors"
**Solution**: Verify LoggerPreferences.EnableLogger is `true`

### Issue: "Old format string still showing"
**Solution**: Clear cached preferences:
```csharp
EditorPrefs.DeleteKey("LoggerEnhancer.FormatString");
```

### Issue: "Compilation errors with 'PathE' namespace"
**Solution**: Verify original Logger.cs imports are available, or update namespace references

### Issue: "LogOverrides struct not recognized"
**Solution**: Ensure Logger_Enhanced.cs is renamed to Logger.cs in project

---

## Success Criteria

✅ All existing `Debug.Log*()` calls work unchanged
✅ Color tokens parse in format strings without errors
✅ Custom log levels register and display correctly
✅ Per-call overrides apply (color, padding, brackets)
✅ Logs appear formatted in Console with proper colors
✅ No compilation errors or warnings
✅ Built game runs without logger-related errors

---

**Need Help?**
- See USAGE_GUIDE.md for feature examples
- See INTEGRATION_NOTES.md for design decisions
- Check console for detailed error messages
