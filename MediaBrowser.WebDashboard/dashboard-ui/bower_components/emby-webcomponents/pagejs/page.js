define([], function () {

    'use strict';

    /**
     * Detect click event
     */
    var clickEvent = ('undefined' !== typeof document) && document.ontouchstart ? 'touchstart' : 'click';

    /**
     * To work properly with the URL
     * history.location generated polyfill in https://github.com/devote/HTML5-History-API
     */

    var location = ('undefined' !== typeof window) && (window.history.location || window.location);

    /**
     * Perform initial dispatch.
     */

    var dispatch = true;


    /**
     * Decode URL components (query string, pathname, hash).
     * Accommodates both regular percent encoding and x-www-form-urlencoded format.
     */
    var decodeURLComponents = true;

    /**
     * Base path.
     */

    var base = '';

    /**
     * Running flag.
     */

    var running;

    /**
     * HashBang option
     */

    var hashbang = false;

    var enableHistory = false;

    /**
     * Previous context, for capturing
     * page exit events.
     */

    var prevContext;

    /**
     * Register `path` with callback `fn()`,
     * or route `path`, or redirection,
     * or `page.start()`.
     *
     *   page(fn);
     *   page('*', fn);
     *   page('/user/:id', load, user);
     *   page('/user/' + user.id, { some: 'thing' });
     *   page('/user/' + user.id);
     *   page('/from', '/to')
     *   page();
     *
     * @param {String|Function} path
     * @param {Function} fn...
     * @api public
     */

    function page(path, fn) {
        // <callback>
        if ('function' === typeof path) {
            return page('*', path);
        }

        // route <path> to <callback ...>
        if ('function' === typeof fn) {
            var route = new Route(path);
            for (var i = 1; i < arguments.length; ++i) {
                page.callbacks.push(route.middleware(arguments[i]));
            }
            // show <path> with [state]
        } else if ('string' === typeof path) {
            page['string' === typeof fn ? 'redirect' : 'show'](path, fn);
            // start [options]
        } else {
            page.start(path);
        }
    }

    /**
     * Callback functions.
     */

    page.callbacks = [];
    page.exits = [];

    /**
     * Current path being processed
     * @type {String}
     */
    page.current = '';

    /**
     * Number of pages navigated to.
     * @type {number}
     *
     *     page.len == 0;
     *     page('/login');
     *     page.len == 1;
     */

    page.len = 0;

    /**
     * Get or set basepath to `path`.
     *
     * @param {String} path
     * @api public
     */

    page.base = function (path) {
        if (0 === arguments.length) {
            return base;
        }
        base = path;
    };

    /**
     * Bind with the given `options`.
     *
     * Options:
     *
     *    - `click` bind to click events [true]
     *    - `popstate` bind to popstate [true]
     *    - `dispatch` perform initial dispatch [true]
     *
     * @param {Object} options
     * @api public
     */

    page.start = function (options) {
        options = options || {};
        if (running) {
            return;
        }
        running = true;
        if (false === options.dispatch) {
            dispatch = false;
        }
        if (false === options.decodeURLComponents) {
            decodeURLComponents = false;
        }
        if (false !== options.popstate) {
            window.addEventListener('popstate', onpopstate, false);
        }
        if (false !== options.click) {
            document.addEventListener(clickEvent, onclick, false);
        }
        if (options.enableHistory != null) {
            enableHistory = options.enableHistory;
        }
        if (true === options.hashbang) {
            hashbang = true;
        }
        if (!dispatch) {
            return;
        }
        var url = (hashbang && ~location.hash.indexOf('#!')) ? location.hash.substr(2) + location.search : location.pathname + location.search + location.hash;
        page.replace(url, null, true, dispatch);
    };

    /**
     * Unbind click and popstate event handlers.
     *
     * @api public
     */

    page.stop = function () {
        if (!running) {
            return;
        }
        page.current = '';
        page.len = 0;
        running = false;
        document.removeEventListener(clickEvent, onclick, false);
        window.removeEventListener('popstate', onpopstate, false);
    };

    /**
     * Show `path` with optional `state` object.
     *
     * @param {String} path
     * @param {Object} state
     * @param {Boolean} dispatch
     * @return {Context}
     * @api public
     */

    page.show = function (path, state, dispatch, push, isBack) {
        var ctx = new Context(path, state);
        ctx.isBack = isBack;
        page.current = ctx.path;
        if (false !== dispatch) {
            page.dispatch(ctx);
        }
        if (false !== ctx.handled && false !== push) {
            ctx.pushState();
        }
        return ctx;
    };

    /**
     * Goes back in the history
     * Back should always let the current route push state and then go back.
     *
     * @param {String} path - fallback path to go back if no more history exists, if undefined defaults to page.base
     * @param {Object} [state]
     * @api public
     */

    page.back = function (path, state) {

        if (enableHistory) {
            // Keep it simple and mimic browser back
            history.back();
            return;
        }

        if (page.len > 0) {
            // this may need more testing to see if all browsers
            // wait for the next tick to go back in history
            if (enableHistory) {
                history.back();
            } else {

                if (backStack.length > 2) {
                    backStack.length--;
                    var previousState = backStack[backStack.length - 1];
                    page.show(previousState.path, previousState.state, true, false, true);
                }
            }
            page.len--;
        } else if (path) {
            setTimeout(function () {
                page.show(path, state);
            });
        } else {
            setTimeout(function () {
                page.show(base, state);
            });
        }
    };

    page.enableNativeHistory = function () {
        return enableHistory;
    };

    page.canGoBack = function () {
        if (enableHistory) {
            return history.length > 1;
        }
        return (page.len || 0) > 0;
    };

    /**
     * Register route to redirect from one path to other
     * or just redirect to another route
     *
     * @param {String} from - if param 'to' is undefined redirects to 'from'
     * @param {String} [to]
     * @api public
     */
    page.redirect = function (from, to) {
        // Define route from a path to another
        if ('string' === typeof from && 'string' === typeof to) {
            page(from, function (e) {
                setTimeout(function () {
                    page.replace(to);
                }, 0);
            });
        }

        // Wait for the push state and replace it with another
        if ('string' === typeof from && 'undefined' === typeof to) {
            setTimeout(function () {
                page.replace(from);
            }, 0);
        }
    };

    /**
     * Replace `path` with optional `state` object.
     *
     * @param {String} path
     * @param {Object} state
     * @return {Context}
     * @api public
     */


    page.replace = function (path, state, init, dispatch, isBack) {
        var ctx = new Context(path, state);
        ctx.isBack = isBack;
        page.current = ctx.path;
        ctx.init = init;
        ctx.save(); // save before dispatching, which may redirect
        if (false !== dispatch) {
            page.dispatch(ctx);
        }
        return ctx;
    };

    /**
     * Dispatch the given `ctx`.
     *
     * @param {Object} ctx
     * @api private
     */

    page.dispatch = function (ctx) {
        var prev = prevContext,
          i = 0,
          j = 0;

        prevContext = ctx;

        function nextExit() {
            var fn = page.exits[j++];
            if (!fn) {
                return nextEnter();
            }
            fn(prev, nextExit);
        }

        function nextEnter() {
            var fn = page.callbacks[i++];

            if (ctx.path !== page.current) {
                ctx.handled = false;
                return;
            }
            if (!fn) {
                return unhandled(ctx);
            }
            fn(ctx, nextEnter);
        }

        if (prev) {
            nextExit();
        } else {
            nextEnter();
        }
    };

    /**
     * Unhandled `ctx`. When it's not the initial
     * popstate then redirect. If you wish to handle
     * 404s on your own use `page('*', callback)`.
     *
     * @param {Context} ctx
     * @api private
     */

    function unhandled(ctx) {
        if (ctx.handled) {
            return;
        }
        var current;

        if (hashbang) {
            current = base + location.hash.replace('#!', '');
        } else {
            current = location.pathname + location.search;
        }

        if (current === ctx.canonicalPath) {
            return;
        }
        page.stop();
        ctx.handled = false;
        location.href = ctx.canonicalPath;
    }

    /**
     * Register an exit route on `path` with
     * callback `fn()`, which will be called
     * on the previous context when a new
     * page is visited.
     */
    page.exit = function (path, fn) {
        if (typeof path === 'function') {
            return page.exit('*', path);
        }

        var route = new Route(path);
        for (var i = 1; i < arguments.length; ++i) {
            page.exits.push(route.middleware(arguments[i]));
        }
    };

    /**
     * Remove URL encoding from the given `str`.
     * Accommodates whitespace in both x-www-form-urlencoded
     * and regular percent-encoded form.
     *
     * @param {str} URL component to decode
     */
    function decodeURLEncodedURIComponent(val) {
        if (typeof val !== 'string') { return val; }
        return decodeURLComponents ? decodeURIComponent(val.replace(/\+/g, ' ')) : val;
    }

    /**
     * Initialize a new "request" `Context`
     * with the given `path` and optional initial `state`.
     *
     * @param {String} path
     * @param {Object} state
     * @api public
     */

    function Context(path, state) {
        if ('/' === path[0] && 0 !== path.indexOf(base)) {
            path = base + (hashbang ? '#!' : '') + path;
        }
        var i = path.indexOf('?');

        this.canonicalPath = path;
        this.path = path.replace(base, '') || '/';
        if (hashbang) {
            this.path = this.path.replace('#!', '') || '/';
        }

        this.title = document.title;
        this.state = state || {};
        this.state.path = path;
        this.querystring = ~i ? decodeURLEncodedURIComponent(path.slice(i + 1)) : '';
        this.pathname = decodeURLEncodedURIComponent(~i ? path.slice(0, i) : path);
        this.params = {};

        // fragment
        this.hash = '';
        if (!hashbang) {
            if (!~this.path.indexOf('#')) {
                return;
            }
            var parts = this.path.split('#');
            this.path = parts[0];
            this.hash = decodeURLEncodedURIComponent(parts[1]) || '';
            this.querystring = this.querystring.split('#')[0];
        }
    }

    /**
     * Expose `Context`.
     */

    page.Context = Context;
    var backStack = [];

    /**
   * Push state.
   *
   * @api private
   */

    Context.prototype.pushState = function () {
        page.len++;

        if (enableHistory) {
            history.pushState(this.state, this.title, hashbang && this.path !== '/' ? '#!' + this.path : this.canonicalPath);
        } else {
            backStack.push({
                state: this.state,
                title: this.title,
                url: (hashbang && this.path !== '/' ? '#!' + this.path : this.canonicalPath),
                path: this.path
            });
        }
    };

    /**
     * Save the context state.
     *
     * @api public
     */

    Context.prototype.save = function () {

        if (enableHistory) {
            history.replaceState(this.state, this.title, hashbang && this.path !== '/' ? '#!' + this.path : this.canonicalPath);
        } else {
            backStack[page.len || 0] = {
                state: this.state,
                title: this.title,
                url: (hashbang && this.path !== '/' ? '#!' + this.path : this.canonicalPath),
                path: this.path
            };
        }
    };

    /**
     * Initialize `Route` with the given HTTP `path`,
     * and an array of `callbacks` and `options`.
     *
     * Options:
     *
     *   - `sensitive`    enable case-sensitive routes
     *   - `strict`       enable strict matching for trailing slashes
     *
     * @param {String} path
     * @param {Object} options.
     * @api private
     */

    function Route(path, options) {
        options = options || {};
        this.path = (path === '*') ? '(.*)' : path;
        this.method = 'GET';
        this.regexp = pathToRegexp(this.path,
          this.keys = [],
          options.sensitive,
          options.strict);
    }

    /**
     * Expose `Route`.
     */

    page.Route = Route;

    /**
     * Return route middleware with
     * the given callback `fn()`.
     *
     * @param {Function} fn
     * @return {Function}
     * @api public
     */

    Route.prototype.middleware = function (fn) {
        var self = this;
        return function (ctx, next) {
            if (self.match(ctx.path, ctx.params)) {
                return fn(ctx, next);
            }
            next();
        };
    };

    /**
     * Check if this route matches `path`, if so
     * populate `params`.
     *
     * @param {String} path
     * @param {Object} params
     * @return {Boolean}
     * @api private
     */

    Route.prototype.match = function (path, params) {
        var keys = this.keys,
          qsIndex = path.indexOf('?'),
          pathname = ~qsIndex ? path.slice(0, qsIndex) : path,
          m = this.regexp.exec(decodeURIComponent(pathname));

        if (!m) {
            return false;
        }

        for (var i = 1, len = m.length; i < len; ++i) {
            var key = keys[i - 1];
            var val = decodeURLEncodedURIComponent(m[i]);
            if (val !== undefined || !(hasOwnProperty.call(params, key.name))) {
                params[key.name] = val;
            }
        }

        return true;
    };


    var previousPopState = {};

    function ignorePopState(event) {

        var state = event.state || {};

        if (previousPopState.navigate === false) {
            // Ignore
            previousPopState = state;
            return true;
        }

        previousPopState = state;
        return false;
    }

    page.pushState = function (state, title, url) {

        if (hashbang) {
            url = '#!' + url;
        }

        history.pushState(state, title, url);
        previousPopState = state;
    };

    /**
   * Handle "populate" events.
   */

    var onpopstate = (function () {
        var loaded = false;
        if ('undefined' === typeof window) {
            return;
        }
        if (document.readyState === 'complete') {
            loaded = true;
        } else {
            window.addEventListener('load', function () {
                setTimeout(function () {
                    loaded = true;
                }, 0);
            });
        }
        return function onpopstate(e) {
            if (!loaded) {
                return;
            }
            if (ignorePopState(e)) {
                return;
            }
            if (e.state) {
                var path = e.state.path;
                page.replace(path, e.state, null, null, true);
            } else {
                page.show(location.pathname + location.hash, undefined, undefined, false, true);
            }
        };
    })();
    /**
     * Handle "click" events.
     */

    function onclick(e) {

        if (1 !== which(e)) {
            return;
        }

        if (e.metaKey || e.ctrlKey || e.shiftKey) {
            return;
        }
        if (e.defaultPrevented) {
            return;
        }



        // ensure link
        var el = e.target;
        while (el && 'A' !== el.nodeName) {
            el = el.parentNode;
        }
        if (!el || 'A' !== el.nodeName) {
            return;
        }



        // Ignore if tag has
        // 1. "download" attribute
        // 2. rel="external" attribute
        if (el.hasAttribute('download') || el.getAttribute('rel') === 'external') {
            return;
        }

        // ensure non-hash for the same path
        var link = el.getAttribute('href');
        if (link === '#') {
            e.preventDefault();
            return;
        }
        if (!hashbang && el.pathname === location.pathname && (el.hash || '#' === link)) {
            return;
        }



        // Check for mailto: in the href
        if (link && link.indexOf('mailto:') > -1) {
            return;
        }

        // check target
        if (el.target) {
            return;
        }

        // x-origin
        if (!sameOrigin(el.href)) {
            return;
        }



        // rebuild path
        var path = el.pathname + el.search + (el.hash || '');

        // same page
        var orig = path;

        if (path.indexOf(base) === 0) {
            path = path.substr(base.length);
        }

        if (hashbang) {
            path = path.replace('#!', '');
        }

        if (base && orig === path) {
            return;
        }

        e.preventDefault();
        page.show(orig);
    }

    /**
     * Event button.
     */

    function which(e) {
        e = e || window.event;
        return null === e.which ? e.button : e.which;
    }

    /**
     * Check if `href` is the same origin.
     */

    function sameOrigin(href) {
        var origin = location.protocol + '//' + location.hostname;
        if (location.port) {
            origin += ':' + location.port;
        }
        return (href && (0 === href.indexOf(origin)));
    }

    page.sameOrigin = sameOrigin;

    /**
     * The main path matching regexp utility.
     *
     * @type {RegExp}
     */
    var PATH_REGEXP = new RegExp([
        // Match escaped characters that would otherwise appear in future matches.
        // This allows the user to escape special characters that won't transform.
        '(\\\\.)',
        // Match Express-style parameters and un-named parameters with a prefix
        // and optional suffixes. Matches appear as:
        //
        // "/:test(\\d+)?" => ["/", "test", "\d+", undefined, "?", undefined]
        // "/route(\\d+)"  => [undefined, undefined, undefined, "\d+", undefined, undefined]
        // "/*"            => ["/", undefined, undefined, undefined, undefined, "*"]
        '([\\/.])?(?:(?:\\:(\\w+)(?:\\(((?:\\\\.|[^()])+)\\))?|\\(((?:\\\\.|[^()])+)\\))([+*?])?|(\\*))'
    ].join('|'), 'g');

    /**
     * Parse a string for the raw tokens.
     *
     * @param  {String} str
     * @return {Array}
     */
    function parse(str) {
        var tokens = [];
        var key = 0;
        var index = 0;
        var path = '';
        var res;

        while ((res = PATH_REGEXP.exec(str)) != null) {
            var m = res[0];
            var escaped = res[1];
            var offset = res.index;
            path += str.slice(index, offset);
            index = offset + m.length;

            // Ignore already escaped sequences.
            if (escaped) {
                path += escaped[1];
                continue;
            }

            // Push the current path onto the tokens.
            if (path) {
                tokens.push(path);
                path = '';
            }

            var prefix = res[2];
            var name = res[3];
            var capture = res[4];
            var group = res[5];
            var suffix = res[6];
            var asterisk = res[7];

            var repeat = suffix === '+' || suffix === '*';
            var optional = suffix === '?' || suffix === '*';
            var delimiter = prefix || '/';
            var pattern = capture || group || (asterisk ? '.*' : '[^' + delimiter + ']+?');

            tokens.push({
                name: name || key++,
                prefix: prefix || '',
                delimiter: delimiter,
                optional: optional,
                repeat: repeat,
                pattern: escapeGroup(pattern)
            });
        }

        // Match any characters still remaining.
        if (index < str.length) {
            path += str.substr(index);
        }

        // If the path exists, push it onto the end.
        if (path) {
            tokens.push(path);
        }

        return tokens;
    }

    var isarray = Array.isArray || function (arr) {
        return Object.prototype.toString.call(arr) === '[object Array]';
    };

    /**
     * Escape a regular expression string.
     *
     * @param  {String} str
     * @return {String}
     */
    function escapeString(str) {
        return str.replace(/([.+*?=^!:${}()[\]|\/])/g, '\\$1');
    }

    /**
     * Escape the capturing group by escaping special characters and meaning.
     *
     * @param  {String} group
     * @return {String}
     */
    function escapeGroup(group) {
        return group.replace(/([=!:$\/()])/g, '\\$1');
    }

    /**
     * Attach the keys as a property of the regexp.
     *
     * @param  {RegExp} re
     * @param  {Array}  keys
     * @return {RegExp}
     */
    function attachKeys(re, keys) {
        re.keys = keys;
        return re;
    }

    /**
     * Get the flags for a regexp from the options.
     *
     * @param  {Object} options
     * @return {String}
     */
    function flags(options) {
        return options.sensitive ? '' : 'i';
    }

    /**
     * Pull out keys from a regexp.
     *
     * @param  {RegExp} path
     * @param  {Array}  keys
     * @return {RegExp}
     */
    function regexpToRegexp(path, keys) {
        // Use a negative lookahead to match only capturing groups.
        var groups = path.source.match(/\((?!\?)/g);

        if (groups) {
            for (var i = 0; i < groups.length; i++) {
                keys.push({
                    name: i,
                    prefix: null,
                    delimiter: null,
                    optional: false,
                    repeat: false,
                    pattern: null
                });
            }
        }

        return attachKeys(path, keys);
    }

    /**
     * Transform an array into a regexp.
     *
     * @param  {Array}  path
     * @param  {Array}  keys
     * @param  {Object} options
     * @return {RegExp}
     */
    function arrayToRegexp(path, keys, options) {
        var parts = [];

        for (var i = 0; i < path.length; i++) {
            parts.push(pathToRegexp(path[i], keys, options).source);
        }

        var regexp = new RegExp('(?:' + parts.join('|') + ')', flags(options));

        return attachKeys(regexp, keys);
    }

    /**
     * Create a path regexp from string input.
     *
     * @param  {String} path
     * @param  {Array}  keys
     * @param  {Object} options
     * @return {RegExp}
     */
    function stringToRegexp(path, keys, options) {
        var tokens = parse(path);
        var re = tokensToRegExp(tokens, options);

        // Attach keys back to the regexp.
        for (var i = 0; i < tokens.length; i++) {
            if (typeof tokens[i] !== 'string') {
                keys.push(tokens[i]);
            }
        }

        return attachKeys(re, keys);
    }

    /**
     * Expose a function for taking tokens and returning a RegExp.
     *
     * @param  {Array}  tokens
     * @param  {Array}  keys
     * @param  {Object} options
     * @return {RegExp}
     */
    function tokensToRegExp(tokens, options) {
        options = options || {};

        var strict = options.strict;
        var end = options.end !== false;
        var route = '';
        var lastToken = tokens[tokens.length - 1];
        var endsWithSlash = typeof lastToken === 'string' && /\/$/.test(lastToken);

        // Iterate over the tokens and create our regexp string.
        for (var i = 0; i < tokens.length; i++) {
            var token = tokens[i];

            if (typeof token === 'string') {
                route += escapeString(token);
            } else {
                var prefix = escapeString(token.prefix);
                var capture = token.pattern;

                if (token.repeat) {
                    capture += '(?:' + prefix + capture + ')*';
                }

                if (token.optional) {
                    if (prefix) {
                        capture = '(?:' + prefix + '(' + capture + '))?';
                    } else {
                        capture = '(' + capture + ')?';
                    }
                } else {
                    capture = prefix + '(' + capture + ')';
                }

                route += capture;
            }
        }

        // In non-strict mode we allow a slash at the end of match. If the path to
        // match already ends with a slash, we remove it for consistency. The slash
        // is valid at the end of a path match, not in the middle. This is important
        // in non-ending mode, where "/test/" shouldn't match "/test//route".
        if (!strict) {
            route = (endsWithSlash ? route.slice(0, -2) : route) + '(?:\\/(?=$))?';
        }

        if (end) {
            route += '$';
        } else {
            // In non-ending mode, we need the capturing groups to match as much as
            // possible by using a positive lookahead to the end or next path segment.
            route += strict && endsWithSlash ? '' : '(?=\\/|$)';
        }

        return new RegExp('^' + route, flags(options));
    }

    /**
     * Normalize the given path string, returning a regular expression.
     *
     * An empty array can be passed in for the keys, which will hold the
     * placeholder key descriptions. For example, using `/user/:id`, `keys` will
     * contain `[{ name: 'id', delimiter: '/', optional: false, repeat: false }]`.
     *
     * @param  {(String|RegExp|Array)} path
     * @param  {Array}                 [keys]
     * @param  {Object}                [options]
     * @return {RegExp}
     */
    function pathToRegexp(path, keys, options) {
        keys = keys || [];

        if (!isarray(keys)) {
            options = keys;
            keys = [];
        } else if (!options) {
            options = {};
        }

        if (path instanceof RegExp) {
            return regexpToRegexp(path, keys, options);
        }

        if (isarray(path)) {
            return arrayToRegexp(path, keys, options);
        }

        return stringToRegexp(path, keys, options);
    }

    return page;

});