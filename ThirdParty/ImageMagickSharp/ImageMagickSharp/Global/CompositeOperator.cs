using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageMagickSharp
{
    public enum CompositeOperator : int
    {
		UndefinedCompositeOp,
		NoCompositeOp,
		ModulusAddCompositeOp,
		AtopCompositeOp,
		BlendCompositeOp,
		BumpmapCompositeOp,
		ChangeMaskCompositeOp,
		ClearCompositeOp,
		ColorBurnCompositeOp,
		ColorDodgeCompositeOp,
		ColorizeCompositeOp,
		CopyBlackCompositeOp,
		CopyBlueCompositeOp,
		CopyCompositeOp,
		CopyCyanCompositeOp,
		CopyGreenCompositeOp,
		CopyMagentaCompositeOp,
		CopyOpacityCompositeOp,
		CopyRedCompositeOp,
		CopyYellowCompositeOp,
		DarkenCompositeOp,
		DstAtopCompositeOp,
		DstCompositeOp,
		DstInCompositeOp,
		DstOutCompositeOp,
		DstOverCompositeOp,
		DifferenceCompositeOp,
		DisplaceCompositeOp,
		DissolveCompositeOp,
		ExclusionCompositeOp,
		HardLightCompositeOp,
		HueCompositeOp,
		InCompositeOp,
		LightenCompositeOp,
		LinearLightCompositeOp,
		LuminizeCompositeOp,
		MinusDstCompositeOp,
		ModulateCompositeOp,
		MultiplyCompositeOp,
		OutCompositeOp,
		OverCompositeOp,
		OverlayCompositeOp,
		PlusCompositeOp,
		ReplaceCompositeOp,
		SaturateCompositeOp,
		ScreenCompositeOp,
		SoftLightCompositeOp,
		SrcAtopCompositeOp,
		SrcCompositeOp,
		SrcInCompositeOp,
		SrcOutCompositeOp,
		SrcOverCompositeOp,
		ModulusSubtractCompositeOp,
		ThresholdCompositeOp,
		XorCompositeOp,
		/* These are new operators, added after the above was last sorted.
		 * The list should be re-sorted only when a new library version is
		 * created.
		 */
		DivideDstCompositeOp,
		DistortCompositeOp,
		BlurCompositeOp,
		PegtopLightCompositeOp,
		VividLightCompositeOp,
		PinLightCompositeOp,
		LinearDodgeCompositeOp,
		LinearBurnCompositeOp,
		MathematicsCompositeOp,
		DivideSrcCompositeOp,
		MinusSrcCompositeOp,
		DarkenIntensityCompositeOp,
		LightenIntensityCompositeOp
    }
}
