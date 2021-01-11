# Yaml Parser

We support a restricted subset of yaml. This is designed to leverage an existing file format (yaml) while avoiding some of the surprises in yaml that would confuse our excel-oriented audience (see https://noyaml.com/ ). 

We wrote our own YAML lexer here because:

1. There was no existing Microsoft-supported YAML parser. The closest C# parser is https://github.com/aaubry/YamlDotNet .  
1. We needed fine grain control for Writing to emit our specific subset and also to gaurantee we can lexically round-trip our files without any spurious diffs.  For example, control to bias to a '|' multiline escape instead of using escape characters and single lines. YamlDotNet also biases to a '>' escape.
1. Fine grain control on reading to warn on unsafe behavior like '#' in formulas that may get treated as comments, or duplicate property names in an object. 

Our subset of YAML is described in the [docs/YAMLFileFormat.md](/docs/YAMLFileFormat.md).




