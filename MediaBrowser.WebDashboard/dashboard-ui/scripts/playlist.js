var PlayList = {

    queue: Array(),

    addItem: function (item) {
        PlayList.queue.push(item);
    },

    removeItem: function (index) {
        PlayList.queue.splice(index, 1);
    },

    playItem: function (index) {
        MediaPlayer.play(PlayList.queue[index]);
        PlayList.queue.shift();
    }

};