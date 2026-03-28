"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.findJsonStringAtPosition = findJsonStringAtPosition;
exports.decodeStringifiedJson = decodeStringifiedJson;
/**
 * Finds the JSON string literal in `line` whose span contains `column`.
 * Returns null if the cursor is not inside a string.
 */
function findJsonStringAtPosition(line, column) {
    const pattern = /("(?:[^"\\]|\\.)*")/g;
    let match;
    while ((match = pattern.exec(line)) !== null) {
        const start = match.index;
        const end = start + match[0].length;
        if (column >= start && column < end) {
            return { start, end, rawContent: match[1] };
        }
    }
    return null;
}
/**
 * Decodes a stringified JSON value.
 * `rawContent` is the content including the outer quotes (still JSON-escaped).
 * On success returns the pretty-printed decoded value.
 * On failure returns the parse error message.
 */
function decodeStringifiedJson(rawContent) {
    let innerString;
    try {
        // Reconstruct the full JSON string token and parse it to get the actual string value
        innerString = JSON.parse(rawContent);
    }
    catch (e) {
        return { success: false, error: e.message };
    }
    try {
        const parsed = JSON.parse(innerString);
        return { success: true, formatted: JSON.stringify(parsed, null, 2) };
    }
    catch (e) {
        return { success: false, error: e.message };
    }
}
//# sourceMappingURL=jsonStringDecoder.js.map