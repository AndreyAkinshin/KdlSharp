namespace KdlSharp.Settings;

/// <summary>
/// Configuration settings for KDL serialization.
/// </summary>
public sealed class KdlFormatterSettings
{
    /// <summary>
    /// Gets or sets the indentation string (default: 4 spaces).
    /// </summary>
    public string Indentation { get; set; } = "    ";

    /// <summary>
    /// Gets or sets whether to use compact formatting (no extra whitespace).
    /// </summary>
    public bool Compact { get; set; } = false;

    /// <summary>
    /// Gets or sets the newline string (default: LF "\n").
    /// </summary>
    public string Newline { get; set; } = "\n";

    /// <summary>
    /// Gets or sets whether to prefer identifier strings over quoted strings when possible.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, strings that meet identifier rules will be written
    /// without quotes. Invalid identifiers and reserved keywords automatically fall
    /// back to quoted format.
    /// </remarks>
    public bool PreferIdentifierStrings { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the KDL version marker.
    /// </summary>
    public bool IncludeVersionMarker { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to preserve original number formats.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, numbers are serialized in their original format (hex, octal, binary).
    /// When <c>false</c>, all numbers are serialized as decimal.
    /// </remarks>
    public bool PreserveNumberFormats { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to preserve original string types.
    /// </summary>
    public bool PreserveStringTypes { get; set; } = false;

    /// <summary>
    /// Gets or sets the target KDL version for output.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="KdlVersion.V1"/>, boolean values are written as <c>true</c>/<c>false</c>
    /// and null as <c>null</c>. When set to <see cref="KdlVersion.V2"/> (the default),
    /// boolean values are written as <c>#true</c>/<c>#false</c> and null as <c>#null</c>.
    /// </remarks>
    public KdlVersion TargetVersion { get; set; } = KdlVersion.V2;

    /// <summary>
    /// Creates a copy of these settings.
    /// </summary>
    public KdlFormatterSettings Clone()
    {
        return new KdlFormatterSettings
        {
            Indentation = Indentation,
            Compact = Compact,
            Newline = Newline,
            PreferIdentifierStrings = PreferIdentifierStrings,
            IncludeVersionMarker = IncludeVersionMarker,
            PreserveNumberFormats = PreserveNumberFormats,
            PreserveStringTypes = PreserveStringTypes,
            TargetVersion = TargetVersion
        };
    }
}

