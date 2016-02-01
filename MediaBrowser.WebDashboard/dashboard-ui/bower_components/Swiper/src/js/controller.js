/*=========================
  Controller
  ===========================*/
s.controller = {
    LinearSpline: function (x, y) {
        this.x = x;
        this.y = y;
        this.lastIndex = x.length - 1;
        // Given an x value (x2), return the expected y2 value:
        // (x1,y1) is the known point before given value,
        // (x3,y3) is the known point after given value.
        var i1, i3;
        var l = this.x.length;

        this.interpolate = function (x2) {
            if (!x2) return 0;

            // Get the indexes of x1 and x3 (the array indexes before and after given x2):
            i3 = binarySearch(this.x, x2);
            i1 = i3 - 1;

            // We have our indexes i1 & i3, so we can calculate already:
            // y2 := ((x2−x1) × (y3−y1)) ÷ (x3−x1) + y1
            return ((x2 - this.x[i1]) * (this.y[i3] - this.y[i1])) / (this.x[i3] - this.x[i1]) + this.y[i1];
        };

        var binarySearch = (function() {
            var maxIndex, minIndex, guess;
            return function(array, val) {
                minIndex = -1;
                maxIndex = array.length;
                while (maxIndex - minIndex > 1)
                    if (array[guess = maxIndex + minIndex >> 1] <= val) {
                        minIndex = guess;
                    } else {
                        maxIndex = guess;
                    }
                return maxIndex;
            };
        })();
    },
    //xxx: for now i will just save one spline function to to
    getInterpolateFunction: function(c){
        if(!s.controller.spline) s.controller.spline = s.params.loop ?
            new s.controller.LinearSpline(s.slidesGrid, c.slidesGrid) :
            new s.controller.LinearSpline(s.snapGrid, c.snapGrid);
    },
    setTranslate: function (translate, byController) {
       var controlled = s.params.control;
       var multiplier, controlledTranslate;
       function setControlledTranslate(c) {
            // this will create an Interpolate function based on the snapGrids
            // x is the Grid of the scrolled scroller and y will be the controlled scroller
            // it makes sense to create this only once and recall it for the interpolation
            // the function does a lot of value caching for performance
            translate = c.rtl && c.params.direction === 'horizontal' ? -s.translate : s.translate;
            if (s.params.controlBy === 'slide') {
                s.controller.getInterpolateFunction(c);
                // i am not sure why the values have to be multiplicated this way, tried to invert the snapGrid
                // but it did not work out
                controlledTranslate = -s.controller.spline.interpolate(-translate);
            }

            if(!controlledTranslate || s.params.controlBy === 'container'){
                multiplier = (c.maxTranslate() - c.minTranslate()) / (s.maxTranslate() - s.minTranslate());
                controlledTranslate = (translate - s.minTranslate()) * multiplier + c.minTranslate();
            }

            if (s.params.controlInverse) {
                controlledTranslate = c.maxTranslate() - controlledTranslate;
            }
            c.updateProgress(controlledTranslate);
            c.setWrapperTranslate(controlledTranslate, false, s);
            c.updateActiveIndex();
       }
       if (s.isArray(controlled)) {
           for (var i = 0; i < controlled.length; i++) {
               if (controlled[i] !== byController && controlled[i] instanceof Swiper) {
                   setControlledTranslate(controlled[i]);
               }
           }
       }
       else if (controlled instanceof Swiper && byController !== controlled) {

           setControlledTranslate(controlled);
       }
    },
    setTransition: function (duration, byController) {
        var controlled = s.params.control;
        var i;
        function setControlledTransition(c) {
            c.setWrapperTransition(duration, s);
            if (duration !== 0) {
                c.onTransitionStart();
                c.wrapper.transitionEnd(function(){
                    if (!controlled) return;
                    if (c.params.loop && s.params.controlBy === 'slide') {
                        c.fixLoop();
                    }
                    c.onTransitionEnd();

                });
            }
        }
        if (s.isArray(controlled)) {
            for (i = 0; i < controlled.length; i++) {
                if (controlled[i] !== byController && controlled[i] instanceof Swiper) {
                    setControlledTransition(controlled[i]);
                }
            }
        }
        else if (controlled instanceof Swiper && byController !== controlled) {
            setControlledTransition(controlled);
        }
    }
};