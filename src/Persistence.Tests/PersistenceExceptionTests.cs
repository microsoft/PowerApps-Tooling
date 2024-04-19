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
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.DeserializationError))
            .WithErrorCode(PersistenceErrorCode.DeserializationError)
            .WithReason(string.Empty, "Reason is defaulted to empty string to avoid it returning the default Exception.Message string.");
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.SerializationError, "A test reason."))
            .WithErrorCode(PersistenceErrorCode.SerializationError)
            .WithReason("A test reason.");
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.MsappArchiveError, "A test reason2.")
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
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.DeserializationError))
            .WithMessage("[3000:DeserializationError] An error occurred during deserialization.");
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.SerializationError, "A test reason."))
            .WithMessage("[2000:SerializationError] An error occurred during serialization. A test reason.");
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.YamlInvalidSyntax)
        {
            LineNumber = 5,
            Column = 3,
        })
            .WithMessage("[3001:YamlInvalidSyntax] Invalid YAML syntax was encountered during deserialization. Line: 5; Column: 3;");
        ThrowAndVerify(new PersistenceException(PersistenceErrorCode.MsappArchiveError, "A test reason2.")
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
        new PersistenceException(PersistenceErrorCode.DeserializationError).Should().BeBinarySerializable();
        new PersistenceException(PersistenceErrorCode.SerializationError, "A test reason.").Should().BeBinarySerializable();
        new PersistenceException(PersistenceErrorCode.MsappArchiveError, "A test reason2.")
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
