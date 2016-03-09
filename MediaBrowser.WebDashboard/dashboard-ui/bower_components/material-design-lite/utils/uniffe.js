/**
 *
 *  Material Design Lite
 *  Copyright 2015 Google Inc. All rights reserved.
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License
 *
 */
'use strict';

var through = require('through2');
var escodegen = require('escodegen');
var acorn = require('acorn');

function uniffe(contents) {
  var comments = [];
  var tokens = [];

  var ast = acorn.parse(contents, {
    ranges: true,
    onComment: comments,
    onToken: tokens
  });

  escodegen.attachComments(ast, comments, tokens);

  if (ast.body[0].expression === undefined ||
      ast.body[0].expression.callee === undefined) {
    return contents;
  }

  var rootProgram = ast.body[0].expression.callee.body;

  rootProgram.type = 'Program';
  // drop use strict
  rootProgram.body = rootProgram.body.slice(1);
  // attach all leading comments from outside iffe
  rootProgram.leadingComments = ast.body[0].leadingComments;

  return escodegen.generate(rootProgram, {comment: true});
}

module.exports = function() {
  return through.obj(function(file, enc, cb) {
    if (file.isBuffer()) {
      file.contents = new Buffer(uniffe(file.contents.toString(enc)), enc);
    }

    cb(null, file);
  });
};
