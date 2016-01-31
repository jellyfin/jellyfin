/*===========================
 Get Dom libraries
 ===========================*/
var swiperDomPlugins = ['jQuery', 'Zepto', 'Dom7'];
for (var i = 0; i < swiperDomPlugins.length; i++) {
	if (window[swiperDomPlugins[i]]) {
		addLibraryPlugin(window[swiperDomPlugins[i]]);
	}
}
// Required DOM Plugins
var domLib;
if (typeof Dom7 === 'undefined') {
	domLib = window.Dom7 || window.Zepto || window.jQuery;
}
else {
	domLib = Dom7;
}