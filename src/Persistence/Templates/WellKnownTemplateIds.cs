// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

/// <summary>
/// A set of constant strings representing templateIds of well-known templates in the Document Server.
/// </summary>
/// <remarks>
/// These often may have custom logic or handling in various parts of the codebase (e.g. attribute parameter values, etc).
/// </remarks>
public static class WellKnownTemplateIds
{
    public const string AppInfo = "http://microsoft.com/appmagic/appinfo";
    public const string Screen = "http://microsoft.com/appmagic/screen";
    public const string Component = "http://microsoft.com/appmagic/Component";
    public const string CommandComponent = "http://microsoft.com/appmagic/CommandComponent";
    public const string DataComponent = "http://microsoft.com/appmagic/DataComponent";
    public const string FunctionComponent = "http://microsoft.com/appmagic/FunctionComponent";
    public const string HostControl = "http://microsoft.com/appmagic/hostcontrol";
    public const string TestSuite = "http://microsoft.com/appmagic/testsuite";
    public const string AppTest = "http://microsoft.com/appmagic/apptest";
    public const string TestCase = "http://microsoft.com/appmagic/testcase";

    public const string Group = "http://microsoft.com/appmagic/group";
    public const string GroupContainer = "http://microsoft.com/appmagic/groupContainer";
    public const string Gallery = "http://microsoft.com/appmagic/gallery";
    public const string GalleryTemplate = "http://microsoft.com/appmagic/galleryTemplate";
}
