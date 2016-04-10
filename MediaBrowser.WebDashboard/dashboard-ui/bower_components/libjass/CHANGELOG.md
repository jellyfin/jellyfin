### v0.11.0 - 2016/01/24
- BREAKING CHANGE - WebRenderer.resize(width, height) used to have a broken implementation of letterboxing to move the subs div right or down. Now it's WebRenderer.resize(width, height, left, top) and expects the caller to calculate letterboxing itself and supply left and top accordingly. DefaultRenderer does it using the video resolution and users of WebRenderer can do the same.
- BREAKING CHANGE - DefaultRenderer.resize() now ignores its parameters and always resizes to its video element's dimensions. It had already stopped resizing the video element when it was renamed from resizeVideo in v0.6.0, so it doesn't make sense to let it take a completely different width and height.
- BREAKING CHANGE - Removed fullscreen support in DefaultRenderer. It started out as a hack using max z-index and works on even fewer browsers now. It probably didn't work for you anyway so it should be no big loss.
- Implemented experimental support for \t
- Added RendererSettings.fallbackFonts to set the fallback fonts for all styles. Defaults to 'Arial, Helvetica, sans-serif, "Segoe UI Symbol"'.
- Better compatibility with loose ASS scripts - assume unnamed first section is Script Info, fall back to Default style for missing styles, recognize arbitrary-case property names, normalize asterisks in style names, etc.
- Various font size improvements - faster calculation, fix for incorrect size when line-height is overridden by site CSS, fix for incorrect scaled sizes for letterboxed subs, fix for incorrect metrics for web fonts, etc. The last one requires that all web fonts be specified in RendererSettings.fontMap to be rendered accurately.
- WebRenderer now supports using local() URLs in addition to url() in CSS font-face rules.
- Added RendererSettings.useAttachedFonts. If true, TTF fonts attached to the script will be used in addition to fonts specified in RendererSettings.fontMap. This setting is false by default, and should only be enabled on trusted fonts since it uses a very naive base64 and TTF parser to extract the font names from the attachment. It also requires ES6 typed arrays - ArrayBuffer, DataView, Uint8Array, etc. in the environment.
- Various pre-render, SVG filter and DOM perf improvements.
- Fixed \fscx and \fscy to not scale shadows.
- Fixed \fscx and \fscy to have optional values.
- Fixed \fs+ and \fs- to have required values.
- Fixed \r<target_style> to use the target style's alpha values instead of 1.
- Fixed \fad subs to not flash after the fade-out ends with low-resolution clocks.
- Fixed outlines to not be darker than they should be.
- Fixed styles to not ignore the ScaleX and ScaleY properties in the script.
- Fixed lack of sufficient space between normal and italic text.
- Fixed SVG filters to interpolate in sRGB space instead of RGB.
- Fixed ASS parser to complain if a script doesn't have a Script Info section at all.
- The promise returned from ASS.from*() is now properly rejected due to errors from loading the script, instead of just remaining unresolved forever.
- Fixed SRT parser to swallow UTF-8 BOM just like the ASS parser.
- Fixed all clocks to suppress redundant ticks if the current timestamp hasn't change from the last tick.
- Fixed {AutoClock, VideoClock}.{setEnabled, toggle} methods to actually enable / disable the high-resolution timer.


