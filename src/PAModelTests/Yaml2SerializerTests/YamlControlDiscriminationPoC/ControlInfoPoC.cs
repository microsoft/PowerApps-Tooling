using FluentAssertions;
using Microsoft.PowerPlatform.Formulas.Tools.Yaml2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PAModelTests.Yaml2SerializerTests.YamlControlDiscriminationPoC;

public class Screen
{
    public string Name { get; set; }
    public IList<ControlInfoPoC> Controls { get; set; }
}

public abstract class ControlInfoPoC
{
    protected ControlInfoPoC(string controlType)
    {
        ControlType = controlType;
    }

    // Used by deserializer to determine which Type to deserialize as, when in a collection of the base type
    [YamlMember(Order = -1)]
    public string ControlType { get; protected set; }

    public Position Position { get; set; }
    public Size Size { get; set; }
    public string ToolTip { get; set; }
}

public class Position
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class Size
{
    public int Height { get; set; }
    public int Width { get; set; }
}

public class Button : ControlInfoPoC
{
    internal const string ControlVersion = "button@v1";
    public Button()
        : base(ControlVersion)
    {
    }

    public string ButtonText { get; set; }
    public string ClickAction { get; set; }
}

public class ButtonV2 : ControlInfoPoC
{
    internal const string ControlVersion = "button@v2";
    public ButtonV2()
        : base(ControlVersion)
    {
    }

    // Simulated breaking change - Button v2 renamed "ButtonText" to "Label"
    public string Label { get; set; }
    public string ClickAction { get; set; }
}

public class TextLabel : ControlInfoPoC
{
    internal const string ControlVersion = "textlabel@v1";
    public TextLabel()
        : base(ControlVersion)
    {
    }

    public string Label { get; set; }
}

public class TextInput : ControlInfoPoC
{
    internal const string ControlVersion = "textinput@v1";
    public TextInput()
        : base(ControlVersion)
    {
    }

    public string Watermark { get; set; }
}

[TestClass]
public class ControlDiscriminationTests
{
    [TestMethod]
    public void ControlDiscriminationPoC()
    {
        var serializer = new YamlPocoConverter().Serializer;

        // Note that this deserializer is NOT the version in YamlPocoConverter, as we are adding
        // a type discriminator here that is not yet present there as a proof of concept.
        var deserializer = new DeserializerBuilder()
            .WithDuplicateKeyChecking()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithTypeDiscriminatingNodeDeserializer(options =>
                {
                    var typeMapping = new Dictionary<string, Type>
                    {
                        { Button.ControlVersion, typeof(Button) },
                        { ButtonV2.ControlVersion, typeof(ButtonV2) },
                        { TextInput.ControlVersion, typeof(TextInput) },
                        { TextLabel.ControlVersion, typeof(TextLabel) }
                    };
                    options.AddKeyValueTypeDiscriminator<ControlInfoPoC>("controlType", typeMapping);
                })
            .Build();

        var screen = new Screen
        {
            Name = "Screen 1",
            Controls = new List<ControlInfoPoC>
            {
                new TextLabel { Label = "Input the thing:" },
                new TextInput { Watermark = "example thing" },
                new Button { ButtonText = "OK", ClickAction = "submit" },
                new ButtonV2 { Label = "Cancel", ClickAction = "cancel" }
            }
        };

        var yaml = serializer.Serialize(screen);
        var deserialized = deserializer.Deserialize<Screen>(yaml);

        // Confirm that the deserialization picked the right types
        deserialized.Controls.Should().HaveCount(4);
        deserialized.Controls[0].Should().BeOfType<TextLabel>();
        deserialized.Controls[1].Should().BeOfType<TextInput>();
        deserialized.Controls[2].Should().BeOfType<Button>();
        deserialized.Controls[3].Should().BeOfType<ButtonV2>();
    }
}
