using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PAModel
{
    // Various data that we can save for round-tripping.
    // Everything here is optional!!
    // Only be written during MsApp. Opaque for source file. 
    class Entropy
    {
        // Json serialize these. 
        public Dictionary<string, string> TemplateVersions { get; set; }  = new Dictionary<string, string>();

        public void SetTemplateVersion(string dataComponentGuid, string version)
        {
            TemplateVersions[dataComponentGuid] = version;
        }

        public string GetTemplateVersion(string dataComponentGuid)
        {
            string version;
            TemplateVersions.TryGetValue(dataComponentGuid, out version);

            // Version string is ok to be null. 
            // DateTime.Now.ToUniversalTime().Ticks.ToString();
            return version;
        }
    }
}
