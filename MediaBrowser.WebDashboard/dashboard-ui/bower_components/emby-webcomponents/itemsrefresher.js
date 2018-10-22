define(["playbackManager", "serverNotifications", "events"], function(playbackManager, serverNotifications, events) {
    "use strict";

    function onUserDataChanged(e, apiClient, userData) {
        var instance = this,
            eventsToMonitor = getEventsToMonitor(instance); - 1 !== eventsToMonitor.indexOf("markfavorite") ? instance.notifyRefreshNeeded() : -1 !== eventsToMonitor.indexOf("markplayed") && instance.notifyRefreshNeeded()
    }

    function getEventsToMonitor(instance) {
        var options = instance.options,
            monitor = options ? options.monitorEvents : null;
        return monitor ? monitor.split(",") : []
    }

    function onTimerCreated(e, apiClient, data) {
        var instance = this;
        if (-1 !== getEventsToMonitor(instance).indexOf("timers")) return void instance.notifyRefreshNeeded()
    }

    function onSeriesTimerCreated(e, apiClient, data) {
        var instance = this;
        if (-1 !== getEventsToMonitor(instance).indexOf("seriestimers")) return void instance.notifyRefreshNeeded()
    }

    function onTimerCancelled(e, apiClient, data) {
        var instance = this;
        if (-1 !== getEventsToMonitor(instance).indexOf("timers")) return void instance.notifyRefreshNeeded()
    }

    function onSeriesTimerCancelled(e, apiClient, data) {
        var instance = this;
        if (-1 !== getEventsToMonitor(instance).indexOf("seriestimers")) return void instance.notifyRefreshNeeded()
    }

    function onLibraryChanged(e, apiClient, data) {
        var instance = this,
            eventsToMonitor = getEventsToMonitor(instance);
        if (-1 === eventsToMonitor.indexOf("seriestimers") && -1 === eventsToMonitor.indexOf("timers")) {
            var itemsAdded = data.ItemsAdded || [],
                itemsRemoved = data.ItemsRemoved || [];
            if (itemsAdded.length || itemsRemoved.length) {
                var options = instance.options || {},
                    parentId = options.parentId;
                if (parentId) {
                    var foldersAddedTo = data.FoldersAddedTo || [],
                        foldersRemovedFrom = data.FoldersRemovedFrom || [],
                        collectionFolders = data.CollectionFolders || [];
                    if (-1 === foldersAddedTo.indexOf(parentId) && -1 === foldersRemovedFrom.indexOf(parentId) && -1 === collectionFolders.indexOf(parentId)) return
                }
                instance.notifyRefreshNeeded()
            }
        }
    }

    function onPlaybackStopped(e, stopInfo) {
        var instance = this,
            state = stopInfo.state,
            eventsToMonitor = getEventsToMonitor(instance);
        if (state.NowPlayingItem && "Video" === state.NowPlayingItem.MediaType) {
            if (-1 !== eventsToMonitor.indexOf("videoplayback")) return void instance.notifyRefreshNeeded(!0)
        } else if (state.NowPlayingItem && "Audio" === state.NowPlayingItem.MediaType && -1 !== eventsToMonitor.indexOf("audioplayback")) return void instance.notifyRefreshNeeded(!0)
    }

    function addNotificationEvent(instance, name, handler, owner) {
        var localHandler = handler.bind(instance);
        owner = owner || serverNotifications, events.on(owner, name, localHandler), instance["event_" + name] = localHandler
    }

    function removeNotificationEvent(instance, name, owner) {
        var handler = instance["event_" + name];
        handler && (owner = owner || serverNotifications, events.off(owner, name, handler), instance["event_" + name] = null)
    }

    function ItemsRefresher(options) {
        this.options = options || {}, addNotificationEvent(this, "UserDataChanged", onUserDataChanged), addNotificationEvent(this, "TimerCreated", onTimerCreated), addNotificationEvent(this, "SeriesTimerCreated", onSeriesTimerCreated), addNotificationEvent(this, "TimerCancelled", onTimerCancelled), addNotificationEvent(this, "SeriesTimerCancelled", onSeriesTimerCancelled), addNotificationEvent(this, "LibraryChanged", onLibraryChanged), addNotificationEvent(this, "playbackstop", onPlaybackStopped, playbackManager)
    }

    function clearRefreshInterval(instance, isPausing) {
        instance.refreshInterval && (clearInterval(instance.refreshInterval), instance.refreshInterval = null, isPausing || (instance.refreshIntervalEndTime = null))
    }

    function resetRefreshInterval(instance, intervalMs) {
        if (clearRefreshInterval(instance), !intervalMs) {
            var options = instance.options;
            options && (intervalMs = options.refreshIntervalMs)
        }
        intervalMs && (instance.refreshInterval = setInterval(instance.notifyRefreshNeeded.bind(instance), intervalMs), instance.refreshIntervalEndTime = (new Date).getTime() + intervalMs)
    }

    function onDataFetched(result) {
        resetRefreshInterval(this), this.afterRefresh && this.afterRefresh(result)
    }
    return ItemsRefresher.prototype.pause = function() {
        clearRefreshInterval(this, !0), this.paused = !0
    }, ItemsRefresher.prototype.resume = function(options) {
        this.paused = !1;
        var refreshIntervalEndTime = this.refreshIntervalEndTime;
        if (refreshIntervalEndTime) {
            var remainingMs = refreshIntervalEndTime - (new Date).getTime();
            remainingMs > 0 && !this.needsRefresh ? resetRefreshInterval(this, remainingMs) : (this.needsRefresh = !0, this.refreshIntervalEndTime = null)
        }
        return this.needsRefresh || options && options.refresh ? this.refreshItems() : Promise.resolve()
    }, ItemsRefresher.prototype.refreshItems = function() {
        return this.fetchData ? this.paused ? (this.needsRefresh = !0, Promise.resolve()) : (this.needsRefresh = !1, this.fetchData().then(onDataFetched.bind(this))) : Promise.resolve()
    }, ItemsRefresher.prototype.notifyRefreshNeeded = function(isInForeground) {
        if (this.paused) return void(this.needsRefresh = !0);
        var timeout = this.refreshTimeout;
        timeout && clearTimeout(timeout), !0 === isInForeground ? this.refreshItems() : this.refreshTimeout = setTimeout(this.refreshItems.bind(this), 1e4)
    }, ItemsRefresher.prototype.destroy = function() {
        clearRefreshInterval(this), removeNotificationEvent(this, "UserDataChanged"), removeNotificationEvent(this, "TimerCreated"), removeNotificationEvent(this, "SeriesTimerCreated"), removeNotificationEvent(this, "TimerCancelled"), removeNotificationEvent(this, "SeriesTimerCancelled"), removeNotificationEvent(this, "LibraryChanged"), removeNotificationEvent(this, "playbackstop", playbackManager), this.fetchData = null, this.options = null
    }, ItemsRefresher
});