/* globals jasmineRequire, phantom */
// Verify arguments
var system = require('system');
var args;

if(phantom.args) {
    args = phantom.args;
} else {
    args = system.args.slice(1);//use system args for phantom 2.0+
}

if (args.length === 0) {
    console.log("Simple JasmineBDD test runner for phantom.js");
    console.log("Usage: phantomjs-testrunner.js url_to_runner.html");
    console.log("Accepts http:// and file:// urls");
    console.log("");
    console.log("NOTE: This script depends on jasmine.HtmlReporter being used\non the page, for the DOM elements it creates.\n");
    phantom.exit(2);
}
else {
    var fs = require("fs"),
        pages = [],
        page, address, resultsKey, i, l;


    var setupPageFn = function(p, k) {
        return function() {
            overloadPageEvaluate(p);
            setupWriteFileFunction(p, k, fs.separator);
        };
    };

    for (i = 0, l = args.length; i < l; i++) {
        address = args[i];
        console.log("Loading " + address);

        // if provided a url without a protocol, try to use file://
        address = address.indexOf("://") === -1 ? "file://" + address : address;

        // create a WebPage object to work with
        page = require("webpage").create();
        page.url = address;

        // When initialized, inject the reporting functions before the page is loaded
        // (and thus before it will try to utilize the functions)
        resultsKey = "__jr" + Math.ceil(Math.random() * 1000000);
        page.onInitialized = setupPageFn(page, resultsKey);
        page.open(address, processPage(null, page, resultsKey));
        pages.push(page);

        page.onConsoleMessage = logAndWorkAroundDefaultLineBreaking;
    }

    // bail when all pages have been processed
    setInterval(function(){
        var exit_code = 0;
        for (i = 0, l = pages.length; i < l; i++) {
            page = pages[i];
            if (page.__exit_code === null) {
                // wait until later
                return;
            }
            exit_code |= page.__exit_code;
        }
        phantom.exit(exit_code);
    }, 100);
}

// Thanks to hoisting, these helpers are still available when needed above
/**
 * Logs a message. Does not add a line-break for single characters '.' and 'F' or lines ending in ' ...'
 *
 * @param msg
 */
function logAndWorkAroundDefaultLineBreaking(msg) {
    var interpretAsWithoutNewline = /(^(\033\[\d+m)*[\.F](\033\[\d+m)*$)|( \.\.\.$)/;
    if (navigator.userAgent.indexOf("Windows") < 0 && interpretAsWithoutNewline.test(msg)) {
        try {
            system.stdout.write(msg);
        } catch (e) {
            var fs = require('fs');
            fs.write('/dev/stdout', msg, 'w');
        }
    } else {
        console.log(msg);
    }
}

/**
 * Stringifies the function, replacing any %placeholders% with mapped values.
 *
 * @param {function} fn The function to replace occurrences within.
 * @param {object} replacements Key => Value object of string replacements.
 */
function replaceFunctionPlaceholders(fn, replacements) {
    if (replacements && typeof replacements === "object") {
        fn = fn.toString();
        for (var p in replacements) {
            if (replacements.hasOwnProperty(p)) {
                var match = new RegExp("%" + p + "%", "g");
                do {
                    fn = fn.replace(match, replacements[p]);
                } while(fn.indexOf(match) !== -1);
            }
        }
    }
    return fn;
}

/**
 * Replaces the "evaluate" method with one we can easily do substitution with.
 *
 * @param {phantomjs.WebPage} page The WebPage object to overload
 */
function overloadPageEvaluate(page) {
    page._evaluate = page.evaluate;
    page.evaluate = function(fn, replacements) { return page._evaluate(replaceFunctionPlaceholders(fn, replacements)); };
    return page;
}

/** Stubs a fake writeFile function into the test runner.
 *
 * @param {phantomjs.WebPage} page The WebPage object to inject functions into.
 * @param {string} key The name of the global object in which file data should
 *                     be stored for later retrieval.
 */
// TODO: not bothering with error checking for now (closed environment)
function setupWriteFileFunction(page, key, path_separator) {
    page.evaluate(function(){
        window["%resultsObj%"] = {};
        window.fs_path_separator = "%fs_path_separator%";
        window.__phantom_writeFile = function(filename, text) {
            window["%resultsObj%"][filename] = text;
        };
    }, {resultsObj: key, fs_path_separator: path_separator.replace("\\", "\\\\")});
}

/**
 * Returns the loaded page's filename => output object.
 *
 * @param {phantomjs.WebPage} page The WebPage object to retrieve data from.
 * @param {string} key The name of the global object to be returned. Should
 *                     be the same key provided to setupWriteFileFunction.
 */
function getXmlResults(page, key) {
    return page.evaluate(function(){
        return window["%resultsObj%"] || {};
    }, {resultsObj: key});
}

/**
 * Processes a page.
 *
 * @param {string} status The status from opening the page via WebPage#open.
 * @param {phantomjs.WebPage} page The WebPage to be processed.
 */
function processPage(status, page, resultsKey) {
    if (status === null && page) {
        page.__exit_code = null;
        return function(stat){
            processPage(stat, page, resultsKey);
        };
    }
    if (status !== "success") {
        console.error("Unable to load resource: " + address);
        page.__exit_code = 2;
    }
    else {
        var isFinished = function() {
            return page.evaluate(function(){
                // if there's a JUnitXmlReporter, return a boolean indicating if it is finished
                if (window.jasmineReporters && window.jasmineReporters.startTime) {
                    return !!window.jasmineReporters.endTime;
                }
                // otherwise, scrape the DOM for the HtmlReporter "finished in ..." output
                var durElem = document.querySelector(".html-reporter .duration");
                if (!durElem) {
                    durElem = document.querySelector(".jasmine_html-reporter .duration");
                }
                return durElem && durElem.textContent && durElem.textContent.toLowerCase().indexOf("finished in") === 0;
            });
        };
        var getResultsFromHtmlRunner = function() {
            return page.evaluate(function(){
                var resultElem = document.querySelector(".html-reporter .alert .bar");
                if (!resultElem) {
                    resultElem = document.querySelector(".jasmine_html-reporter .alert .bar");
                }
                return resultElem && resultElem.textContent &&
                    resultElem.textContent.match(/(\d+) spec.* (\d+) failure.*/) ||
                   ["Unable to determine success or failure."];
            });
        };
        var timeout = 60000;
        var loopInterval = 100;
        var ival = setInterval(function(){
            if (isFinished()) {
                // get the results that need to be written to disk
                var fs = require("fs"),
                    xml_results = getXmlResults(page, resultsKey),
                    output;
                for (var filename in xml_results) {
                    if (xml_results.hasOwnProperty(filename) && (output = xml_results[filename]) && typeof(output) === "string") {
                        fs.write(filename, output, "w");
                    }
                }

                // print out a success / failure message of the results
                var results = getResultsFromHtmlRunner();
                var failures = Number(results[2]);
                if (failures > 0) {
                    page.__exit_code = 1;
                    clearInterval(ival);
                }
                else {
                    page.__exit_code = 0;
                    clearInterval(ival);
                }
            }
            else {
                timeout -= loopInterval;
                if (timeout <= 0) {
                    console.log('Page has timed out; aborting.');
                    page.__exit_code = 2;
                    clearInterval(ival);
                }
            }
        }, loopInterval);
    }
}
