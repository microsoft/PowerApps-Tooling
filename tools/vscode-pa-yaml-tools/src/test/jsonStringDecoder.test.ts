import { test, describe } from 'node:test';
import assert from 'node:assert/strict';
import { findJsonStringAtPosition, decodeStringifiedJson } from '../jsonStringDecoder';

describe('findJsonStringAtPosition', () => {
    test('cursor inside the only string returns that token', () => {
        const result = findJsonStringAtPosition('"hello"', 3);
        assert.ok(result !== null);
        assert.equal(result!.start, 0);
        assert.equal(result!.end, 7);
        assert.equal(result!.rawContent, '"hello"');
    });

    test('cursor on opening quote returns token', () => {
        const result = findJsonStringAtPosition('"hello"', 0);
        assert.ok(result !== null);
        assert.equal(result!.rawContent, '"hello"');
    });

    test('cursor on last character before closing quote returns token', () => {
        // "hello" — closing quote is at index 6, so column 5 ('o') is still inside
        const result = findJsonStringAtPosition('"hello"', 5);
        assert.ok(result !== null);
        assert.equal(result!.rawContent, '"hello"');
    });

    test('cursor past end of string returns null', () => {
        const result = findJsonStringAtPosition('"hello"', 7);
        assert.equal(result, null);
    });

    test('cursor on second of two strings on same line', () => {
        const line = '  "key": "value"';
        // "value" starts at index 9
        const result = findJsonStringAtPosition(line, 12);
        assert.ok(result !== null);
        assert.equal(result!.rawContent, '"value"');
    });

    test('string with escaped content is captured correctly', () => {
        const line = '"{\\"foo\\":1}"';
        const result = findJsonStringAtPosition(line, 5);
        assert.ok(result !== null);
        assert.equal(result!.rawContent, '"{\\"foo\\":1}"');
    });

    test('cursor not in any string returns null', () => {
        const result = findJsonStringAtPosition('  "key": "value"', 7); // the colon
        assert.equal(result, null);
    });
});

describe('decodeStringifiedJson', () => {
    test('success: decodes a stringified object', () => {
        // rawContent includes outer quotes, inner content is JSON-escaped
        const rawContent = '"{\\"foo\\":1,\\"bar\\":\\"baz\\"}"';
        const result = decodeStringifiedJson(rawContent);
        assert.equal(result.success, true);
        if (result.success) {
            const parsed = JSON.parse(result.formatted) as unknown;
            assert.deepEqual(parsed, { foo: 1, bar: 'baz' });
        }
    });

    test('success: decodes a stringified array', () => {
        const rawContent = '"[1,2,3]"';
        const result = decodeStringifiedJson(rawContent);
        assert.equal(result.success, true);
        if (result.success) {
            assert.deepEqual(JSON.parse(result.formatted), [1, 2, 3]);
        }
    });

    test('success: formatted output uses 2-space indent', () => {
        const rawContent = '"{\\"a\\":1}"';
        const result = decodeStringifiedJson(rawContent);
        assert.equal(result.success, true);
        if (result.success) {
            assert.equal(result.formatted, '{\n  "a": 1\n}');
        }
    });

    test('failure: invalid JSON string returns error message', () => {
        // Outer string is valid JSON ("not valid json"), but inner content is not JSON
        const rawContent = '"not valid json"';
        const result = decodeStringifiedJson(rawContent);
        assert.equal(result.success, false);
        if (!result.success) {
            assert.ok(result.error.length > 0);
        }
    });

    test('failure: truncated JSON returns error message', () => {
        // Inner content is {"foo"} — missing the value, not valid JSON
        const rawContent = '"{\\"foo\\"}"';
        const result = decodeStringifiedJson(rawContent);
        assert.equal(result.success, false);
        if (!result.success) {
            assert.ok(result.error.length > 0);
        }
    });
});
