using System.Reflection;
using PdfSharp.Fonts;

namespace HelpDesk.Api.Infrastructure.Reports;

/// <summary>Configures the embedded, platform-independent font used by PDF reports.</summary>
public static class ReportPdfFontConfiguration
{
    private static readonly object Sync = new();
    private static readonly EmbeddedReportFontResolver Resolver = new();
    private static bool configured;

    /// <summary>Gets the font-family name understood by the report font resolver.</summary>
    public const string FamilyName = "HelpDesk DejaVu Sans";

    /// <summary>Configures PDFsharp once, before any report font is constructed.</summary>
    public static void Configure()
    {
        lock (Sync)
        {
            if (configured)
                return;

            GlobalFontSettings.FontResolver = Resolver;
            configured = true;
        }
    }

    private sealed class EmbeddedReportFontResolver : IFontResolver
    {
        private const string RegularFace = "HelpDesk.DejaVuSans.Regular";
        private const string BoldFace = "HelpDesk.DejaVuSans.Bold";
        private const string RegularResource = "HelpDesk.Api.Assets.Fonts.DejaVuSans.ttf";
        private const string BoldResource = "HelpDesk.Api.Assets.Fonts.DejaVuSans-Bold.ttf";
        private static readonly Assembly Assembly = typeof(ReportPdfFontConfiguration).Assembly;

        public FontResolverInfo? ResolveTypeface(
            string familyName,
            bool isBold,
            bool isItalic)
        {
            if (!string.Equals(familyName, FamilyName, StringComparison.OrdinalIgnoreCase))
                return null;

            return new FontResolverInfo(
                isBold ? BoldFace : RegularFace,
                mustSimulateBold: false,
                mustSimulateItalic: isItalic);
        }

        public byte[]? GetFont(string faceName) => faceName switch
        {
            RegularFace => ReadResource(RegularResource),
            BoldFace => ReadResource(BoldResource),
            _ => null
        };

        private static byte[] ReadResource(string resourceName)
        {
            using var stream = Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"Embedded PDF font resource '{resourceName}' is unavailable.");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
