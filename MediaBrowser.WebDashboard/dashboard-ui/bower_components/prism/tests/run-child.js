"use strict";

var TestCase = require("./helper/test-case");
var argv = require("yargs").argv;

if (argv.language) {
	process.on('message', function (data) {
		if (data.filePath) {
			try {
				TestCase.runTestCase(argv.language, data.filePath);
				process.send({success: true});
			} catch (e) {
				process.send({error: e});
			}
		}
	});
}