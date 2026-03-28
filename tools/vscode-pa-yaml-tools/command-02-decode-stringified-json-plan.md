# Plan: Decode Stringified JSON Command

## Context
JSON files sometimes contain string values that are themselves serialized JSON (e.g. `"payload": "{\"foo\":1,\"bar\":\"baz\"}"` stored as a nested JSON string). Reading and editing these is painful. A VS Code command that decodes the string in place — replacing the JSON string literal with the formatted JSON value — removes the friction.

## Approach

Add a new VS Code command `pa-yaml.decodeStringifiedJson` that:

1. Is gated to `editorLangId == json` (context menu `when` clause + runtime guard).
2. Finds the JSON string literal whose span contains the current cursor position on the active line.
3. Parses the outer string (to recover the raw string value) using `JSON.parse`.
4. Tries `JSON.parse` on that raw value to decode the embedded JSON.
5. On failure → `vscode.window.showErrorMessage` with the parse error message.
6. On success → replaces the original string literal (including its surrounding quotes) with `JSON.stringify(parsed, null, 2)`, indented to match the surrounding document.

## Files

- `src/jsonStringDecoder.ts` — Pure logic (no VS Code dependency):
  - `findJsonStringAtPosition(line, column)` — locate the string token under the cursor
  - `decodeStringifiedJson(rawContent)` — decode the embedded JSON, return success/error
- `src/extension.ts` — Register the new command alongside the existing one
- `package.json` — Contribute command + context menu entry gated on `editorLangId == json`
- `src/test/jsonStringDecoder.test.ts` — Node built-in test runner; covers success path and invalid-JSON error path

## Key Implementation Details

### Finding the string token
Scan the line with `/\"((?:[^\"\\\\]|\\\\.)*)\"/g` and return the first match whose range `[match.index, match.index + match[0].length)` contains the cursor column.

### Decoding
```ts
const outer = JSON.parse(rawContent) as string;   // decode the outer JSON string
const inner = JSON.parse(outer);                   // parse the embedded JSON — may throw
return JSON.stringify(inner, null, 2);
```

### Indentation
The replacement text is formatted with 2-space indent. Subsequent lines get the leading indentation of the original line prepended so the block stays aligned in the document.

### Error UX
`vscode.window.showErrorMessage('PA JSON: ' + err.message)` — no document change on failure.

## Verification
1. `npm test` — all tests (both files) pass.
2. Open a JSON file with a stringified JSON string, place cursor inside it, run command → string replaced with formatted JSON.
3. Place cursor inside an invalid string → error toast shown, document unchanged.
