# Plan: VS Code PA YAML Block Scalar Converter

## Context
Power Apps `.pa.yaml` files store multi-line values as single-line double-quoted strings with `\n` escape sequences (e.g. `OnVisible: "=Set(...);\nDoMore()"`), which is hard to read. The preferred format is YAML block scalar (`|-`, `|`, `|+`). This is a general YAML utility — not PFx-specific.

YamlDotNet cannot auto-convert these because its default `|` (clip) appends a trailing `\n`, mutating values that don't originally end with `\n`. The user wants a VS Code command to do this manually, with explicit control over the chomping style.

## Approach

Create a VS Code extension at `tools/vscode-pa-yaml-tools/`.

**Detection criteria**:
- Current line is a single-line YAML property with a **double-quoted** scalar value only
- Single-quoted strings are excluded — they use `''` escaping, not `\n`
- No restriction on value content — general YAML tool

**Parsing**: Use `JSON.parse('"' + rawValue + '"')` to decode the double-quoted string. YAML double-quoted escapes are a superset of JSON, but for all real-world content the common sequences (`\n`, `\t`, `\"`, `\\`, `\uXXXX`) are identical — no custom escape handling needed.

**Conversion**: split parsed value by `\n`, re-emit as block scalar with chosen chomping style.

**UX**: Single command `PA YAML: Convert to block scalar...` → Quick Pick for style.

---

## Files

- `package.json` — Extension manifest with command and context menu contribution
- `tsconfig.json` — TypeScript config
- `src/yamlScalarConverter.ts` — Pure conversion logic (no VS Code dependency)
- `src/extension.ts` — VS Code activation / command registration
- `src/test/yamlScalarConverter.test.ts` — Node.js built-in test runner tests

## Key Implementation Details

### Detection regex
```
/^(\s*)(\S+):\s+"((?:[^"\\]|\\.)*)"(\s*)$/
```

### Indentation indicator
If the first content line is empty (value starts with `\n`), YAML requires an explicit indentation indicator digit after the style character (e.g. `|2-`, `|2`, `|2+`) so parsers know the indentation level.

### Chomping behavior
- `|-` (strip): trailing empty lines removed
- `|`  (clip): trailing empty lines kept as-is in split array (YAML parser will add exactly one `\n`)
- `|+` (keep): trailing empty lines kept as-is (YAML parser preserves all)

## Verification
1. `cd tools/vscode-pa-yaml-tools && npm install && npm test` — all tests pass
2. F5 in VS Code → Extension Development Host
3. Open any `.yaml` file, place cursor on a line like:
   ```yaml
   MyProp: "line1\nline2\nline3"
   ```
4. Run `PA YAML: Convert to block scalar...` → pick `|-`
5. Verify output:
   ```yaml
   MyProp: |-
     line1
     line2
     line3
   ```
6. Sanity-check: cursor on an unquoted or block-scalar line → warning shown, no change
