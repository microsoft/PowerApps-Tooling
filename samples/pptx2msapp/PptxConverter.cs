// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.PowerPlatform.PowerApps.Persistence.Models;
using Microsoft.PowerPlatform.PowerApps.Persistence.MsApp;
using Microsoft.PowerPlatform.PowerApps.Persistence.Templates;

namespace pptx2msapp;

internal class PptxConverter(IMsappArchiveFactory MsappArchiveFactory, IControlFactory ControlFactory) : IDisposable
{
    private IMsappArchive? _msappArchive;
    private bool _isDisposed;
    private int _slideSizeX;
    private int _slideSizeY;
    private PresentationPart? _presentationPart;

    internal void Convert(string pptxFilePath, string msappFilePath)
    {
        if (string.IsNullOrWhiteSpace(pptxFilePath))
            throw new ArgumentNullException(nameof(pptxFilePath));
        if (string.IsNullOrWhiteSpace(msappFilePath))
            throw new ArgumentNullException(nameof(msappFilePath));

        _msappArchive = MsappArchiveFactory.Create(msappFilePath, overwrite: true);

        _msappArchive.App = ControlFactory.CreateApp();

        // Open the presentation as read-only.
        using (var pptx = PresentationDocument.Open(pptxFilePath, isEditable: false))
        {
            if (pptx.PresentationPart == null || pptx.PresentationPart.Presentation == null)
                throw new InvalidOperationException("The presentation part is missing.");

            _presentationPart = pptx.PresentationPart;

            // Get slide size
            _slideSizeX = pptx.PresentationPart.Presentation.SlideSize!.Cx!.Value;
            _slideSizeY = pptx.PresentationPart.Presentation.SlideSize!.Cy!.Value;

            ConvertSlides(pptx.PresentationPart, pptx.PresentationPart.Presentation);
        }

        _msappArchive.Save();
    }

    private void ConvertSlides(PresentationPart presentationPart, Presentation presentation)
    {
        if (presentation.SlideIdList == null)
            throw new InvalidOperationException("The slide ID list is missing.");

        var slideIdx = 1;
        foreach (var slideId in presentation.SlideIdList.Cast<SlideId>())
        {
            if (slideId == null || slideId.RelationshipId == null || slideId.RelationshipId.Value == null)
                throw new InvalidOperationException("The slide ID is missing.");

            var part = presentationPart.GetPartById(id: slideId.RelationshipId.Value) ?? throw new InvalidOperationException("The slide part is missing.");
            if (part is not SlidePart slidePart || slidePart.Slide == null)
                throw new InvalidOperationException("The slide is missing.");

            var screen = ControlFactory.CreateScreen($"Screen{slideIdx}");
            _msappArchive!.App!.Screens.Add(screen);

            ConvertShapeTree(slidePart, screen, $"Slide{slideIdx}", slidePart.Slide.CommonSlideData);

            slideIdx++;
        }
    }

    private void ConvertShapeTree(SlidePart slidePart, Screen screen, string controlPrefix, CommonSlideData? commonSlideData)
    {
        if (commonSlideData == null || commonSlideData.ShapeTree == null)
            return;

        screen.Children = new List<Microsoft.PowerPlatform.PowerApps.Persistence.Models.Control>();

        var controlIdx = 1;
        foreach (var element in commonSlideData.ShapeTree.Elements().Reverse())
        {
            switch (element)
            {
                case Shape shape:
                    ConvertShape($"{controlPrefix}_{controlIdx}", shape.TextBody, shape.ShapeProperties, screen.Children);
                    break;
                case Picture picture:
                    ConvertPicture(slidePart, $"{controlPrefix}_{controlIdx}", picture);
                    break;
            }
            controlIdx++;
        }
    }

