/*!
 * Social Share Kit v1.0.3 (http://socialsharekit.com)
 * Copyright 2015 Social Share Kit / Kaspars Sprogis.
 * Licensed under Creative Commons Attribution-NonCommercial 3.0 license:
 * https://github.com/darklow/social-share-kit/blob/master/LICENSE
 * ---
 */
var SocialShareKit = (function () {
    var els, options, supportsShare, urlsToCount = {}, sep = '*|*';

    function init(opts) {
        options = opts || {};
        supportsShare = /(twitter|facebook|google-plus|pinterest|tumblr|vk|linkedin|email)/;
        ready(function () {
            els = $(options.selector || '.ssk');
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
                    //networksToCount.push(network);
                    network = network[0];
                    uniqueKey = network + sep + getShareUrl(network, el);
                    if (!(uniqueKey in urlsToCount)) {
                        urlsToCount[uniqueKey] = [];
                    }
                    urlsToCount[uniqueKey].push(el);
                }
            });

            processShareCount();
        });
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

    function onClick(e) {
        var target = preventDefault(e),
            match = elSupportsShare(target), url;
        if (!match)
            return;

        url = getUrl(match[0], target);
        if (!url)
            return;
        if (match[0] != 'email') {
            winOpen(url);
        } else {
            document.location = url;
        }
    }

    function preventDefault(e) {
        var evt = e || window.event; // IE8 compatibility
        if (evt.preventDefault) {
            evt.preventDefault();
        } else {
            evt.returnValue = false;
            evt.cancelBubble = true;
        }
        return evt.target || evt.srcElement;
    }

    function winOpen(url) {
        var width = 575, height = 400,
            left = (document.documentElement.clientWidth / 2 - width / 2),
            top = (document.documentElement.clientHeight - height) / 2,
            opts = 'status=1,resizable=yes' +
                ',width=' + width + ',height=' + height +
                ',top=' + top + ',left=' + left;
        win = window.open(url, '', opts);
        win.focus();
        return win;
    }

    function getUrl(network, el) {
        var url, dataOpts = getDataOpts(network, el),
            shareUrl = getShareUrl(network, el, dataOpts),
            shareUrlEnc = encodeURIComponent(shareUrl),
            title = typeof dataOpts['title'] !== 'undefined' ? dataOpts['title'] : getTitle(network),
            text = typeof dataOpts['text'] !== 'undefined' ? dataOpts['text'] : getText(network),
            image = dataOpts['image'], via = dataOpts['via'];
        switch (network) {
            case 'facebook':
                url = 'https://www.facebook.com/share.php?u=' + shareUrlEnc;
                break;
            case 'twitter':
                url = 'https://twitter.com/share?url=' + shareUrlEnc +
                '&text=' + encodeURIComponent(title + (text && title ? ' - ' : '') + text);
                via = via || getMetaContent('twitter:site');
                if (via)
                    url += '&via=' + via.replace('@', '');
                break;
            case 'google-plus':
                url = 'https://plus.google.com/share?url=' + shareUrlEnc;
                break;
            case 'pinterest':
                url = 'http://pinterest.com/pin/create/button/?url=' + shareUrlEnc +
                '&description=' + encodeURIComponent(text);
                image = image || getMetaContent('og:image');
                if (image)
                    url += '&media=' + encodeURIComponent(image);
                break;
            case 'tumblr':
                url = 'http://www.tumblr.com/share/link?url=' + shareUrlEnc +
                '&name=' + encodeURIComponent(title) +
                '&description=' + encodeURIComponent(text);
                break;
            case 'linkedin':
                url = 'http://www.linkedin.com/shareArticle?mini=true&url=' + shareUrlEnc +
                '&title=' + encodeURIComponent(title) +
                '&summary=' + encodeURIComponent(text);
                break;
            case 'vk':
                url = 'http://vkontakte.ru/share.php?url=' + shareUrlEnc;
                break;
            case 'email':
                url = 'mailto:?subject=' + encodeURIComponent(title) +
                '&body=' + encodeURIComponent(title + '\n' + shareUrl + '\n\n' + text + '\n');
                break;
        }
        return url;
    }

    function getShareUrl(network, el, dataOpts) {
        dataOpts = dataOpts || getDataOpts(network, el);
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

    function getDataOpts(network, el) {
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

    function processShareCount() {
        var a, ref;
        for (a in urlsToCount) {
            ref = a.split(sep);
            (function (els) {
                getCount(ref[0], ref[1], function (cnt) {
                    for (var c in els)
                        addCount(els[c], cnt);
                });
            })(urlsToCount[a]);
        }
    }

    function addCount(el, cnt) {
        var newEl = document.createElement('div');
        newEl.innerHTML = cnt;
        newEl.className = 'ssk-num';
        el.appendChild(newEl);
    }

    function getCount(network, shareUrl, onReady) {
        var url, parseFunc, body,
            shareUrlEnc = encodeURIComponent(shareUrl);
        switch (network) {
            case 'facebook':
                url = 'http://graph.facebook.com/?id=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.shares ? r.shares : 0);
                };
                break;
            case 'twitter':
                url = 'http://cdn.api.twitter.com/1/urls/count.json?url=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.count);
                };
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
                url = 'http://www.linkedin.com/countserv/count/share?url=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.count);
                };
                break;
            case 'pinterest':
                url = 'http://api.pinterest.com/v1/urls/count.json?url=' + shareUrlEnc;
                parseFunc = function (r) {
                    return onReady(r.count);
                };
                break;
            case 'vk':
                url = 'http://vk.com/share.php?act=count&url=' + shareUrlEnc;
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
