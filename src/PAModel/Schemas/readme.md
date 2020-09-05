
This tool must be able to faithfully roundtrip an .msapp file to text and back. The contents of the .msapp are primarily json. 

## Challenges 
1. Json is not a "canonical" representation - it can vary with whitespace, object property ordering, trailing 0s in a number, string escapes, etc. 
1. Json arrays are ordered, but the server arrays in the .msapp are often still unordered - so round tripping needs to normalize order. 
1. The server emits things like timestamps and version numbers - which will cause "noisy diffs". 
1. The server schema may vary as new properties are added. Roundtripping needs to preservrve 


## Steps we take

1. When unpacking, immediately repack the app and compare. Thus if we have a problem roundtripping, we know upfront. 
2. On unpacking, we collect noisy diffs (like timestamps) and emit them to new file 'entropy.json'. The entropy file has noisy diffs, but the other files should then be stable. 
3. We use System.Text.JSon parser to parse the json and share converters and some structures from the server code.  
4. Client writes out normalized json and enforces array ordering. 
5. Create a checksum. The client and server can use this to detect offline changes.  

## Versioning

The client's view of the structures doesn't need the full set of server properties - this would be inflexible and  break as the server adds new properties. Instead:  
- Client structures just need the *minimal properties* for decoding the msapp. 
- Use `[JsonExtensionData]` to collect all other unrecognized properties. Pass this as a property bag through round-tripping. 

Get the schema objects and converters from: 
 https://msazure.visualstudio.com/OneAgile/_git/PowerApps-Client?path=%2Fsrc%2FCloud%2FDocumentServer.Core%2FDocument%2FDocument%2FPersistence%2FSerialization%2FSchemas&version=GBmaster&_a=contents


 ## Checksums
 The checksum is not cryptographic and does not use a secret because a client tool can't secure the secret - and we don't want the increase in complexity to send to a service to sign it. 
