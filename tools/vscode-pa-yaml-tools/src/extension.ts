import * as vscode from 'vscode';
import { detectDoubleQuotedScalar, convertToBlockScalar, BlockScalarStyle } from './yamlScalarConverter';

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
}

export function deactivate(): void {
    // nothing to clean up
}
