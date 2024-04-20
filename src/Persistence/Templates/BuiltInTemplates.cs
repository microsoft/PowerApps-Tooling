// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

public class BuiltInTemplates
{
    public static readonly (string Name, string Id) App = ("Appinfo", "http://microsoft.com/appmagic/appinfo");
    public static readonly (string Name, string Id) Host = ("HostControl", "http://microsoft.com/appmagic/hostcontrol");
    public static readonly (string Name, string Id) Screen = ("Screen", "http://microsoft.com/appmagic/screen");

    public static readonly (string Name, string Id) AppTest = ("AppTest", "http://microsoft.com/appmagic/apptest");
    public static readonly (string Name, string Id) TestSuite = ("AppTest", "http://microsoft.com/appmagic/testsuite");
    public static readonly (string Name, string Id) TestCase = ("AppTest", "http://microsoft.com/appmagic/testcase");

    public static readonly (string Name, string Id) Component = ("Component", "http://microsoft.com/appmagic/Component");
    public static readonly (string Name, string Id) FunctionComponent = ("FunctionComponent", "http://microsoft.com/appmagic/FunctionComponent");
    public static readonly (string Name, string Id) DataComponent = ("DataComponent", "http://microsoft.com/appmagic/DataComponent");
    public static readonly (string Name, string Id) CommandComponent = ("CommandComponent", "http://microsoft.com/appmagic/CommandComponent");

    // Group is the legacy group container template
    public static readonly (string Name, string Id) Group = ("Group", "http://microsoft.com/appmagic/group");

    // Group Container is the newer layout container template
    public static readonly (string Name, string Id) GroupContainer = ("GroupContainer", "http://microsoft.com/appmagic/groupContainer");
}
