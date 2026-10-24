namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Decision produced by <see cref="ScaleBranching.DecideSwScale"/>: which SW scale branch
/// applies plus the encoder traits that branch needs (mod-N width alignment, aspect-ratio
/// expression form). Filter-string builder and dim resolver both consume this so the
/// decision is made in one place.
/// </summary>
/// <param name="Branch">The SW scale branch that fired.</param>
/// <param name="ScaleVal">Width alignment multiple — 64 for v4l2m2m, 2 otherwise.</param>
/// <param name="TargetArExpression">
/// ffmpeg expression for aspect ratio: <c>"(a*sar)"</c> for mjpeg (which can't carry SAR in
/// its output, so the filter has to produce display-correct dims), <c>"a"</c> otherwise.
/// </param>
/// <param name="IsMjpeg">
/// Whether the encoder is mjpeg; used by the resolver to compute the numeric form of
/// <see cref="TargetArExpression"/> as <c>a * sar</c> versus just <c>a</c>.
/// </param>
public readonly record struct SwScaleDecision(
    SwScaleBranch Branch,
    int ScaleVal,
    string TargetArExpression,
    bool IsMjpeg);
