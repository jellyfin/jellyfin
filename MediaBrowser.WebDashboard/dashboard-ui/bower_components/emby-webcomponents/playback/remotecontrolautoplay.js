define(["events", "playbackManager"], function(events, playbackManager) {
    "use strict";

    function transferPlayback(oldPlayer, newPlayer) {
        var state = playbackManager.getPlayerState(oldPlayer),
            item = state.NowPlayingItem;
        if (item) {
            var playState = state.PlayState || {},
                resumePositionTicks = playState.PositionTicks || 0;
            playbackManager.stop(oldPlayer).then(function() {
                playbackManager.play({
                    ids: [item.Id],
                    serverId: item.ServerId,
                    startPositionTicks: resumePositionTicks
                }, newPlayer)
            })
        }
    }
    events.on(playbackManager, "playerchange", function(e, newPlayer, newTarget, oldPlayer) {
        if (oldPlayer && newPlayer) return oldPlayer.isLocalPlayer ? newPlayer.isLocalPlayer ? void console.log("Skipping remote control autoplay because newPlayer is a local player") : void transferPlayback(oldPlayer, newPlayer) : void console.log("Skipping remote control autoplay because oldPlayer is not a local player")
    })
});