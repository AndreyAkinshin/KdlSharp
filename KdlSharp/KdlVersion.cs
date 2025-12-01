namespace KdlSharp;

/// <summary>
/// KDL version enumeration.
/// </summary>
/// <remarks>
/// The default value (V2 = 0) ensures that uninitialized settings use the current specification version.
/// V1 is for legacy document support only.
/// </remarks>
public enum KdlVersion
{
    /// <summary>KDL version 2.0 (current specification, default).</summary>
    V2 = 0,

    /// <summary>KDL version 1.0 (legacy support).</summary>
    V1 = 1,

    /// <summary>Auto-detect version (try v2, then fall back to v1). Only for migration scenarios.</summary>
    Auto = 2
}

