// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;


namespace Microsoft.PowerPlatform.Formulas.Tools
{
    // Helpers for creating chceksums. 
    interface IHashMaker : IDisposable
    {
        void AppendStartObj();
        void AppendPropName(string name);
        void AppendPropNameSkip(string name);
        void AppendEndObj();
        void AppendStartArray();
        void AppendEndArray();

        void AppendData(string value);
        void AppendData(double value);
        void AppendData(bool value);
        void AppendNull();

        // Called after all Appends(). 
        byte[] GetFinalValue();
    }
     
    // Create a checksum using an incremental hash.    
    class Sha256HashMaker : IHashMaker, IDisposable
    {
        private readonly IncrementalHash _hash;

        public Sha256HashMaker()
        {
            _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        }

        public void AppendData(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            _hash.AppendData(bytes);
        }

        public void AppendData(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            _hash.AppendData(bytes);
        }

        public void AppendData(bool value)
        {
            var bytes = new byte[] { value ? (byte)1 : (byte)0 };
            _hash.AppendData(bytes);
        }

        public void AppendPropName(string name)
        {
            this.AppendData(name);
        }
        public void AppendPropNameSkip(string name)
        {
            // Skipped. 
        }

        public void AppendStartObj()
        {
        }

        public void AppendEndObj()
        {
        }

        public void AppendStartArray()
        {
        }

        public void AppendEndArray()
        {
        }

        public void AppendNull()
        {
            this.AppendData(false);
        }

        public void Dispose()
        {
            _hash.Dispose();
        }

        public byte[] GetFinalValue()
        {
            var key = _hash.GetHashAndReset();
            return key;
        }
    }


    // A debug version of the checksum maker that captures the full raw normalized input.
    // If a checksum doesn't match, re-run it with this algorithm and you can see and diff the raw inputs. 
    class DebugTextHashMaker : IHashMaker
    {
        private readonly Utf8JsonWriter _writer;
        private readonly MemoryStream _buffer = new MemoryStream();

        public DebugTextHashMaker()
        {            
            JsonWriterOptions opts = new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            _writer = new Utf8JsonWriter(_buffer, opts);
        }

        public void AppendData(string value)
        {
            _writer.WriteStringValue(value);
        }

        public void AppendData(double value)
        {
            _writer.WriteNumberValue(value);
        }

        public void AppendData(bool value)
        {
            _writer.WriteBooleanValue(value);
        }

        public void AppendEndArray()
        {
            _writer.WriteEndArray();
        }

        public void AppendEndObj()
        {
            _writer.WriteEndObject();
        }

        public void AppendNull()
        {
            _writer.WriteNullValue();
        }

        public void AppendPropName(string name)
        {
            _writer.WritePropertyName(name);
        }
        public void AppendPropNameSkip(string name)
        {
            this.AppendPropName(name);
        }

        public void AppendStartArray()
        {
            _writer.WriteStartArray();
        }

        public void AppendStartObj()
        {
            _writer.WriteStartObject();
        }

        public void Dispose()
        {
            _writer.Dispose();
            _buffer.Dispose();
        }

        public byte[] GetFinalValue()
        {
            _writer.Flush();

            _buffer.Position = 0;
            var bytes = this._buffer.ToArray();            
            return bytes;
        }
    }
}
