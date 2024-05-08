// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions.Specialized;
using Microsoft.PowerPlatform.PowerApps.Persistence;

namespace Persistence.Tests;

[TestClass]
public class PersistenceExceptionTests
{
    [TestMethod]
    public void ConstructTest()
    {
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.DeserializationError))
            .WithErrorCode(PersistenceErrorCode.DeserializationError)
            .WithReason(null);
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.SerializationError, "A test reason."))
            .WithErrorCode(PersistenceErrorCode.SerializationError)
            .WithReason("A test reason.");
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.MsappArchiveError, "A test reason2.")
        {
            MsappEntryFullPath = "src/entry1.txt",
            LineNumber = 5,
            Column = 3,
            JsonPath = "some.json.path[0].value",
        })
            .WithErrorCode(PersistenceErrorCode.MsappArchiveError)
            .WithReason("A test reason2.");
    }

    [TestMethod]
    public void MessageTest()
    {
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.DeserializationError))
            .WithMessage("[3000:DeserializationError] An error occurred during deserialization.");
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.SerializationError, "A test reason."))
            .WithMessage("[2000:SerializationError] An error occurred during serialization. A test reason.");
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.YamlInvalidSyntax)
        {
            LineNumber = 5,
            Column = 3,
        })
            .WithMessage("[3001:YamlInvalidSyntax] Invalid YAML syntax was encountered during deserialization. Line: 5; Column: 3;");
        ThrowAndVerify(new PaPersistenceException(PersistenceErrorCode.MsappArchiveError, "A test reason2.")
        {
            MsappEntryFullPath = "src/entry1.txt",
            LineNumber = 5,
            Column = 3,
            JsonPath = "some.json.path[0].value",
        })
            .WithMessage("[5000:MsappArchiveError] An error was detected in an msapp file. A test reason2. Line: 5; Column: 3; MsappEntry: src/entry1.txt; JsonPath: some.json.path[0].value;");
    }

    [TestMethod]
    public void IsSerializableTests()
    {
        new PaPersistenceException(PersistenceErrorCode.DeserializationError).Should().BeBinarySerializable();
        new PaPersistenceException(PersistenceErrorCode.SerializationError, "A test reason.").Should().BeBinarySerializable();
        new PaPersistenceException(PersistenceErrorCode.MsappArchiveError, "A test reason2.")
        {
            MsappEntryFullPath = "src/entry1.txt",
            LineNumber = 5,
            Column = 3,
            JsonPath = "some.json.path[0].value",
        }.Should().BeBinarySerializable();
    }

    /// <summary>
    /// Throws the specified exception and then asserts it was thrown,
    /// returning the <see cref="ExceptionAssertions{TException}"/> allowing caller to
    /// continue asserting additional.
    /// </summary>
    private static ExceptionAssertions<T> ThrowAndVerify<T>(T ex) where T : Exception
    {
        var act = () => { throw ex; };
        return act.Should().ThrowExactly<T>();
    }
}
