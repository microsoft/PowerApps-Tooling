"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const yamlScalarConverter_1 = require("./yamlScalarConverter");
const jsonStringDecoder_1 = require("./jsonStringDecoder");
function activate(context) {
    const disposable = vscode.commands.registerCommand('pa-yaml.convertToBlockScalar', async () => {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            return;
        }
        const cursor = editor.selection.active;
        const lineText = editor.document.lineAt(cursor.line).text;
        const detected = (0, yamlScalarConverter_1.detectDoubleQuotedScalar)(lineText);
        if (!detected) {
            vscode.window.showWarningMessage('PA YAML: Current line is not a single-line double-quoted YAML scalar.');
            return;
        }
        const items = [
            { label: '|- (strip)', description: 'Trailing newlines are stripped', style: '|-' },
            { label: '|  (clip)', description: 'Exactly one trailing newline is added', style: '|' },
            { label: '|+ (keep)', description: 'All trailing newlines are preserved', style: '|+' },
        ];
        const picked = await vscode.window.showQuickPick(items, {
            placeHolder: 'Choose block scalar chomping style',
        });
        if (!picked) {
            return;
        }
        const result = (0, yamlScalarConverter_1.convertToBlockScalar)(lineText, picked.style);
        if (result === null) {
            vscode.window.showWarningMessage('PA YAML: Conversion failed.');
            return;
        }
        const lineRange = editor.document.lineAt(cursor.line).range;
        await editor.edit(editBuilder => {
            editBuilder.replace(lineRange, result);
        });
    });
    context.subscriptions.push(disposable);
    const jsonDisposable = vscode.commands.registerCommand('pa-yaml.decodeStringifiedJson', async () => {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            return;
        }
        if (editor.document.languageId !== 'json') {
            vscode.window.showWarningMessage('PA JSON: This command only works in JSON files.');
            return;
        }
        const cursor = editor.selection.active;
        const line = editor.document.lineAt(cursor.line);
        const token = (0, jsonStringDecoder_1.findJsonStringAtPosition)(line.text, cursor.character);
        if (!token) {
            vscode.window.showWarningMessage('PA JSON: Cursor is not inside a JSON string value.');
            return;
        }
        const result = (0, jsonStringDecoder_1.decodeStringifiedJson)(token.rawContent);
        if (!result.success) {
            vscode.window.showErrorMessage('PA JSON: ' + result.error);
            return;
        }
        // Indent all lines after the first to align with the start of the string token
        const lineIndent = line.text.slice(0, line.firstNonWhitespaceCharacterIndex);
        const indented = result.formatted
            .split('\n')
            .map((l, i) => (i === 0 ? l : lineIndent + l))
            .join('\n');
        const replaceRange = new vscode.Range(cursor.line, token.start, cursor.line, token.end);
        await editor.edit(editBuilder => {
            editBuilder.replace(replaceRange, indented);
        });
    });
    context.subscriptions.push(jsonDisposable);
}
function deactivate() {
    // nothing to clean up
}
//# sourceMappingURL=extension.js.map