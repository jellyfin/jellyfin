### 2.2.2 - *August 3 2016*

  * [Fixed handling of keyframes with overlapping offsets.](https://github.com/web-animations/web-animations-next/pull/470)

  * [Throw TypeError on invalid keyframe input.](https://github.com/web-animations/web-animations-next/pull/471)

  * [Fixed display and other animation properties being animated.](https://github.com/web-animations/web-animations-next/pull/474)

  * [Throw InvalidStateError when calling play() on reversed infinite animation.](https://github.com/web-animations/web-animations-next/pull/475)

  * [Fixed infinite loop in cubic-bezier timing function.](https://github.com/web-animations/web-animations-next/pull/476)

  * [Fixed idle animations not becoming paused when seeked.](https://github.com/web-animations/web-animations-next/pull/479)

  * [Fixed pause() not rewinding idl animations.](https://github.com/web-animations/web-animations-next/pull/480)

  * [Extended cubic-bezier timing function domain from [0, 1] to (-Infinity, Infinity).](https://github.com/web-animations/web-animations-next/pull/481)

  * [Fixed timing model to handle corner cases involving Infinity and 0 correctly.](https://github.com/web-animations/web-animations-next/pull/482)

  * [Fixed source files missing from npm package.](https://github.com/web-animations/web-animations-next/pull/483)

  * [Improved performance of starting and updating individual animations.](https://github.com/web-animations/web-animations-next/pull/485)

### 2.2.1 - *April 28 2016*
  * [Deprecated invalid timing inputs](https://github.com/web-animations/web-animations-next/pull/437) as they will soon throw [TypeErrors](https://github.com/web-animations/web-animations-next/pull/426) in native browsers.

    For example, this is deprecated and will eventually throw a TypeError:

        element.animate([], {
            duration: -1,
            iterationStart: -1,
            iterations: -1,
            easing: 'garbage string',
        });

  * [Fixed polyfill crash in browsers based on Chromium 36 to 46.](https://github.com/web-animations/web-animations-next/pull/434)

  * [Increased cubic-bezier accuracy.](https://github.com/web-animations/web-animations-next/pull/428)

  * [Added support for grad and turn units for angles.](https://github.com/web-animations/web-animations-next/pull/427)

### 2.2.0 - *April 6 2016*
  * Deprecated the use of hyphens in property names.

    For example, this is deprecated:

        element.animate([{'font-size': '0px'}, {'font-size': '10px'}]);

    and this should be used instead:

        element.animate([{fontSize: '0px'}, {fontSize: '10px'}]);

  * Added arbitrary easing capitalisation.

  * Added "id" effect option. (http://w3c.github.io/web-animations/#dom-keyframeanimationoptions-id)

  * Added "oncancel" event handler.

  * Added value list keyframe syntax.

    As as alternative to:

        element.animate([{color: 'red'}, {color: 'green'}, {color: 'blue'}]);

    you can now use:

        element.animate({color: ['red', 'green', 'blue']});

  * Fixed easing TypeError in FireFox Nightly when using groups.

  * Fixed delayed animation updates on Safari and Firefox

  * Fixed infinite recursion when setting onfinish to null.

### 2.1.4 - *December 1 2015*
  * Use `Date.now()` instead of `performace.now()` for mobile Safari.

### 2.1.3 - *October 12 2015*
  * Removed web-animations.min.js.gz

### 2.1.2 - *July 8 2015*
  * Fix a bug where onfinish was being called for GroupEffects before they were finished.

### 2.1.1 - *July 1 2015*
  * Add Animation.timeline getter
  * Add AnimationEffect.parent getter
  * Make AnimationEffectTiming (returned by AnimationEffect.timing) attributes mutable
  * Expose the Animation constructor
  * Change custom effects from AnimationEffects to onsample functions. Custom effects should now be created by setting the onsample attribute of a KeyframeEffect.

    For example, this is deprecated:

        var myEffect = new KeyframeEffect(
           element,
           function(timeFraction, target, effect) {
              target.style.opacity = timeFraction;
           },
           1000);
        var myAnimation = document.timeline.play(myEffect);

    and this should be used insead:

        var myEffect = new KeyframeEffect(element, [], 1000);
        effect.onsample = function(timeFraction, effect, animation) {
           effect.target.style.opacity = timeFraction;
        };
        var myAnimation = document.timeline.play(myEffect);

### 2.1.0 - *June 15 2015*
  * Fix bug affecting GroupEffects with infinite iteration children
  * Add GroupEffect.firstChild and GroupEffect.lastChild
  * Add initial values for most CSS properties
  * Allow `timeline.play()` to be called with no arguments
  * Add AnimationEffect.clone
  * Add GroupEffect.append and GroupEffect.prepend
  * Add AnimationEffect.remove
  * Add Animation.ready and Animation.finished promises

### 2.0.0 - *April 5 2015*

  * Improve behavior of group Animation playback rate.
  * Rename Animation to KeyframeEffect.
  * Rename AnimationSequence to SequenceEffect.
  * Rename AnimationGroup to GroupEffect.
  * Rename AnimationPlayer to Animation.
  * Remove KeyframeEffect.effect and add KeyframeEffect.getFrames.
  * Rename Animation.source to Animation.effect.
  * Rename Timeline.getAnimationPlayers to Timeline.getAnimations.
  * Rename Element.getAnimationPlayers to Element.getAnimations.

### 1.0.7 - *March 10 2015*

  * Improve performance of constructing groups and sequences.
  * Remove support for animating zoom.
  * Add bower file.

### 1.0.6 - *February 5 2015*

  * Implement playbackRate setter for group players.
  * Fix pausing a group player before its first tick.
  * Fix cancelling a group player before its first tick.
  * Fix excess CPU use on idle pages where custom effects and groups were used.
  * Suppress AnimationTiming.playbackRate deprecation warning for cases where AnimationTiming.playbackRate == 1.

### 1.0.5 - *January 6 2015*

  * Fix loading the polyfill in an SVG document
  * Fix a problem where groups didn't take effect in their first frame
  * Don't rely on performance.now

### 1.0.4 - *December 8 2014*

  * Fix a critical bug where deprecation logic wasn't being loaded
    when `web-animations-next` and `web-animations-next-lite` were
    executed on top of a native `element.animate`.

### 1.0.3 - *December 4 2014*

  * Fix a critical bug on iOS 7 and Safari <= 6. Due to limitations,
    inline style patching is not supported on these platforms.

### 1.0.2 - *November 28 2014*

  * Deprecated `AnimationTiming.playbackRate`.

    For example, this is no longer supported:

        var player = element.animate(
            keyframes,
            {duration: 1000, playbackRate: 2});

    Use `AnimationPlayer.playbackRate` instead:

        var player = element.animate(
            keyframes,
            {duration: 1000});
        player.playbackRate = 2;

    If you have any feedback on this change, please start a discussion
    on the public-fx mailing list:
    http://lists.w3.org/Archives/Public/public-fx/

    Or file an issue against the specification on GitHub:
    https://github.com/w3c/web-animations/issues/new

### 1.0.1 - *November 26 2014*

  * Players should be constructed in idle state
  * `play()` and `reverse()` should not force a start times
  * Add `requestAnimationFrame` ids and `cancelAnimationFrame`

### 1.0.0 — *November 21 2014*

  The web-animations-js hackers are pleased to announce the release of
  a new codebase for the Web Animations Polyfill:
  https://github.com/web-animations/web-animations-js

  The previous polyfill has been moved to:
  https://github.com/web-animations/web-animations-js-legacy

  The new codebase is focused on code-size -- our smallest target is
  now only 33kb or 11kb after gzip.

  We've implemented native fallback. If the target browser provides
  Web Animations features natively, the Polyfill will use them.

  We now provide three different build targets:

  `web-animations.min.js` - Tracks the Web Animations features that
  are supported natively in browsers. Today that means Element.animate
  and Playback Control in Chrome. If you’re not sure what features you
  will need, start with this.

  `web-animations-next.min.js` - All of web-animations.min.js plus
  features that are still undergoing discussion or have yet to be
  implemented natively.

  `web-animations-next-lite.min.js` - A cut down version of
  web-animations-next, removes several lesser used property handlers
  and some of the larger and less used features such as matrix
  interpolation/decomposition.

  Not all features of the previous polyfill have been ported to the
  new codebase; most notably mutation of Animations and Groups and
  Additive Animations are not yet supported. These features are still
  important and will be implemented in the coming weeks.
