## Yaml Parser

We support a restricted subset of yaml. This is designed to leverage an existing file format (yaml) while avoiding some of the surprises in yaml (see https://noyaml.com/ ). 

1. **Single line properties must start with a '='**. This is to keep yaml from interpretting as a yaml expression, and instead l

```
prop: =12
```

2. **Multiline properties must use a '|' escape**. 

```
prop: |
  First
  Second
```

3. **Forbid '#' and ':' characters in singel line expressions**.  These must be multi-line escaped instead. 

```
Prop: |
   Set(Color,  #FF88CC) 
```

4. **Forbid spurious newlines**.  These will get lost when round tripping through studio. 

1. **Forbid empty objects**.  While Yaml allows empty objects, formulas do not. So these would likely be an indentation error. 



## The principles behind these rules:
1. **Easily embed the Power Apps formula as a DSL in the yaml properties**. 
We should be able to directly copy and paste text between the formula bar in Studio and these yaml files. 

2. **Roundtripping through the Studio on import/export**. 
This means we must be canonical to avoid noisy diffs. For example, extra newlines or '#' comments could get lost and won't round trip. 

3. **Warn on dangerous behavior**
For example:
-  '#' can naturally occur in PowerApps formulas. But '#' is also the Yaml comment delimeter and so can lead to formulas getting truncated. So disallow # comments.
- if Yaml has multiple properties with the same name, it won't error and instead just  take the last one. That could lead to silently losing properties. 