    private void ConvertShape(string controlPrefix, TextBody? textBody, ShapeProperties? shapeProperties, IList<Microsoft.PowerPlatform.PowerApps.Persistence.Models.Control> children)
    {
        if (textBody == null || string.IsNullOrWhiteSpace(textBody.InnerText) || shapeProperties == null)
            return;

        var groupContainer = ControlFactory.Create($"{controlPrefix}_GC1", "GroupContainer", variant: "horizontalAutoLayoutContainer",
            properties: new()
            {
                { "X", $"Parent.Width * {shapeProperties.Transform2D!.Offset!.X} / {_slideSizeX}" },
                { "Y", $"Parent.Height * {shapeProperties.Transform2D!.Offset!.Y} / {_slideSizeY}" },
                { "Width", $"Parent.Width * {shapeProperties.Transform2D!.Extents!.Cx} / {_slideSizeX}" },
                { "Height", $"Parent.Height * {shapeProperties.Transform2D!.Extents!.Cx} / {_slideSizeX}" },
                { "LayoutDirection", $"LayoutDirection.Vertical" },
                { "LayoutMode", $"LayoutMode.Auto" },
            }
        );

        groupContainer.Children = new List<Microsoft.PowerPlatform.PowerApps.Persistence.Models.Control>();
        var controlIdx = 1;
        foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
        {
            var text = new StringBuilder();
            foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
            {
                if (run.Text == null)
                    continue;

                text.Append($"<span style='");
                if (run.RunProperties != null)
                {
                    if (run.RunProperties.Bold != null)
                        text.Append($"font-weight: {(run.RunProperties.Bold.Value ? "bold" : "normal")}; ");
                    if (run.RunProperties.FontSize != null)
                        text.Append($"font-size: {run.RunProperties.FontSize.Value / 100}pt; ");
                }
                text.Append($"'>");

                text.Append(run.Text.Text);
                text.Append($"</span>");
            }

            if (text.Length == 0)
                continue;

            groupContainer.Children.Add(ControlFactory.Create($"{controlPrefix}_HtmlViewer{controlIdx}", "HtmlViewer",
                properties: new()
                {
                    { "HtmlText", $"\"{text}\"" },
                    { "AlignInContainer", $"AlignInContainer.Stretch" },
                    { "FillPortions", $"1" },
                }
            ));
            controlIdx++;
        }

        children.Add(groupContainer);
    }

    private void ConvertPicture(SlidePart slidePart, string controlPrefix, Picture picture)
    {
        if (picture.BlipFill == null || picture.BlipFill.Blip == null || picture.ShapeProperties == null)
            return;

        var embedId = picture.BlipFill.Blip.Embed?.Value;
        var imagePart = GetPartById<ImagePart>(slidePart, embedId);
        var imageStream = imagePart.GetStream();
        var imageResouce = _msappArchive!.AddImage(Path.GetFileName(imagePart.Uri.OriginalString), imageStream);

        var image = ControlFactory.Create($"{controlPrefix}_Image1", "Image",
            properties: new()
            {
            { "X", $"Parent.Width * {picture.ShapeProperties.Transform2D!.Offset!.X} / {_slideSizeX}" },
            { "Y", $"Parent.Height * {picture.ShapeProperties.Transform2D!.Offset!.Y} / {_slideSizeY}" },
            { "Width", $"Parent.Width * {picture.ShapeProperties.Transform2D!.Extents!.Cx} / {_slideSizeX}" },
            { "Height", $"Parent.Height * {picture.ShapeProperties.Transform2D!.Extents!.Cy} / {_slideSizeY}" },
            { "ImagePosition", "ImagePosition.Fill" },
            { "Image", imageResouce },
            }
        );

        _msappArchive!.App!.Screens.Last().Children.Add(image);
    }

    #region Helpers

    private static T GetPartById<T>(OpenXmlPartContainer partContainer, string? partId)
    {
        ArgumentNullException.ThrowIfNull(partContainer);
        if (string.IsNullOrWhiteSpace(partId))
            throw new ArgumentNullException(nameof(partId));

        var part = partContainer.GetPartById(id: partId) ?? throw new InvalidOperationException($"The part {partId} is missing.");
        if (part is not T typedPart)
            throw new InvalidOperationException($"Part {partId} is not of type {typeof(T)}");

        return typedPart;
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                if (_msappArchive != null)
                {
                    _msappArchive.Dispose();
                    _msappArchive = null;
                }
            }

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
