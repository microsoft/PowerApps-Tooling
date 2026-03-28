"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const node_test_1 = require("node:test");
const strict_1 = __importDefault(require("node:assert/strict"));
const jsonStringDecoder_1 = require("../jsonStringDecoder");
(0, node_test_1.describe)('findJsonStringAtPosition', () => {
    (0, node_test_1.test)('cursor inside the only string returns that token', () => {
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)('"hello"', 3);
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.start, 0);
        strict_1.default.equal(result.end, 7);
        strict_1.default.equal(result.rawContent, '"hello"');
    });
    (0, node_test_1.test)('cursor on opening quote returns token', () => {
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)('"hello"', 0);
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.rawContent, '"hello"');
    });
    (0, node_test_1.test)('cursor on last character before closing quote returns token', () => {
        // "hello" — closing quote is at index 6, so column 5 ('o') is still inside
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)('"hello"', 5);
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.rawContent, '"hello"');
    });
    (0, node_test_1.test)('cursor past end of string returns null', () => {
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)('"hello"', 7);
        strict_1.default.equal(result, null);
    });
    (0, node_test_1.test)('cursor on second of two strings on same line', () => {
        const line = '  "key": "value"';
        // "value" starts at index 9
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)(line, 12);
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.rawContent, '"value"');
    });
    (0, node_test_1.test)('string with escaped content is captured correctly', () => {
        const line = '"{\\"foo\\":1}"';
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)(line, 5);
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.rawContent, '"{\\"foo\\":1}"');
    });
    (0, node_test_1.test)('cursor not in any string returns null', () => {
        const result = (0, jsonStringDecoder_1.findJsonStringAtPosition)('  "key": "value"', 7); // the colon
        strict_1.default.equal(result, null);
    });
});
(0, node_test_1.describe)('decodeStringifiedJson', () => {
    (0, node_test_1.test)('success: decodes a stringified object', () => {
        // rawContent includes outer quotes, inner content is JSON-escaped
        const rawContent = '"{\\"foo\\":1,\\"bar\\":\\"baz\\"}"';
        const result = (0, jsonStringDecoder_1.decodeStringifiedJson)(rawContent);
        strict_1.default.equal(result.success, true);
        if (result.success) {
            const parsed = JSON.parse(result.formatted);
            strict_1.default.deepEqual(parsed, { foo: 1, bar: 'baz' });
        }
    });
    (0, node_test_1.test)('success: decodes a stringified array', () => {
        const rawContent = '"[1,2,3]"';
        const result = (0, jsonStringDecoder_1.decodeStringifiedJson)(rawContent);
        strict_1.default.equal(result.success, true);
        if (result.success) {
            strict_1.default.deepEqual(JSON.parse(result.formatted), [1, 2, 3]);
        }
    });
    (0, node_test_1.test)('success: formatted output uses 2-space indent', () => {
        const rawContent = '"{\\"a\\":1}"';
        const result = (0, jsonStringDecoder_1.decodeStringifiedJson)(rawContent);
        strict_1.default.equal(result.success, true);
        if (result.success) {
            strict_1.default.equal(result.formatted, '{\n  "a": 1\n}');
        }
    });
    (0, node_test_1.test)('failure: invalid JSON string returns error message', () => {
        // Outer string is valid JSON ("not valid json"), but inner content is not JSON
        const rawContent = '"not valid json"';
        const result = (0, jsonStringDecoder_1.decodeStringifiedJson)(rawContent);
        strict_1.default.equal(result.success, false);
        if (!result.success) {
            strict_1.default.ok(result.error.length > 0);
        }
    });
    (0, node_test_1.test)('failure: truncated JSON returns error message', () => {
        // Inner content is {"foo"} — missing the value, not valid JSON
        const rawContent = '"{\\"foo\\"}"';
        const result = (0, jsonStringDecoder_1.decodeStringifiedJson)(rawContent);
        strict_1.default.equal(result.success, false);
        if (!result.success) {
            strict_1.default.ok(result.error.length > 0);
        }
    });
});
//# sourceMappingURL=jsonStringDecoder.test.js.map