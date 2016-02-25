// This is mostly for support of the es6 module export
// syntax with the babel compiler, it looks like it doesnt support
// function exports like we are used to in node/commonjs
module.exports = require('./hls.js').default;
