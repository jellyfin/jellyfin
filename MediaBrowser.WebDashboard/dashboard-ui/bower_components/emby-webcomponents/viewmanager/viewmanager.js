define(['viewcontainer', 'focusManager', 'queryString', 'layoutManager'], function (viewcontainer, focusManager, queryString, layoutManager) {
    'use strict';

    var currentView;
    var dispatchPageEvents;

    viewcontainer.setOnBeforeChange(function (newView, isRestored, options) {

        var lastView = currentView;
        if (lastView) {
            var beforeHideResult = dispatchViewEvent(lastView, 'viewbeforehide', null, true);

            if (!beforeHideResult) {
                // todo: cancel
            }
        }

        if (!newView.initComplete) {
            newView.initComplete = true;

            var eventDetail = getViewEventDetail(newView, options, false);

            if (options.controllerFactory) {

                // Use controller method
                var controller = new options.controllerFactory(newView, eventDetail.detail.params);
            }

            if (!options.controllerFactory || dispatchPageEvents) {
                dispatchViewEvent(newView, 'viewinit');
            }
        }

        dispatchViewEvent(newView, 'viewbeforeshow', isRestored);
    });

    function onViewChange(view, options, isRestore) {

        var lastView = currentView;
        if (lastView) {
            dispatchViewEvent(lastView, 'viewhide');
        }

        currentView = view;

        var eventDetail = getViewEventDetail(view, options, isRestore);

        if (!isRestore) {
            if (options.autoFocus !== false) {
                focusManager.autoFocus(view);
            }
        }
        else if (!layoutManager.mobile) {
            if (view.activeElement && document.body.contains(view.activeElement) && focusManager.isCurrentlyFocusable(view.activeElement)) {
                focusManager.focus(view.activeElement);
            } else {
                focusManager.autoFocus(view);
            }
        }

        view.dispatchEvent(new CustomEvent('viewshow', eventDetail));

        if (dispatchPageEvents) {
            view.dispatchEvent(new CustomEvent('pageshow', eventDetail));
        }
    }

    function dispatchViewEvent(view, eventName, isRestored, isCancellable) {

        var eventResult = view.dispatchEvent(new CustomEvent(eventName, {
            detail: {
                type: view.getAttribute('data-type'),
                isRestored: isRestored
            },
            bubbles: true,
            cancelable: isCancellable || false
        }));

        if (dispatchPageEvents) {
            view.dispatchEvent(new CustomEvent(eventName.replace('view', 'page'), {
                detail: {
                    type: view.getAttribute('data-type'),
                    isRestored: isRestored
                },
                bubbles: true,
                cancelable: false
            }));
        }

        return eventResult;
    }

    function getViewEventDetail(view, options, isRestore) {

        var url = options.url;
        var state = options.state;
        var index = url.indexOf('?');
        var params = index === -1 ? {} : queryString.parse(url.substring(index + 1));

        return {
            detail: {
                type: view.getAttribute('data-type'),
                params: params,
                isRestored: isRestore,
                state: options.state,

                // The route options
                options: options.options || {}
            },
            bubbles: true,
            cancelable: false
        };
    }

    function resetCachedViews() {
        // Reset all cached views whenever the skin changes
        viewcontainer.reset();
    }

    document.addEventListener('skinunload', resetCachedViews);

    //events.on(connectionManager, 'localusersignedin', resetCachedViews);
    //events.on(connectionManager, 'localusersignedout', resetCachedViews);

    function ViewManager() {
    }

    ViewManager.prototype.loadView = function (options) {

        var lastView = currentView;

        // Record the element that has focus
        if (lastView) {
            lastView.activeElement = document.activeElement;
        }

        if (options.cancel) {
            return;
        }

        viewcontainer.loadView(options).then(function (view) {

            onViewChange(view, options);
        });
    };

    ViewManager.prototype.tryRestoreView = function (options, onViewChanging) {

        if (options.cancel) {
            return Promise.reject({ cancelled: true });
        }

        // Record the element that has focus
        if (currentView) {
            currentView.activeElement = document.activeElement;
        }

        return viewcontainer.tryRestoreView(options).then(function (view) {

            onViewChanging();
            onViewChange(view, options, true);

        });
    };

    ViewManager.prototype.currentView = function () {
        return currentView;
    };

    ViewManager.prototype.dispatchPageEvents = function (value) {
        dispatchPageEvents = value;
    };

    return new ViewManager();
});
