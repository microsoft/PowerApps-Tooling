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

export function convertToBlockScalar(line: string, style: BlockScalarStyle): string | null {
    const detected = detectDoubleQuotedScalar(line);
    if (!detected) {
        return null;
    }

    const { indent, key, rawValue } = detected;
    const parsed: string = JSON.parse('"' + rawValue + '"');
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
