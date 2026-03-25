"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const node_test_1 = require("node:test");
const strict_1 = __importDefault(require("node:assert/strict"));
const yamlScalarConverter_1 = require("../yamlScalarConverter");
(0, node_test_1.describe)('detectDoubleQuotedScalar', () => {
    (0, node_test_1.test)('simple key: "value" matches', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)('key: "value"');
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.indent, '');
        strict_1.default.equal(result.key, 'key');
        strict_1.default.equal(result.rawValue, 'value');
    });
    (0, node_test_1.test)('indented property captures indent', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)('    Prop: "hello"');
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.indent, '    ');
        strict_1.default.equal(result.key, 'Prop');
        strict_1.default.equal(result.rawValue, 'hello');
    });
    (0, node_test_1.test)('single-quoted value returns null', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)("key: 'single'");
        strict_1.default.equal(result, null);
    });
    (0, node_test_1.test)('unquoted value returns null', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)('key: unquoted');
        strict_1.default.equal(result, null);
    });
    (0, node_test_1.test)('block scalar line returns null', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)('key: |');
        strict_1.default.equal(result, null);
    });
    (0, node_test_1.test)('value with escaped newline matches (rawValue contains literal \\n)', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)('key: "val1\\nval2"');
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.key, 'key');
        strict_1.default.equal(result.rawValue, 'val1\\nval2');
    });
    (0, node_test_1.test)('value with escaped quote matches', () => {
        const result = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)('key: "say \\"hi\\""');
        strict_1.default.ok(result !== null);
        strict_1.default.equal(result.rawValue, 'say \\"hi\\"');
    });
});
(0, node_test_1.describe)('convertToBlockScalar', () => {
    (0, node_test_1.test)('single-line value with strip style', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "value"', '|-');
        strict_1.default.equal(result, 'key: |-\n  value');
    });
    (0, node_test_1.test)('multi-line value with strip style produces correct lines', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "line1\\nline2\\nline3"', '|-');
        strict_1.default.equal(result, 'key: |-\n  line1\n  line2\n  line3');
    });
    (0, node_test_1.test)('strip style removes trailing empty line', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "line1\\nline2\\n"', '|-');
        strict_1.default.equal(result, 'key: |-\n  line1\n  line2');
    });
    (0, node_test_1.test)('clip style keeps trailing newline structure', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "line1\\nline2\\n"', '|');
        strict_1.default.equal(result, 'key: |\n  line1\n  line2\n  ');
    });
    (0, node_test_1.test)('keep style preserves trailing empty line', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "line1\\nline2\\n"', '|+');
        strict_1.default.equal(result, 'key: |+\n  line1\n  line2\n  ');
    });
    (0, node_test_1.test)('indented property uses correct content indent', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('  MyProp: "hello\\nworld"', '|-');
        strict_1.default.equal(result, '  MyProp: |-\n    hello\n    world');
    });
    (0, node_test_1.test)('value with escaped quote decoded correctly', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "say \\"hi\\""', '|-');
        strict_1.default.equal(result, 'key: |-\n  say "hi"');
    });
    (0, node_test_1.test)('value with tab decoded correctly', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "col1\\tcol2"', '|-');
        strict_1.default.equal(result, 'key: |-\n  col1\tcol2');
    });
    (0, node_test_1.test)('non-matching line returns null', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: unquoted', '|-');
        strict_1.default.equal(result, null);
    });
    (0, node_test_1.test)('value starting with newline uses indentation indicator (strip)', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "\\nline1"', '|-');
        strict_1.default.ok(result !== null);
        strict_1.default.match(result, /^key: \|[0-9]+-/);
    });
    (0, node_test_1.test)('value starting with newline uses indentation indicator (clip)', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "\\nline1"', '|');
        strict_1.default.ok(result !== null);
        strict_1.default.match(result, /^key: \|[0-9]+\n/);
    });
    (0, node_test_1.test)('value starting with newline uses indentation indicator (keep)', () => {
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)('key: "\\nline1"', '|+');
        strict_1.default.ok(result !== null);
        strict_1.default.match(result, /^key: \|[0-9]+\+/);
    });
});
//# sourceMappingURL=yamlScalarConverter.test.js.map