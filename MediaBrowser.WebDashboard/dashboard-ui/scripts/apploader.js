! function() {
    "use strict";

    function loadApp() {
        var script = document.createElement("script"),
            src = "./scripts/site.js";
        self.dashboardVersion && (src += "?v=" + self.dashboardVersion), script.src = src, document.head.appendChild(script)
    }! function() {
        var src, script = document.createElement("script");
        src = self.Promise ? "./bower_components/alameda/alameda.js" : "./bower_components/requirejs/require.js", self.dashboardVersion && (src += "?v=" + self.dashboardVersion), script.src = src, script.onload = loadApp, document.head.appendChild(script)
    }()
}();