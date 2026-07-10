# Contributing to PA YAML Tools

Thank you for your interest in contributing to this VS Code extension!

## Development Setup

### Prerequisites

- Node.js (version 20.x or higher)
- npm (usually comes with Node.js)
- VS Code (version 1.85.0 or higher)

### Getting Started

1. **Clone the repository and navigate to the extension directory:**
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

4. **Run tests:**
   ```bash
   npm test
   ```

## Development Workflow

### Running in Development Mode

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

### Installing from Source

You have three options for testing your local build:

**Option A: Install from VSIX package (recommended)**

First, create the package:
```bash
npm run package
```

Then install it:
```bash
code --install-extension vscode-pa-yaml-tools-0.0.1.vsix
```

**Option B: Install from source directory**
```bash
code --install-extension .
```

**Option C: Using symlink/copy method**
- Copy or symlink this folder to your VS Code extensions directory:
  - **Windows**: `%USERPROFILE%\.vscode\extensions\vscode-pa-yaml-tools-0.0.1`
  - **macOS/Linux**: `~/.vscode/extensions/vscode-pa-yaml-tools-0.0.1`

After installation, reload VS Code:
- Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on macOS)
- Type "Reload Window" and select "Developer: Reload Window"

## Building for Distribution

To create a distributable `.vsix` package:

```bash
npm run package
```

This creates `vscode-pa-yaml-tools-0.0.1.vsix` in the current directory.

For pre-release versions:
```bash
npm run package:prerelease
```

## Project Structure

```
vscode-pa-yaml-tools/
├── src/                          # TypeScript source files
│   ├── extension.ts              # Extension entry point
│   ├── yamlScalarConverter.ts    # YAML conversion logic
│   ├── jsonStringDecoder.ts      # JSON decoding logic
│   └── test/                     # Test files
├── out/                          # Compiled JavaScript output
├── package.json                  # Extension manifest
├── tsconfig.json                 # TypeScript configuration
└── .vscodeignore                 # Files to exclude from package
```

## Troubleshooting

- If the extension doesn't appear after installation, ensure you've reloaded the VS Code window
- If compilation fails, make sure you have TypeScript installed: `npm list typescript`
- Check the output in VS Code's Developer Console (`Help > Toggle Developer Tools`) for any errors
- If tests fail, ensure you've run `npm run compile` first

## Code Style

This project follows standard TypeScript conventions. Please ensure your code:
- Passes TypeScript compilation without errors
- Includes appropriate error handling
- Has tests for new features

## Testing

Run the test suite with:
```bash
npm test
```

This compiles the code and runs all tests in the `out/test/` directory.

## Questions or Issues?

If you have questions or encounter issues, please open an issue on the GitHub repository.
