"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.detectDoubleQuotedScalar = detectDoubleQuotedScalar;
exports.convertToBlockScalar = convertToBlockScalar;
function detectDoubleQuotedScalar(line) {
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
function convertToBlockScalar(line, style) {
    const detected = detectDoubleQuotedScalar(line);
    if (!detected) {
        return null;
    }
    const { indent, key, rawValue } = detected;
    const parsed = JSON.parse('"' + rawValue + '"');
    const contentIndent = indent + '  ';
    let lines = parsed.split('\n');
    // Strip trailing empty strings for strip style
    if (style === '|-') {
        while (lines.length > 0 && lines[lines.length - 1] === '') {
            lines.pop();
        }
    }
    // If the first content line is empty (value starts with '\n'), add explicit indentation indicator
    let effectiveStyle = style;
    if (lines.length > 0 && lines[0] === '') {
        const indentDigit = contentIndent.length;
        if (style === '|-') {
            effectiveStyle = `|${indentDigit}-`;
        }
        else if (style === '|+') {
            effectiveStyle = `|${indentDigit}+`;
        }
        else {
            effectiveStyle = `|${indentDigit}`;
        }
    }
    const blockLines = lines.map(l => contentIndent + l).join('\n');
    return `${indent}${key}: ${effectiveStyle}\n${blockLines}`;
}
//# sourceMappingURL=yamlScalarConverter.js.map