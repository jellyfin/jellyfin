(function() {var chrome = window.chrome || {};
chrome.cast = chrome.cast || {};
chrome.cast.media = chrome.cast.media || {};
chrome.cast.ApiBootstrap_ = function() {
};
chrome.cast.ApiBootstrap_.EXTENSION_IDS = ["boadgeojelhgndaghljhdicfkmllpafd", "dliochdbjfkdbacpmhlcpmleaejidimm", "hfaagokkkhdbgiakmmlclaapfelnkoah", "fmfcbgogabcbclcofgocippekhfcmgfj", "enhhojjnijigcajfphajepfemndkmdlo"];
chrome.cast.ApiBootstrap_.findInstalledExtension_ = function(callback) {
  //chrome.cast.ApiBootstrap_.findInstalledExtensionHelper_(0, callback);
};
chrome.cast.ApiBootstrap_.findInstalledExtensionHelper_ = function(index, callback) {
  index == chrome.cast.ApiBootstrap_.EXTENSION_IDS.length ? callback(null) : chrome.cast.ApiBootstrap_.isExtensionInstalled_(chrome.cast.ApiBootstrap_.EXTENSION_IDS[index], function(installed) {
    installed ? callback(chrome.cast.ApiBootstrap_.EXTENSION_IDS[index]) : chrome.cast.ApiBootstrap_.findInstalledExtensionHelper_(index + 1, callback);
  });
};
chrome.cast.ApiBootstrap_.getCastSenderUrl_ = function(extensionId) {
  return "chrome-extension://" + extensionId + "/cast_sender.js";
};
chrome.cast.ApiBootstrap_.isExtensionInstalled_ = function(extensionId, callback) {
  var xmlhttp = new XMLHttpRequest;
  xmlhttp.onreadystatechange = function() {
    4 == xmlhttp.readyState && 200 == xmlhttp.status && callback(!0);
  };
  xmlhttp.onerror = function() {
    callback(!1);
  };

    try {
        // Throws an error in other browsers
        xmlhttp.open("GET", chrome.cast.ApiBootstrap_.getCastSenderUrl_(extensionId), !0);
        xmlhttp.send();
    } catch (ex) {
        
    }
};
chrome.cast.ApiBootstrap_.findInstalledExtension_(function(extensionId) {
  if (extensionId) {
    console.log("Found cast extension: " + extensionId);
    chrome.cast.extensionId = extensionId;
    var apiScript = document.createElement("script");
    apiScript.src = chrome.cast.ApiBootstrap_.getCastSenderUrl_(extensionId);
    (document.head || document.documentElement).appendChild(apiScript);
  } else {
    var msg = "No cast extension found";
    console.log(msg);
    var callback = window.__onGCastApiAvailable;
    callback && "function" == typeof callback && callback(!1, msg);
  }
});
})();
