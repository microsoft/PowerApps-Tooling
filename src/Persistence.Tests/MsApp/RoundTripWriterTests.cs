// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.PowerPlatform.PowerApps.Persistence;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;

namespace Persistence.Tests.MsApp;

[TestClass]
public class RoundTripWriterTests
{
    [TestMethod]
    [DataRow("input", "input")]
    [DataRow("input : string", "input : string")]
    [DataRow("", "")]
    [DataRow("\r\n", "\n")]
    [DataRow("\n", "\r\n")]
    [DataRow("input : \nstring", "input : \nstring")]
    [DataRow("input : \r\nstring", "input : \nstring")]
    [DataRow("input : \r\nstring", "input : \r\nstring")]
    [DataRow("input : \nstring", "input : \r\nstring")]
    public void Write_Should_Be_Identical(string input, string output)
    {
        // Arrange
        var inputReader = new StringReader(input);
        using var roundTripWriter = new RoundTripWriter(inputReader, "test");

        // Act
        foreach (var c in output)
        {
            roundTripWriter.Write(c);
        }
    }

    [TestMethod]
    [DataRow(" ab", "ab", 1, 1)]
    [DataRow("ab", " ab", 1, 1)]
    [DataRow("input", "input2", 1, 6)]
    [DataRow("input2", "input", 1, 5)]
    [DataRow("input : value", "input : value ", 1, 14)]
    [DataRow("input : value ", "input : value", 1, 13)]
    [DataRow("input : \r\nvalue ", "input : \r\nvalue", 2, 5)]
    [DataRow("input : \r\nvalue", "input : \r\nValue", 2, 1)]
    [DataRow("input : \r\nvalue", "input : \nValue", 2, 1)]
    public void Write_Should_Fail(string input, string output, int line, int column)
    {
        // Arrange
        var inputReader = new StringReader(input);

        // Act
        Action act = () =>
        {
            using var roundTripWriter = new RoundTripWriter(inputReader, "test");
            foreach (var c in output)
            {
                roundTripWriter.Write(c);
            };
        };

        // Assert
        var thrownEx = act.Should().ThrowExactly<PersistenceLibraryException>()
            .WithErrorCode(PersistenceErrorCode.RoundTripValidationFailed)
            .Which;
        thrownEx.MsappEntryFullPath.Should().Be("test");
        thrownEx.LineNumber.Should().Be(line);
        thrownEx.Column.Should().Be(column);
    }
}
