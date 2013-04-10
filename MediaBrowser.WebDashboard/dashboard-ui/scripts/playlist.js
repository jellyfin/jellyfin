var Playlist = (function() {

    var self = this;

    self.queue = [];

    self.add = function(item) {

        queue.push(item);
    };

    self.remove = function (index) {

        queue.splice(index, 1);
    };

    self.play = function (index) {
        
        MediaPlayer.play(queue[index]);
        queue.shift();
    };

    return self;

})();