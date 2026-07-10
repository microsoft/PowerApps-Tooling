# PA YAML Tools

A VS Code extension providing tools for working with Power Apps `.pa.yaml` files and inspecting `.msapp` contents.

## Features

### Convert to Block Scalar
Convert YAML literal scalars (single-line strings) to block scalars (multi-line format) for better readability.

**Usage:**
1. Open a `.pa.yaml` file in VS Code
2. Right-click in the editor
3. Select **"PA YAML: Convert to block scalar..."**

### Decode Stringified JSON
Decode escaped JSON strings in JSON files to make them readable.

**Usage:**
1. Open a JSON file in VS Code
2. Right-click in the editor
3. Select **"PA JSON: Decode stringified JSON"**

## Installation

### From VSIX Package

If you have a `.vsix` package file:

**Via VS Code UI:**
1. Open VS Code
2. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
3. Type "Extensions: Install from VSIX..."
4. Select the `.vsix` file

**Via command line:**
```bash
code --install-extension vscode-pa-yaml-tools-0.0.1.vsix
```

### From VS Code Marketplace

*(Coming soon)*

## Requirements

- VS Code 1.85.0 or higher

## Contributing

Interested in contributing? See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and guidelines.

## License

This extension is licensed under the MIT License.

