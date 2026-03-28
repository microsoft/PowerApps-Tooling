export interface JsonStringToken {
    /** Start index (inclusive) of the opening `"` in the line */
    start: number;
    /** End index (exclusive) — one past the closing `"` */
    end: number;
    /** The raw content including the outer quotes, still JSON-escaped */
    rawContent: string;
}

/**
 * Finds the JSON string literal in `line` whose span contains `column`.
 * Returns null if the cursor is not inside a string.
 */
export function findJsonStringAtPosition(line: string, column: number): JsonStringToken | null {
    const pattern = /("(?:[^"\\]|\\.)*")/g;
    let match: RegExpExecArray | null;
    while ((match = pattern.exec(line)) !== null) {
        const start = match.index;
        const end = start + match[0].length;
        if (column >= start && column < end) {
            return { start, end, rawContent: match[1] };
        }
    }
    return null;
}

export type DecodeResult =
    | { success: true; formatted: string }
    | { success: false; error: string };

/**
 * Decodes a stringified JSON value.
 * `rawContent` is the content including the outer quotes (still JSON-escaped).
 * On success returns the pretty-printed decoded value.
 * On failure returns the parse error message.
 */
export function decodeStringifiedJson(rawContent: string): DecodeResult {
    let innerString: string;
    try {
        // Reconstruct the full JSON string token and parse it to get the actual string value
        innerString = JSON.parse(rawContent) as string;
    } catch (e) {
        return { success: false, error: (e as Error).message };
    }

    try {
        const parsed: unknown = JSON.parse(innerString);
        return { success: true, formatted: JSON.stringify(parsed, null, 2) };
    } catch (e) {
        return { success: false, error: (e as Error).message };
    }
}
