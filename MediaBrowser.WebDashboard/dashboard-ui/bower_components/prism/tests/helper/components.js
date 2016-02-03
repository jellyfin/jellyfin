"use strict";

var fs = require("fs");
var vm = require("vm");

var fileContent = fs.readFileSync(__dirname + "/../../components.js", "utf8");
var context = {};
vm.runInNewContext(fileContent, context);

module.exports = context.components;
