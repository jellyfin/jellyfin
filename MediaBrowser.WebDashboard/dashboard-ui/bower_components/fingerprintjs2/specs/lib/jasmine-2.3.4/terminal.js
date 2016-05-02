(function(global) {
    var UNDEFINED,
        exportObject;

    if (typeof module !== "undefined" && module.exports) {
        exportObject = exports;
    } else {
        exportObject = global.jasmineReporters = global.jasmineReporters || {};
    }

    function elapsed(start, end) { return (end - start)/1000; }
    function isFailed(obj) { return obj.status === "failed"; }
    function isSkipped(obj) { return obj.status === "pending"; }
    function isDisabled(obj) { return obj.status === "disabled"; }
    function extend(dupe, obj) { // performs a shallow copy of all props of `obj` onto `dupe`
        for (var prop in obj) {
            if (obj.hasOwnProperty(prop)) {
                dupe[prop] = obj[prop];
            }
        }
        return dupe;
    }
    function log(str) {
        var con = global.console || console;
        if (con && con.log && str && str.length) {
            con.log(str);
        }
    }


    /**
     * Basic reporter that outputs spec results to the terminal.
     * Use this reporter in your build pipeline.
     *
     * Usage:
     *
     * jasmine.getEnv().addReporter(new jasmineReporters.TerminalReporter(options);
     *
     * @param {object} [options]
     * @param {number} [options.verbosity] meaningful values are 0 through 3; anything
     *   greater than 3 is treated as 3 (default: 2)
     * @param {boolean} [options.color] print in color or not (default: true)
     * @param {boolean} [opts.showStack] show stack trace for failed specs (default: false)
     */
    var DEFAULT_VERBOSITY = 2,
        ATTRIBUTES_TO_ANSI = {
            "off": 0,
            "bold": 1,
            "red": 31,
            "green": 32,
            "yellow": 33,
            "blue": 34,
            "magenta": 35,
            "cyan": 36
        };

    exportObject.TerminalReporter = function(options) {
        var self = this;
        self.started = false;
        self.finished = false;

        // sanitize arguments
        options = options || {};
        self.verbosity = typeof options.verbosity === "number" ? options.verbosity : DEFAULT_VERBOSITY;
        self.color = options.color;
        self.showStack = options.showStack;

        var indent_string = '  ',
            startTime,
            currentSuite = null,
            totalSpecsExecuted = 0,
            totalSpecsSkipped = 0,
            totalSpecsDisabled = 0,
            totalSpecsFailed = 0,
            totalSpecsDefined,
            // when use use fit, jasmine never calls suiteStarted / suiteDone, so make a fake one to use
            fakeFocusedSuite = {
                id: 'focused',
                description: 'focused specs',
                fullName: 'focused specs'
            };

        var __suites = {}, __specs = {};
        function getSuite(suite) {
            __suites[suite.id] = extend(__suites[suite.id] || {}, suite);
            return __suites[suite.id];
        }
        function getSpec(spec) {
            __specs[spec.id] = extend(__specs[spec.id] || {}, spec);
            return __specs[spec.id];
        }

        self.jasmineStarted = function(summary) {
            totalSpecsDefined = summary && summary.totalSpecsDefined || NaN;
            startTime = exportObject.startTime = new Date();
            self.started = true;
        };
        self.suiteStarted = function(suite) {
            suite = getSuite(suite);
            suite._specs = 0;
            suite._nestedSpecs = 0;
            suite._failures = 0;
            suite._nestedFailures = 0;
            suite._skipped = 0;
            suite._nestedSkipped = 0;
            suite._disabled = 0;
            suite._nestedDisabled = 0;
            suite._depth = currentSuite ? currentSuite._depth+1 : 1;
            suite._parent = currentSuite;
            currentSuite = suite;
            if (self.verbosity > 2) {
                log(indentWithLevel(suite._depth, inColor(suite.description, "bold")));
            }
        };
        self.specStarted = function(spec) {
            if (!currentSuite) {
                // focused spec (fit) -- suiteStarted was never called
                self.suiteStarted(fakeFocusedSuite);
            }
            spec = getSpec(spec);
            spec._suite = currentSuite;
            spec._depth = currentSuite._depth+1;
            currentSuite._specs++;
            if (self.verbosity > 2) {
                log(indentWithLevel(spec._depth, spec.description + ' ...'));
            }
        };
        self.specDone = function(spec) {
            spec = getSpec(spec);
            var failed = false,
                skipped = false,
                disabled = false,
                color = 'green',
                resultText = '';
            if (isSkipped(spec)) {
                skipped = true;
                color = '';
                spec._suite._skipped++;
                totalSpecsSkipped++;
            }
            if (isFailed(spec)) {
                failed = true;
                color = 'red';
                spec._suite._failures++;
                totalSpecsFailed++;
            }
            if (isDisabled(spec)) {
                disabled = true;
                color = 'yellow';
                spec._suite._disabled++;
                totalSpecsDisabled++;
            }
            totalSpecsExecuted++;

            if (self.verbosity === 2) {
                resultText = failed ? 'F' : skipped ? 'S' : disabled ? 'D' : '.';
            } else if (self.verbosity > 2) {
                resultText = ' ' + (failed ? 'Failed' : skipped ? 'Skipped' : disabled ? 'Disabled' : 'Passed');
            }
            log(inColor(resultText, color));

            if (failed) {
                if (self.verbosity === 1) {
                    log(spec.fullName);
                } else if (self.verbosity === 2) {
                    log(' ');
                    log(indentWithLevel(spec._depth, spec.fullName));
                }

                for (var i = 0; i < spec.failedExpectations.length; i++) {
                    log(inColor(indentWithLevel(spec._depth, indent_string + spec.failedExpectations[i].message), color));
                    if (self.showStack){
                        logStackLines(spec._depth, spec.failedExpectations[i].stack.split('\n'));
                    }
                }
            }
        };
        self.suiteDone = function(suite) {
            suite = getSuite(suite);
            if (suite._parent === UNDEFINED) {
                // disabled suite (xdescribe) -- suiteStarted was never called
                self.suiteStarted(suite);
            }
            if (suite._parent) {
                suite._parent._specs += suite._specs + suite._nestedSpecs;
                suite._parent._failures += suite._failures + suite._nestedFailures;
                suite._parent._skipped += suite._skipped + suite._nestedSkipped;
                suite._parent._disabled += suite._disabled + suite._nestedDisabled;

            }
            currentSuite = suite._parent;
            if (self.verbosity < 3) {
                return;
            }

            var total = suite._specs + suite._nestedSpecs,
                failed = suite._failures + suite._nestedFailures,
                skipped = suite._skipped + suite._nestedSkipped,
                disabled = suite._disabled + suite._nestedDisabled,
                passed = total - failed - skipped,
                color = failed ? 'red+bold' : 'green+bold',
                str = passed + ' of ' + total + ' passed (' + skipped + ' skipped, ' + disabled + ' disabled)';
            log(indentWithLevel(suite._depth, inColor(str+'.', color)));
        };
        self.jasmineDone = function() {
            if (currentSuite) {
                // focused spec (fit) -- suiteDone was never called
                self.suiteDone(fakeFocusedSuite);
            }
            var now = new Date(),
                dur = elapsed(startTime, now),
                total = totalSpecsDefined || totalSpecsExecuted,
                disabled = total - totalSpecsExecuted + totalSpecsDisabled,
                skipped = totalSpecsSkipped,
                spec_str = total + (total === 1 ? " spec, " : " specs, "),
                fail_str = totalSpecsFailed + (totalSpecsFailed === 1 ? " failure, " : " failures, "),
                skip_str = skipped + " skipped, ",
                disabled_str = disabled + " disabled in ",
                summary_str = spec_str + fail_str + skip_str + disabled_str + dur + "s.",
                result_str = (totalSpecsFailed && "FAILURE: " || "SUCCESS: ") + summary_str,
                result_color = totalSpecsFailed && "red+bold" || "green+bold";

            if (self.verbosity === 2) {
                log('');
            }

            if (self.verbosity > 0) {
                log(inColor(result_str, result_color));
            }
            //log("Specs skipped but not reported (entire suite skipped or targeted to specific specs)", totalSpecsDefined - totalSpecsExecuted + totalSpecsDisabled);

            self.finished = true;
            // this is so phantomjs-testrunner.js can tell if we're done executing
            exportObject.endTime = now;
        };
        function indentWithLevel(level, string) {
            return new Array(level).join(indent_string) + string;
        }
        function logStackLines(depth, lines) {
            lines.forEach(function(line){
                log(inColor(indentWithLevel(depth, indent_string + line), 'magenta'));
            });
        }
        function inColor(string, color) {
            var color_attributes = color && color.split("+"),
                ansi_string = "",
                i;

            if (!self.color || !color_attributes) {
                return string;
            }

            for(i = 0; i < color_attributes.length; i++) {
                ansi_string += "\033[" + ATTRIBUTES_TO_ANSI[color_attributes[i]] + "m";
            }
            ansi_string += string + "\033[" + ATTRIBUTES_TO_ANSI["off"] + "m";

            return ansi_string;
        }
    };
})(this);
