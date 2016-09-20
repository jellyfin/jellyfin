/*!
 * Social Share Kit v1.0.10 (http://socialsharekit.com)
 * Copyright 2015 Social Share Kit / Kaspars Sprogis.
 * @Licensed under Creative Commons Attribution-NonCommercial 3.0 license:
 * https://github.com/darklow/social-share-kit/blob/master/LICENSE
 * ---
 */
var SocialShareKit = (function () {
    var supportsShare = /(twitter|facebook|google-plus|pinterest|tumblr|vk|linkedin|email)/,
        sep = '*|*', wrap, _wrap;

    // Wrapper to support multiple instances per page by selector
    _wrap = function (opts) {
        var options = opts || {},
            selector = options.selector || '.ssk';
        this.nodes = $(selector);
        this.options = options;
    };

    // Instance related functions
    _wrap.prototype = {
        share: function () {
            var els = this.nodes,
                options = this.options,
                urlsToCount = {};

            ready(function () {
                if (!els.length)
                    return;

                each(els, function (el) {
                    var network = elSupportsShare(el), uniqueKey;
                    if (!network) {
                        return;
                    }
                    removeEventListener(el, 'click', onClick);
                    addEventListener(el, 'click', onClick);

                    // Gather icons with share counts
                    if (el.parentNode.className.indexOf('ssk-count') !== -1) {
                        network = network[0];
                        uniqueKey = network + sep + getShareUrl(options, network, el);
                        if (!(uniqueKey in urlsToCount)) {
                            urlsToCount[uniqueKey] = [];
                        }
                        urlsToCount[uniqueKey].push(el);
                    }
                });

                processShareCount();
            });

            function onClick(e) {
                var target = preventDefault(e),
                    match = elSupportsShare(target),
                    network = match[0],
                    url;

                if (!match)
                    return;

                url = getUrl(options, network, target);
                if (!url)
                    return;

                // To use Twitter intent events, replace URL and use Twitter native share JS
                if (window.twttr && target.getAttribute('href').indexOf('twitter.com/intent/') !== -1) {
                    target.setAttribute('href', url);
                    return;
                }

                if (network != 'email') {
                    var win = winOpen(url);

                    if (options.onOpen) {
                        options.onOpen(target, network, url, win);
                    }

                    if (options.onClose) {
                        var closeInt = window.setInterval(function () {
                            if (win.closed !== false) {
                                window.clearInterval(closeInt);
                                options.onClose(target, network, url, win);
                            }
                        }, 250);
                    }

                } else {
                    document.location = url;
                }
            }

            function processShareCount() {
                var a, ref;
                for (a in urlsToCount) {
                    ref = a.split(sep);
                    (function (els) {
                        getCount(ref[0], ref[1], options, function (cnt) {
                            for (var c in els)
                                addCount(els[c], cnt);
                        });
                    })(urlsToCount[a]);
                }
            }

            return this.nodes;
        }
    };

    wrap = function (selector) {
        return new _wrap(selector);
    };

    function init(opts) {
        return wrap(opts).share();
    }

    function ready(fn) {
        if (document.readyState != 'loading') {
            fn();
        } else if (document.addEventListener) {
            document.addEventListener('DOMContentLoaded', fn);
        } else {
            document.attachEvent('onreadystatechange', function () {
                if (document.readyState != 'loading')
                    fn();
            });
        }
    }

    function $(selector) {
        return document.querySelectorAll(selector);
    }

    function each(elements, fn) {
        for (var i = 0; i < elements.length; i++)
            fn(elements[i], i);
    }

    function addEventListener(el, eventName, handler) {
        if (el.addEventListener) {
            el.addEventListener(eventName, handler);
        } else {
            el.attachEvent('on' + eventName, function () {
                handler.call(el);
            });
        }
    }

    function removeEventListener(el, eventName, handler) {
        if (el.removeEventListener)
            el.removeEventListener(eventName, handler);
        else
            el.detachEvent('on' + eventName, handler);
    }

    function elSupportsShare(el) {
        return el.className.match(supportsShare);
    }


    function preventDefault(e) {
        var evt = e || window.event; // IE8 compatibility
        if (evt.preventDefault) {
            evt.preventDefault();
        } else {
            evt.returnValue = false;
            evt.cancelBubble = true;
        }
        return evt.currentTarget || evt.srcElement;
    }

    function winOpen(url) {
        var width = 575, height = 400,
            left = (document.documentElement.clientWidth / 2 - width / 2),
            top = (document.documentElement.clientHeight - height) / 2,
            opts = 'status=1,resizable=yes' +
                ',width=' + width + ',height=' + height +
                ',top=' + top + ',left=' + left,
            win = window.open(url, '', opts);
        win.focus();
        return win;
    }

    function getUrl(options, network, el) {
        var url, dataOpts = getDataOpts(options, network, el),
            shareUrl = getShareUrl(options, network, el, dataOpts),
            title = typeof dataOpts['title'] !== 'undefined' ? dataOpts['title'] : getTitle(network),
            text = typeof dataOpts['text'] !== 'undefined' ? dataOpts['text'] : getText(network),
            image = dataOpts['image'] ? dataOpts['image'] : getMetaContent('og:image'),
            via = typeof dataOpts['via'] !== 'undefined' ? dataOpts['via'] : getMetaContent('twitter:site'),
            paramsObj = {
                shareUrl: shareUrl,
                title: title,
                text: text,
                image: image,
                via: via,
                options: options,
                shareUrlEncoded: function () {
                    return encodeURIComponent(this.shareUrl);
                }
            };
        switch (network) {
            case 'facebook':
                url = 'https://www.facebook.com/share.php?u=' + paramsObj.shareUrlEncoded();
                break;
            case 'twitter':
                url = 'https://twitter.com/intent/tweet?url=' + paramsObj.shareUrlEncoded() +
                    '&text=' + encodeURIComponent(title + (text && title ? ' - ' : '') + text);
                if (via)
                    url += '&via=' + via.replace('@', '');
                break;
            case 'google-plus':
                url = 'https://plus.google.com/share?url=' + paramsObj.shareUrlEncoded();
                break;
            case 'pinterest':
                url = 'https://pinterest.com/pin/create/button/?url=' + paramsObj.shareUrlEncoded() +
                    '&description=' + encodeURIComponent(text);
                if (image)
                    url += '&media=' + encodeURIComponent(image);
                break;
            case 'tumblr':
                url = 'https://www.tumblr.com/share/link?url=' + paramsObj.shareUrlEncoded() +
                    '&name=' + encodeURIComponent(title) +
                    '&description=' + encodeURIComponent(text);
                break;
            case 'linkedin':
                url = 'https://www.linkedin.com/shareArticle?mini=true&url=' + paramsObj.shareUrlEncoded() +
                    '&title=' + encodeURIComponent(title) +
                    '&summary=' + encodeURIComponent(text);
                break;
            case 'vk':
                url = 'https://vkontakte.ru/share.php?url=' + paramsObj.shareUrlEncoded();
                break;
            case 'email':
                url = 'mailto:?subject=' + encodeURIComponent(title) +
                    '&body=' + encodeURIComponent(title + '\n' + shareUrl + '\n\n' + text + '\n');
                break;
        }

        paramsObj.networkUrl = url;

        if (options.onBeforeOpen) {
            options.onBeforeOpen(el, network, paramsObj)
        }

        return paramsObj.networkUrl;
    }

    function getShareUrl(options, network, el, dataOpts) {
        dataOpts = dataOpts || getDataOpts(options, network, el);
        return dataOpts['url'] || window.location.href;
    }

    function getTitle(network) {
        var title;
        if (network == 'twitter')
            title = getMetaContent('twitter:title');
        return title || document.title;
    }

    function getText(network) {
        var text;
        if (network == 'twitter')
            text = getMetaContent('twitter:description');
        return text || getMetaContent('description');
    }

    function getMetaContent(tagName, attr) {
        var text, tag = $('meta[' + (attr ? attr : tagName.indexOf('og:') === 0 ? 'property' : 'name') + '="' + tagName + '"]');
        if (tag.length) {
            text = tag[0].getAttribute('content') || '';
        }
        return text || ''
    }

    function getDataOpts(options, network, el) {
        var validOpts = ['url', 'title', 'text', 'image'],
            opts = {}, optValue, optKey, dataKey, a, parent = el.parentNode;
        network == 'twitter' && validOpts.push('via');
        for (a in validOpts) {
            optKey = validOpts[a];
            dataKey = 'data-' + optKey;
            optValue = el.getAttribute(dataKey) || parent.getAttribute(dataKey) ||
                (options[network] && typeof options[network][optKey] != 'undefined' ? options[network][optKey] : options[optKey]);
            if (typeof optValue != 'undefined') {
                opts[optKey] = optValue;
            }
        }
        return opts;
    }


    function addCount(el, cnt) {
        var newEl = document.createElement('div');
        newEl.innerHTML = cnt;
        newEl.className = 'ssk-num';
        el.appendChild(newEl);
    }

    function getCount(network, shareUrl, options, onReady) {
        var url, parseFunc, body,
            shareUrlEnc = encodeURIComponent(shareUrl);
        switch (network) {
            case 'facebook':
                url = 'https://graph.facebook.com/?id=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.share ? r.share.share_count : 0);
                };
                break;
            case 'twitter':
                if (options && options.twitter && options.twitter.countCallback) {
                    options.twitter.countCallback(shareUrl, onReady);
                }
                break;
            case 'google-plus':
                url = 'https://clients6.google.com/rpc?key=AIzaSyCKSbrvQasunBoV16zDH9R33D88CeLr9gQ';
                body = "[{\"method\":\"pos.plusones.get\",\"id\":\"p\"," +
                    "\"params\":{\"id\":\"" + shareUrl + "\",\"userId\":\"@viewer\",\"groupId\":\"@self\",\"nolog\":true}," +
                    "\"jsonrpc\":\"2.0\",\"key\":\"p\",\"apiVersion\":\"v1\"}]";
                parseFunc = function (r) {
                    r = JSON.parse(r);
                    if (r.length) {
                        return onReady(r[0].result.metadata.globalCounts.count);
                    }
                };
                ajax(url, parseFunc, body);
                return;
            case 'linkedin':
                url = 'https://www.linkedin.com/countserv/count/share?url=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.count);
                };
                break;
            case 'pinterest':
                url = 'https://api.pinterest.com/v1/urls/count.json?url=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.count);
                };
                break;
            case 'vk':
                url = 'https://vk.com/share.php?act=count&url=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r);
                };
                break;
        }
        url && parseFunc && JSONPRequest(network, url, parseFunc, body);
    }

    function ajax(url, callback, body) {
        var request = new XMLHttpRequest();
        request.onreadystatechange = function () {
            if (this.readyState === 4) {
                if (this.status >= 200 && this.status < 400) {
                    callback(this.responseText);
                }
            }
        };
        request.open('POST', url, true);
        request.setRequestHeader('Content-Type', 'application/json');
        request.send(body);
    }

    function JSONPRequest(network, url, callback) {
        var callbackName = 'cb_' + network + '_' + Math.round(100000 * Math.random()),
            script = document.createElement('script');
        window[callbackName] = function (data) {
            try { // IE8
                delete window[callbackName];
            } catch (e) {
            }
            document.body.removeChild(script);
            callback(data);
        };
        if (network == 'vk') {
            window['VK'] = {
                Share: {
                    count: function (a, b) {
                        window[callbackName](b);
                    }
                }
            };
        } else if (network == 'google-plus') {
            window['services'] = {
                gplus: {
                    cb: window[callbackName]
                }
            };
        }
        script.src = url + (url.indexOf('?') >= 0 ? '&' : '?') + 'callback=' + callbackName;
        document.body.appendChild(script);
        return true;
    }

    return {
        init: init
    };
})();

window.SocialShareKit = SocialShareKit;
