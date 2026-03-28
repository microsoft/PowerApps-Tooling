import * as vscode from 'vscode';
import { detectDoubleQuotedScalar, convertToBlockScalar, BlockScalarStyle } from './yamlScalarConverter';
import { findJsonStringAtPosition, decodeStringifiedJson } from './jsonStringDecoder';

export function activate(context: vscode.ExtensionContext): void {
    const disposable = vscode.commands.registerCommand('pa-yaml.convertToBlockScalar', async () => {
        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            return;
        }

        const cursor = editor.selection.active;
        const lineText = editor.document.lineAt(cursor.line).text;

        const detected = detectDoubleQuotedScalar(lineText);
        if (!detected) {
            vscode.window.showWarningMessage(
                'PA YAML: Current line is not a single-line double-quoted YAML scalar.'
            );
            return;
        }

        const items: Array<{ label: string; description: string; style: BlockScalarStyle }> = [
            { label: '|- (strip)', description: 'Trailing newlines are stripped',       style: '|-' },
            { label: '|  (clip)',  description: 'Exactly one trailing newline is added', style: '|'  },
            { label: '|+ (keep)', description: 'All trailing newlines are preserved',   style: '|+' },
        ];

        const picked = await vscode.window.showQuickPick(items, {
            placeHolder: 'Choose block scalar chomping style',
        });

        if (!picked) {
            return;
        }

        const result = convertToBlockScalar(lineText, picked.style);
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
        const token = findJsonStringAtPosition(line.text, cursor.character);

        if (!token) {
            vscode.window.showWarningMessage('PA JSON: Cursor is not inside a JSON string value.');
            return;
        }

        const result = decodeStringifiedJson(token.rawContent);

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

        const replaceRange = new vscode.Range(
            cursor.line, token.start,
            cursor.line, token.end
        );

        await editor.edit(editBuilder => {
            editBuilder.replace(replaceRange, indented);
        });
    });

    context.subscriptions.push(jsonDisposable);
}

export function deactivate(): void {
    // nothing to clean up
}
