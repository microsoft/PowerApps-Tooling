export type BlockScalarStyle = '|' | '|-' | '|+';

export interface DetectedScalar {
    indent: string;    // leading whitespace of the line
    key: string;       // property name
    rawValue: string;  // content between the outer quotes (still JSON-escaped)
}

export function detectDoubleQuotedScalar(line: string): DetectedScalar | null {
    const match = /^(\s*)(\S+):\s+"((?:[^"\\]|\\.)*)"(\s*)$/.exec(line);
    if (!match) {
        return null;
    }
    return {
        indent: match[1],
        key: match[2],
        rawValue: match[3],
    };
}

/**
 * Unescape a YAML double-quoted string value.
 * YAML supports escape sequences that JSON does not, including \UXXXXXXXX.
 */
function unescapeYamlString(escaped: string): string {
    return escaped.replace(/\\(u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8}|[0abtnvfre "\/\\NLP_])/g, (match, code) => {
        if (code.startsWith('u')) {
            // \uXXXX - 4 hex digits
            return String.fromCharCode(parseInt(code.slice(1), 16));
        } else if (code.startsWith('U')) {
            // \UXXXXXXXX - 8 hex digits (YAML-specific, not in JSON)
            const codePoint = parseInt(code.slice(1), 16);
            return String.fromCodePoint(codePoint);
        }
        // Standard escape sequences
        const escapes: Record<string, string> = {
            '0': '\0',
            'a': '\x07',
            'b': '\b',
            't': '\t',
            'n': '\n',
            'v': '\x0B',
            'f': '\f',
            'r': '\r',
            'e': '\x1B',
            ' ': ' ',
            '"': '"',
            '/': '/',
            '\\': '\\',
            'N': '\u0085',
            '_': '\u00A0',
            'L': '\u2028',
            'P': '\u2029',
        };
        return escapes[code] || match;
    });
}

export function convertToBlockScalar(line: string, style: BlockScalarStyle): string | null {
    const detected = detectDoubleQuotedScalar(line);
    if (!detected) {
        return null;
    }

    const { indent, key, rawValue } = detected;
    const parsed: string = unescapeYamlString(rawValue);
    const contentIndent = indent + '  ';

    let lines = parsed.split('\n');

    // Strip trailing empty strings for strip style
    if (style === '|-') {
        while (lines.length > 0 && lines[lines.length - 1] === '') {
            lines.pop();
        }
    }

    // If the first content line is empty (value starts with '\n'), add explicit indentation indicator
    let effectiveStyle: string = style;
    if (lines.length > 0 && lines[0] === '') {
        const indentDigit = contentIndent.length;
        if (style === '|-') {
            effectiveStyle = `|${indentDigit}-`;
        } else if (style === '|+') {
            effectiveStyle = `|${indentDigit}+`;
        } else {
            effectiveStyle = `|${indentDigit}`;
        }
    }

    const blockLines = lines.map(l => contentIndent + l).join('\n');
    return `${indent}${key}: ${effectiveStyle}\n${blockLines}`;
}
