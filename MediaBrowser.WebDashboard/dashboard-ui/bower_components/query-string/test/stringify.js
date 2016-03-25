import test from 'ava';
import fn from '../';

test('stringify', t => {
	t.same(fn.stringify({foo: 'bar'}), 'foo=bar');
	t.same(fn.stringify({foo: 'bar', bar: 'baz'}), 'bar=baz&foo=bar');
});

test('different types', t => {
	t.same(fn.stringify(), '');
	t.same(fn.stringify(0), '');
});

test('URI encode', t => {
	t.same(fn.stringify({'foo bar': 'baz faz'}), 'foo%20bar=baz%20faz');
	t.same(fn.stringify({'foo bar': 'baz\'faz'}), 'foo%20bar=baz%27faz');
});

test('handle array value', t => {
	t.same(fn.stringify({abc: 'abc', foo: ['bar', 'baz']}), 'abc=abc&foo=bar&foo=baz');
});

test('handle empty array value', t => {
	t.same(fn.stringify({abc: 'abc', foo: []}), 'abc=abc');
});

test('should not encode undefined values', t => {
	t.same(fn.stringify({abc: undefined, foo: 'baz'}), 'foo=baz');
});

test('should encode null values as just a key', t => {
	t.same(fn.stringify({xyz: null, abc: null, foo: 'baz'}), 'abc&foo=baz&xyz');
});