### v0.10.0 - 2015/05/05
- Implemented libjass.renderers.AutoClock, a clock that automatically ticks and generates clock events according to the state of an external driver.
- Implemented \k
- libjass.{Set, Map, Promise} can now be set to null to force the use of the polyfills, even if it defaulted to a runtime-provided implementation.
- Added ASS.fromReadableStream(), a function that can be used to parse ASS from a readable stream such as the response of window.fetch().
- ASS.fromUrl() now tries to use window.fetch() if available instead of XMLHttpRequest.
- Fixed constant pausing and playing on Firefox triggered by how slowly it updates video.currentTime (wasn't noticeable but still undesirable).
- Fixed a dialogue's animation state not getting updated while seeking if the start and end times of the seek were within its start and end times.
- Fixed wrapping mode 1 (end-of-line wrapping) to actually wrap.
- Fixed parser to parse the time components of karaoke tags as centiseconds instead of seconds.
- Fixed parser to swallow leading BOM, if any.
- Fixed errors reported by webworker API were empty objects without message and stack properties.


### v0.9.0 - 2014/11/27
- BREAKING CHANGE - ASS.fromString() now returns a Promise of an ASS object, not an ASS object directly. The synchronous ASS parser used by ASS.fromString() is no more.
- BACKWARD-COMPATIBLE CHANGE - WebRenderer constructor parameters order has changed from (ass, clock, settings, libjassSubsWrapper) to (ass, clock, libjassSubsWrapper, settings). The constructor will detect the old order and reorder accordingly.
- Added ASS.fromStream and ASS.fromXhr that read a stream and an XMLHttpRequest object's response respectively and return (a Promise of) an ASS object. Both of these parse the script asynchronously.
- Added RendererSettings.enableSvg that can be used to toggle the use of SVG filter effects for outlines and blur.
- libjass.js now has an AMD wrapper so that it can be used with RequireJS, etc.
- Settings parameter is now optional for WebRenderer and DefaultRenderer.
- Added support for clock rates apart from 1 to clocks and renderers.
- Added a parameter to libjass.createWorker to specify the path to libjass.js that will run in the worker.
- Fixed Style and Dialogue constructors not setting defaults for missing properties.
- Fixed color and alpha parser to support more formats.
- Fixed SRT parser to replace all HTML tags it finds, instead of just the first one.
- Fixed font size calculation to use the vertical scale instead of horizontal.
- Fixed line-height on newlines.
- Fixed missing perspective on X and Y rotations.


### v0.8.0 - 2014/08/16
- Added web worker support. libjass.parse can now be offloaded to a web worker.
- Implemented \fs+ and \fs-
- Added ASS.addEvent() to add dialogue lines to an ASS object.
- Renamed ClockEvent.TimeUpdate to ClockEvent.Tick, and added ClockEvent.Stop
- Clock.enable() and .disable() now return a boolean to indicate whether the function had any effect.
- Added Clock.setEnabled() to force the enabled-state to the given value.
- Renamed ManualClock.timeUpdate() to ManualClock.tick()
- Moved WebRenderer.enable(), .disable() and .enabled to NullRenderer
- Fixed not being able to parse tags with default values.
- Fixed font preloader downloading the same font multiple times because it didn't filter for duplicates.
- Fixed min-width value not taking separate left and right margins into account.
- Fixed absolutely positioned subs were always left-aligned even if they had an alignment tag.
- Fixed blur and outlines getting truncated.


### v0.7.0 - 2014/05/15
- Implemented \be
- Split a new renderer, WebRenderer, off DefaultRenderer that doesn't rely on a video element.
- All renderers now require a Clock to generate time events. VideoClock is a Clock backed by a video element, while ManualClock is a clock that can be used to generate arbitrary time events.


### v0.6.0 - 2014/03/24
- All script properties and style properties are now parsed and stored in the ASS and Style objects.
- Basic SRT support, by passing in a libjass.Format argument to ASS.fromString()
- \clip and \iclip now have their drawing instructions parsed as an array of libjass.parts.drawing.Instruction's instead of just a string.
- Added DefaultRenderer.enable(), DefaultRenderer.disable() and DefaultRenderer.toggle() to change whether the renderer is displaying subtitles or not.
- DefaultRenderer.resizeVideo is now called DefaultRenderer.resize. Now it only resizes the subtitle wrapper div, not the video element.
- Replaced the 41ms setInterval-bsed timer with a requestAnimationFrame-based timer to reduce load on minimized or hidden browser tabs.
- DefaultRenderer now renders dialogues in the correct order according to the script.
- Fixed incorrect font sizes.
- Replaced jake with gulp.


### v0.5.0 - 2014/01/26
- Removed preLoadFonts renderer setting. It was redundant with the actual fontMap setting since the presence or absence of that setting is enough to signal whether the user wants to preload fonts or not.
- Multiple renderers can now be used on the same page without conflicting with each other.
- Implemented \shad, \xshad, \yshad
- Fixed ASS draw scale being used incorrectly.
- ASS.resolutionX and ASS.resolutionY are now properties of ASS.properties, a ScriptProperties object.


### v0.4.0 - 2013/12/27
- All parts moved from the libjass.tags namespace to the libjass.parts namespace.
- Replaced PEG.js parser with a hand-written one. This allows for parsing lines that are strictly invalid grammar but are parsed successfully by VSFilter or libass.
- All ASS tags are now supported by the parser.
- Removed the useHighResolutionTimer setting for DefaultRenderer. DefaultRenderer always uses the 41ms timer now.
- Implemented \move
- Implemented ASS draw
- Fixed subs overflowing the video dimensions still being visible.
- SVG filters are now used for outlines and blur.
- Delay parsing of dialogue lines till they need to be pre-rendered. As a side-effect, all fonts in the font map are preloaded now, not just the ones used in the current script.


### v0.3.0 - 2013/10/28
- Moved libjass.DefaultRenderer to libjass.renderers.DefaultRenderer
- Added libjass.renderers.NullRenderer, a renderer that doesn't render anything.
- DefaultRenderer's fontMap setting is now a Map instead of an Object. It now supports more than one URL for each font name.
- DefaultRenderer now generates the subtitle wrapper div itself.
- DefaultRenderer now takes video letterboxing into account when resizing the subtitles.
- DefaultRenderer has a new setting useHighResolutionTimer that makes it use a 41ms timer instead of video.timeUpdate's 250ms timer.
- div IDs and CSS class names are now prefixed with "libjass-" to avoid collisions with other elements on the page.
- All numeric CSS property values are now truncated to three decimal places.
- Added ```jake watch``` that rebuilds and runs tests on changes to the source.
- Added ```jake doc``` that builds API documentation.
- Added Travis CI build.


### v0.2.0 - 2013/09/11
- Added libjass.DefaultRenderer, a class that handles initializing the layer div's, preloading fonts, and drawing Dialogues based on the current video time.
- libjass.js can now be loaded in node. Only the parser can be used.
- Tests can now be run with ```jake test``` or ```npm test``` using Mocha.

### v0.1.0 - 2013/08/29
- First npm release.
