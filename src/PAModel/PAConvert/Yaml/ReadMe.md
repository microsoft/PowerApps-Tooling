# Yaml Parser

We support a restricted subset of yaml. This is designed to leverage an existing file format (yaml) while avoiding some of the surprises in yaml that would confuse our excel-oriented audience (see https://noyaml.com/ ). 

We wrote our own YAML lexer here because:

1. There was no existing Microsoft-supported YAML parser. The closest C# parser is https://github.com/aaubry/YamlDotNet .  
1. We needed fine grain control for Writing to emit our specific subset and also to gaurantee we can lexically round-trip our files without any spurious diffs.  For example, control to bias to a '|' multiline escape instead of using escape characters and single lines. YamlDotNet also biases to a '>' escape.
1. Fine grain control on reading to warn on unsafe behavior like '#' in formulas that may get treated as comments, or duplicate property names in an object. 

## Rules for restricted subset:

1. **Single line properties must start with a '='**. This is to keep yaml from interpretting as a yaml expression, and instead l

```
prop: =12
```

2. **Multiline properties must use a '|' escape**.  This is to facilitate direct copy and paste between Studio and text, particularly for preserving newlines. We can choose between |-,|,|+ to preserve the correct trailing newline.  Avoid '>' because that will interfere with newlines in the middle of the content. 
Multiline properties still start with an '=' to be consistent with single-line properties.

```
prop: |
  =First
  Second
```

3. **Forbid '#' and ':' characters in single line expressions**.  These must be multi-line escaped instead. 

```
Prop: |
   =Set(Color,  #FF88CC) 
```

4. **Forbid empty objects**.  While Yaml allows empty objects, formulas do not. So these would likely be an indentation error. 



## The principles behind these rules:
1. **Easily embed the Power Apps formula as a DSL in the yaml properties**. 
We should be able to directly copy and paste text between the formula bar in Studio and these yaml files. 
One implication here is to avoid single-line escaping (single and double quotes), and instead use the '|' multiline block to avoid escaping. 

2. **Roundtripping through the Studio on import/export**. 
This means we must be canonical to avoid noisy diffs. For example, extra newlines or '#' comments could get lost and won't round trip. 

3. **Warn on dangerous behavior**
For example:
-  '#' can naturally occur in PowerApps formulas. But '#' is also the Yaml comment delimeter and so can lead to formulas getting truncated. So disallow # comments.
- if Yaml has multiple properties with the same name, it won't error and instead just  take the last one. That could lead to silently losing properties. 

