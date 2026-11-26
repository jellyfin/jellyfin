namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Which branch of <see cref="EncodingHelper.GetSwScaleFilter"/> fires for a given request.
/// The filter-string builder and the dim resolver switch on the same enum so their
/// branching logic can't drift — adding a branch on one side without the other triggers a
/// non-exhaustive switch warning at compile time.
/// </summary>
public enum SwScaleBranch
{
    /// <summary>No sizing requested — scale filter is a no-op (empty string).</summary>
    None,

    /// <summary>Both Width and Height explicit; v4l2m2m alignment (mod-64 width).</summary>
    FixedWHv4l2,

    /// <summary>Both Width and Height explicit; non-v4l2 (delegates to <c>GetFixedSwScaleFilter</c>).</summary>
    FixedWH,

    /// <summary>Both MaxWidth and MaxHeight explicit; fit-in-box with source DAR preserved.</summary>
    MaxWMaxH,

    /// <summary>Width only, no 3D source — height derived from source aspect.</summary>
    FixedW,

    /// <summary>Width only, source has a 3D format — delegates to <c>GetFixedSwScaleFilter</c>'s 3D chains.</summary>
    FixedW3D,

    /// <summary>Height only — width derived from source aspect.</summary>
    FixedH,

    /// <summary>MaxWidth only — fit-in-box capped by width.</summary>
    MaxW,

    /// <summary>MaxHeight only — fit-in-box capped by height.</summary>
    MaxH,
}
