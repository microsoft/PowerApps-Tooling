// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Backdoor.Repl
{
    public class Parser
    {
        public static IEnumerable<string> Tokenize(string input)
        {
            var start = 0;
            var end = 0;
            var tokens = new List<string>();
            var inQuotes = false;
            for (var i = 0; i < input.Length; i++)
            {
                var character = input[i];
                switch (character)
                {
                    case ' ':
                        if (inQuotes)
                        {
                            end += 1;
                            break;
                        }

                        if (TryGetToken(start, end, input, out string token))
                        {
                            tokens.Add(token);
                        }
                        start = end = i + 1;
                        break;

                    case '"':
                        inQuotes = !inQuotes;
                        end += 1;
                        break;
                    default:
                        end += 1;
                        break;
                }
            }

            if (TryGetToken(start, end, input, out string finalToken))
            {
                tokens.Add(finalToken);
            }

            return tokens;
        }

        private static bool TryGetToken(int start, int end, string input, out string s)
        {
            if (start < 0 || end < 0 || end <= start || end > input.Length)
            {
                s = default(string);
                return false;
            }

            s = input.Substring(start, end - start);
            return true;
        }
    }
}
