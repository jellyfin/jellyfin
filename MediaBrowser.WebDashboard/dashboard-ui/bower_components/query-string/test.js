'use strict';
var assert = require('assert');
var qs = require('./');

describe('.parse()', function () {
	it('query strings starting with a `?`', function () {
		assert.deepEqual(qs.parse('?foo=bar'), {foo: 'bar'});
	});

	it('query strings starting with a `#`', function () {
		assert.deepEqual(qs.parse('#foo=bar'), {foo: 'bar'});
	});

	it('query strings starting with a `&', function () {
		assert.deepEqual(qs.parse('&foo=bar&foo=baz'), {foo: ['bar', 'baz']});
	});

	it('parse a query string', function () {
		assert.deepEqual(qs.parse('foo=bar'), {foo: 'bar'});
	});

	it('parse multiple query string', function () {
		assert.deepEqual(qs.parse('foo=bar&key=val'), {foo: 'bar', key: 'val'});
	});

	it('parse query string without a value', function () {
		assert.deepEqual(qs.parse('foo'), {foo: null});
		assert.deepEqual(qs.parse('foo&key'), {foo: null, key: null});
		assert.deepEqual(qs.parse('foo=bar&key'), {foo: 'bar', key: null});
	});

	it('return empty object if no qss can be found', function () {
		assert.deepEqual(qs.parse('?'), {});
		assert.deepEqual(qs.parse('&'), {});
		assert.deepEqual(qs.parse('#'), {});
		assert.deepEqual(qs.parse(' '), {});
	});

	it('handle `+` correctly', function () {
		assert.deepEqual(qs.parse('foo+faz=bar+baz++'), {'foo faz': 'bar baz  '});
	});

	it('handle multiple of the same key', function () {
		assert.deepEqual(qs.parse('foo=bar&foo=baz'), {foo: ['bar', 'baz']});
	});

	it('query strings params including embedded `=`', function () {
		assert.deepEqual(qs.parse('?param=http%3A%2F%2Fsomeurl%3Fid%3D2837'), {param: 'http://someurl?id=2837'});
	});
});

describe('.stringify()', function () {
	it('stringify', function () {
		assert.strictEqual(qs.stringify({foo: 'bar'}), 'foo=bar');
		assert.strictEqual(qs.stringify({foo: 'bar', bar: 'baz'}), 'bar=baz&foo=bar');
	});

	it('different types', function () {
		assert.strictEqual(qs.stringify(), '');
		assert.strictEqual(qs.stringify(0), '');
	});

	it('URI encode', function () {
		assert.strictEqual(qs.stringify({'foo bar': 'baz faz'}), 'foo%20bar=baz%20faz');
	});

	it('handle array value', function () {
		assert.strictEqual(qs.stringify({abc: 'abc', foo: ['bar', 'baz']}), 'abc=abc&foo=bar&foo=baz');
	});
});

describe('.extract()', function () {
	it('should extract qs from url', function () {
		assert.equal(qs.extract('http://foo.bar/?abc=def&hij=klm'), 'abc=def&hij=klm');
		assert.equal(qs.extract('http://foo.bar/?'), '');
	});

	it('should handle strings not containing qs', function () {
		assert.equal(qs.extract('http://foo.bar/'), '');
		assert.equal(qs.extract(''), '');
	});

	it('should throw for invalid values', function() {
		assert.throws(function() {
			qs.extract(null);
		}, TypeError);
		assert.throws(function() {
			qs.extract(undefined);
		}, TypeError);
	});
});