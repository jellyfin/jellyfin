(function(){

if (
	typeof self !== 'undefined' && !self.Prism ||
	typeof global !== 'undefined' && !global.Prism
) {
	return;
}

for (var language in Prism.languages) {
	var tokens = Prism.languages[language];
	
    tokens.tab = /\t/g;
    tokens.crlf = /\r\n/g;
    tokens.lf = /\n/g;
	tokens.cr = /\r/g;
}

})();
