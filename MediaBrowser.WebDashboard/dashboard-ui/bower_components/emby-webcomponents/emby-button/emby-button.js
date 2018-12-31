define(['browser', 'dom', 'layoutManager', 'shell', 'appRouter', 'apphost', 'css!./emby-button', 'registerElement'], function (browser, dom, layoutManager, shell, appRouter, appHost) {
    'use strict';

    var EmbyButtonPrototype = Object.create(HTMLButtonElement.prototype);
    var EmbyLinkButtonPrototype = Object.create(HTMLAnchorElement.prototype);

    function openPremiumInfo() {

        require(['registrationServices'], function (registrationServices) {
            registrationServices.showPremiereInfo();
        });
    }

    function onAnchorClick(e) {

        var href = this.getAttribute('href') || '';

        if (href !== '#') {

            if (this.getAttribute('target')) {
                if (href.indexOf('emby.media/premiere') !== -1 && !appHost.supports('externalpremium')) {
                    e.preventDefault();
                    openPremiumInfo();
                }
                else if (!appHost.supports('targetblank')) {
                    e.preventDefault();
                    shell.openUrl(href);
                }
            } else {
                appRouter.handleAnchorClick(e);
            }
        } else {
            e.preventDefault();
        }
    }

    EmbyButtonPrototype.createdCallback = function () {

        if (this.classList.contains('emby-button')) {
            return;
        }

        this.classList.add('emby-button');

        if (browser.firefox) {
            // a ff hack is needed for vertical alignment
            this.classList.add('button-link-inline');
        }

        if (layoutManager.tv) {
            if (this.getAttribute('data-focusscale') !== 'false') {
                this.classList.add('emby-button-focusscale');
            }
            this.classList.add('emby-button-tv');
        }
    };

    EmbyButtonPrototype.attachedCallback = function () {

        if (this.tagName === 'A') {

            dom.removeEventListener(this, 'click', onAnchorClick, {
            });

            dom.addEventListener(this, 'click', onAnchorClick, {
            });

            if (this.getAttribute('data-autohide') === 'true') {
                if (appHost.supports('externallinks')) {
                    this.classList.remove('hide');
                } else {
                    this.classList.add('hide');
                }
            }
        }
    };

    EmbyButtonPrototype.detachedCallback = function () {

        dom.removeEventListener(this, 'click', onAnchorClick, {
        });
    };

    EmbyLinkButtonPrototype.createdCallback = EmbyButtonPrototype.createdCallback;
    EmbyLinkButtonPrototype.attachedCallback = EmbyButtonPrototype.attachedCallback;

    document.registerElement('emby-button', {
        prototype: EmbyButtonPrototype,
        extends: 'button'
    });

    document.registerElement('emby-linkbutton', {
        prototype: EmbyLinkButtonPrototype,
        extends: 'a'
    });

    // For extension purposes
    return EmbyButtonPrototype;
});