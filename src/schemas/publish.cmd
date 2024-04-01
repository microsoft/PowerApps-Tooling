:: SETLOCAL will also autoreset the current directory
@SETLOCAL
@CD %~dp0

@SET _RepoRoot=%~dp0..\..\
@SET _SchemasDistRoot=%_RepoRoot%schemas\


:: TODO: Instead of a straight copy, we should remove yaml comments; only keeping '$comment'
:: TODO: We MAY want to also publish the *.schema.json files too.
@ECHO Copying pa-yaml/v2.2 ...
@RMDIR /S /Q "%_SchemasDistRoot%pa-yaml\v2.2\"
@MKDIR "%_SchemasDistRoot%pa-yaml\v2.2\"
@COPY pa-yaml\v2.2\pa.schema.yaml "%_SchemasDistRoot%pa-yaml\v2.2\"
