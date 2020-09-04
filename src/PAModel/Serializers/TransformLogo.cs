// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AppMagic.Authoring.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PAModel.Serializers
{    
    // Logo file has a random filename that is continually regenerated, which creates Noisy Diffs.        
    // Find the file - based on the PublishInfo.LogoFileName and pull it out. 
    // Normalize name (logo.jpg), touchup PublishInfo so that it's stable.
    // Save the old name in Entropy so that we can still roundtrip. 
    internal static class TransformLogo
    {
        public static void TranformLogoOnLoad(this MsApp app)
        {
            // May be null or "" 
            var oldLogoName = app._publishInfo.LogoFileName;
            if (!string.IsNullOrEmpty(oldLogoName))
            {
                string newLogoName = "logo" + Path.GetExtension(oldLogoName);

                FileEntry logoFile;
                var oldKey = @"Resources\" + oldLogoName;
                if (app._unknownFiles.TryGetValue(oldKey, out logoFile))
                {
                    app._unknownFiles.Remove(oldKey);

                    logoFile.Name = @"Resources\" + newLogoName;
                    app._logoFile = logoFile;


                    app._entropy.SetLogoFileName(oldLogoName);
                    app._publishInfo.LogoFileName = newLogoName;
                }
            }
        }
                

        // Get the original logo file (using entropy to get the old name) 
        // And return a touched publishInfo pointing to it.
        public static (PublishInfoJson, FileEntry) TransformLogoOnSave(this MsApp app)
        {
            FileEntry logoFile = null;
            var publishInfo = app._publishInfo.JsonClone();

            if (!string.IsNullOrEmpty(publishInfo.LogoFileName))
            {
                publishInfo.LogoFileName = app._entropy.OldLogoFileName ?? Path.GetFileName(app._logoFile.Name);
                logoFile = new FileEntry
                {
                    Name = @"Resources\" + publishInfo.LogoFileName,
                    RawBytes = app._logoFile.RawBytes
                };
            }

            return (publishInfo, logoFile);
        }
    }

}
