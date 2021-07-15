using Microsoft.PowerPlatform.Formulas.Tools.Yaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.PowerPlatform
{
    /// <summary>
    /// Convert a dictionary to and from yaml
    /// </summary>
    public static class YamlConverter
    {
        public static Dictionary<string, string> Read(TextReader reader, string filenameHint = null)
        {
            var properties = new Dictionary<string, string>(StringComparer.Ordinal);

            var yaml = new YamlLexer(reader, filenameHint);

            while (true)
            {
                var t = yaml.ReadNext();
                if (t.Kind == YamlTokenKind.EndOfFile)
                {
                    break;
                }

                if (t.Kind != YamlTokenKind.Property)
                {
                    // $$$ error
                    t = YamlToken.NewError(t.Span, "Only properties are supported in this context");

                }
                if (t.Kind == YamlTokenKind.Error)
                {
                    // ToString will include source span  and message.
                    throw new InvalidOperationException(t.ToString());
                }

                properties[t.Property] = t.Value;
            }
            return properties;
        }

        public static void Write(TextWriter writer, IDictionary<string, string> properties)
        {
            var yaml = new YamlWriter(writer);

            // Sort by keys to enforce canonical format. 
            foreach (var kv in properties.OrderBy(x => x.Key))
            {
                yaml.WriteProperty(kv.Key, kv.Value);
            }
            writer.Flush();
        }
    }
}
