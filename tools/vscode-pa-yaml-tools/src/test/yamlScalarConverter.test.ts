import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import { detectDoubleQuotedScalar, convertToBlockScalar } from '../yamlScalarConverter';

describe('detectDoubleQuotedScalar', () => {
    test('simple key: "value" matches', () => {
        const result = detectDoubleQuotedScalar('key: "value"');
        assert.ok(result !== null);
        assert.equal(result!.indent, '');
        assert.equal(result!.key, 'key');
        assert.equal(result!.rawValue, 'value');
    });

    test('indented property captures indent', () => {
        const result = detectDoubleQuotedScalar('    Prop: "hello"');
        assert.ok(result !== null);
        assert.equal(result!.indent, '    ');
        assert.equal(result!.key, 'Prop');
        assert.equal(result!.rawValue, 'hello');
    });

    test('single-quoted value returns null', () => {
        const result = detectDoubleQuotedScalar("key: 'single'");
        assert.equal(result, null);
    });

    test('unquoted value returns null', () => {
        const result = detectDoubleQuotedScalar('key: unquoted');
        assert.equal(result, null);
    });

    test('block scalar line returns null', () => {
        const result = detectDoubleQuotedScalar('key: |');
        assert.equal(result, null);
    });

    test('value with escaped newline matches (rawValue contains literal \\n)', () => {
        const result = detectDoubleQuotedScalar('key: "val1\\nval2"');
        assert.ok(result !== null);
        assert.equal(result!.key, 'key');
        assert.equal(result!.rawValue, 'val1\\nval2');
    });

    test('value with escaped quote matches', () => {
        const result = detectDoubleQuotedScalar('key: "say \\"hi\\""');
        assert.ok(result !== null);
        assert.equal(result!.rawValue, 'say \\"hi\\"');
    });
});

describe('convertToBlockScalar', () => {
    test('single-line value with strip style', () => {
        const result = convertToBlockScalar('key: "value"', '|-');
        assert.equal(result, 'key: |-\n  value');
    });

    test('multi-line value with strip style produces correct lines', () => {
        const result = convertToBlockScalar('key: "line1\\nline2\\nline3"', '|-');
        assert.equal(result, 'key: |-\n  line1\n  line2\n  line3');
    });

    test('strip style removes trailing empty line', () => {
        const result = convertToBlockScalar('key: "line1\\nline2\\n"', '|-');
        assert.equal(result, 'key: |-\n  line1\n  line2');
    });

    test('clip style keeps trailing newline structure', () => {
        const result = convertToBlockScalar('key: "line1\\nline2\\n"', '|');
        assert.equal(result, 'key: |\n  line1\n  line2\n  ');
    });

    test('keep style preserves trailing empty line', () => {
        const result = convertToBlockScalar('key: "line1\\nline2\\n"', '|+');
        assert.equal(result, 'key: |+\n  line1\n  line2\n  ');
    });

    test('indented property uses correct content indent', () => {
        const result = convertToBlockScalar('  MyProp: "hello\\nworld"', '|-');
        assert.equal(result, '  MyProp: |-\n    hello\n    world');
    });

    test('value with escaped quote decoded correctly', () => {
        const result = convertToBlockScalar('key: "say \\"hi\\""', '|-');
        assert.equal(result, 'key: |-\n  say "hi"');
    });

    test('value with tab decoded correctly', () => {
        const result = convertToBlockScalar('key: "col1\\tcol2"', '|-');
        assert.equal(result, 'key: |-\n  col1\tcol2');
    });

    test('non-matching line returns null', () => {
        const result = convertToBlockScalar('key: unquoted', '|-');
        assert.equal(result, null);
    });

    test('value starting with newline uses indentation indicator (strip)', () => {
        const result = convertToBlockScalar('key: "\\nline1"', '|-');
        assert.ok(result !== null);
        assert.match(result, /^key: \|[0-9]+-/);
    });

    test('value starting with newline uses indentation indicator (clip)', () => {
        const result = convertToBlockScalar('key: "\\nline1"', '|');
        assert.ok(result !== null);
        assert.match(result, /^key: \|[0-9]+\n/);
    });

    test('value starting with newline uses indentation indicator (keep)', () => {
        const result = convertToBlockScalar('key: "\\nline1"', '|+');
        assert.ok(result !== null);
        assert.match(result, /^key: \|[0-9]+\+/);
    });

    test('value with Unicode escape \\uXXXX (4 digits) decoded correctly', () => {
        const result = convertToBlockScalar('key: "Hello \\u2764 World"', '|-');
        assert.equal(result, 'key: |-\n  Hello ❤ World');
    });

    test('value with Unicode escape \\UXXXXXXXX (8 digits) decoded correctly', () => {
        const result = convertToBlockScalar('key: "\\U0001F389 Party time!"', '|-');
        assert.equal(result, 'key: |-\n  🎉 Party time!');
    });

    test('complex value with Unicode and escaped quotes', () => {
        const input = 'z: "={ Message: \\"\\U0001F389 Checklist has been successfully submitted\\",\\n                    Message2: \\"Great work! Small achievements lead to big victories. Keep going!\\"\\n                }\\n"';
        const result = convertToBlockScalar(input, '|-');
        assert.ok(result !== null);
        assert.ok(result.includes('🎉 Checklist'));
    });
});
