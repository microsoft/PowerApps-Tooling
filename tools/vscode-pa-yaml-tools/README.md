# vscode-pa-yaml-tools

A set of tools for working with PaYaml (*.pa.yaml) files along with other tools useful for inspecting msapp contents.

## Features

- **Convert to block scalar**: Convert YAML literal scalars (single line strings) to block scalars (multi-line format) for better readability
- **Decode stringified JSON**: Decode escaped JSON strings in JSON files to make them readable

## How to build and install on local machine

Follow these steps to build and run this extension locally without having to publish it to the marketplace.

### Prerequisites

- Node.js (version 20.x or higher)
- npm (usually comes with Node.js)
- VS Code (version 1.85.0 or higher)

### Build and Install Steps

1. **Navigate to the extension directory:**
   ```bash
   cd tools/vscode-pa-yaml-tools
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Compile the TypeScript source:**
   ```bash
   npm run compile
   ```

   This will compile the TypeScript files from `src/` to JavaScript in the `out/` directory.

4. **Run tests (optional but recommended):**
   ```bash
   npm test
   ```

5. **Install the extension in VS Code:**

   You have two options:

   **Option A: Using the VS Code command line (recommended)**
   ```bash
   code --install-extension .
   ```

   **Option B: Using symlink/copy method**
   - Copy or symlink this folder to your VS Code extensions directory:
     - **Windows**: `%USERPROFILE%\.vscode\extensions\vscode-pa-yaml-tools-0.0.1`
     - **macOS/Linux**: `~/.vscode/extensions/vscode-pa-yaml-tools-0.0.1`

6. **Reload VS Code:**
   - Open VS Code
   - Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
   - Type "Reload Window" and select "Developer: Reload Window"

### Using the Extension

After installation, the extension adds commands to the context menu:

- **In YAML files** (*.pa.yaml): Right-click and select "PA YAML: Convert to block scalar..."
- **In JSON files**: Right-click and select "PA JSON: Decode stringified JSON"

### Development Mode

For active development, you can run the extension in debug mode:

1. Open this folder in VS Code
2. Press `F5` to launch the Extension Development Host
3. This opens a new VS Code window with the extension loaded
4. Make changes to the source code
5. Press `Ctrl+R` (or `Cmd+R` on macOS) in the Extension Development Host to reload changes

Alternatively, use watch mode to automatically recompile on file changes:
```bash
npm run watch
```

### Troubleshooting

- If the extension doesn't appear after installation, ensure you've reloaded the VS Code window
- If compilation fails, make sure you have TypeScript installed: `npm list typescript`
- Check the output in VS Code's Developer Console (`Help > Toggle Developer Tools`) for any errors

