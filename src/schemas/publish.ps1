$ErrorActionPreference = "Stop"
Set-StrictMode -Version 3.0

function BuildSchemaForPublish([string]$srcSchemaFilePath, [string]$outputFilePath) {
    #Copy-Item -Path $srcSchemaFilePath -Destination $outputFilePath
    $contentLines = Get-Content $srcSchemaFilePath;

    # Remove whitespace at beginning of lines
    # This makes the check for blank lines easier by allowing a simple IndexOf('')
    $contentLines = $contentLines | foreach {
        if ($_ -match '^\s+# \[schema-build-[a-z-]+\]\s*$') {
            $_ -replace '^\s+', ''
        } else {
            $_
        }
    };

    # Uncomment lines requested.
    for ($lineIdx = $contentLines.IndexOf('# [schema-build-uncomment-next-line]'); $lineIdx -ge 0; $lineIdx = $contentLines.IndexOf('# [schema-build-uncomment-next-line]')) {
        $contentLines[$lineIdx] = '# ';
        $contentLines[$lineIdx+1] = $contentLines[$lineIdx+1] -replace '#', '';
    }

    # Remove lines requested
    for ($lineIdx = $contentLines.IndexOf('# [schema-build-remove-next-line]'); $lineIdx -ge 0; $lineIdx = $contentLines.IndexOf('# [schema-build-remove-next-line]')) {
        # instead of removing the lines, just comment them out so they'll get removed later
        $contentLines[$lineIdx] = '# ';
        $contentLines[$lineIdx+1] = '# ';
    }

    # Remove comment lines as these are intended only as source code comments.
    # Schema owners should utilize $comment properties to add comments intended for public consumption.
    $contentLines = $contentLines | where {$_ -notmatch '^\s*#.*?$'};

    # Remove blank lines at the beginning
    $contentLines = [Linq.Enumerable]::ToArray( `
        [Linq.Enumerable]::SkipWhile([string[]]$contentLines, [Func[string, bool]] { param($line) [string]::IsNullOrWhiteSpace($line) }) `
    );

    Set-Content -Path $outputFilePath $contentLines
}

$repoRoot = [IO.Path]::GetFullPath("$PSScriptRoot/../..");
$schemaSrcRoot = "$repoRoot/src/schemas";
$schemasDistRoot = "$repoRoot/schemas";

Write-Host "Copying pa-yaml/v3.0..."
$sourceFolder = "$schemaSrcRoot/pa-yaml/v3.0";
$outputFolder = "$schemasDistRoot/pa-yaml/v3.0";
if (Test-Path $outputFolder -PathType Container) {
    [IO.Directory]::Delete($outputFolder, $true);
}

$null = [IO.Directory]::CreateDirectory($outputFolder);

BuildSchemaForPublish "$sourceFolder/pa.schema.yaml" "$outputFolder/pa.schema.yaml"
