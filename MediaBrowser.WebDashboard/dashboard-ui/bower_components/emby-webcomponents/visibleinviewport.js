define([], function () {

    /**
   * Copyright 2012, Digital Fusion
   * Licensed under the MIT license.
   * http://teamdf.com/jquery-plugins/license/
   *
   * @author Sam Sehnert
   * @desc A small plugin that checks whether elements are within
   *       the user visible viewport of a web browser.
   *       only accounts for vertical position, not horizontal.
   */
    function visibleInViewport(elem, partial, thresholdX, thresholdY, windowSize) {

        thresholdX = thresholdX || 0;
        thresholdY = thresholdY || 0;

        if (!elem.getBoundingClientRect) {
            return true;
        }

        windowSize = windowSize || {
            innerHeight: window.innerHeight,
            innerWidth: window.innerWidth
        };

        var vpWidth = windowSize.innerWidth,
            vpHeight = windowSize.innerHeight;

        // Use this native browser method, if available.
        var rec = elem.getBoundingClientRect(),
            tViz = rec.top >= 0 && rec.top < vpHeight + thresholdY,
            bViz = rec.bottom > 0 && rec.bottom <= vpHeight + thresholdY,
            lViz = rec.left >= 0 && rec.left < vpWidth + thresholdX,
            rViz = rec.right > 0 && rec.right <= vpWidth + thresholdX,
            vVisible = partial ? tViz || bViz : tViz && bViz,
            hVisible = partial ? lViz || rViz : lViz && rViz;

        return vVisible && hVisible;
    }

    return visibleInViewport;
});