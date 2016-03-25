import test from 'ava';
import fn from '../';

test('should extract query string from url', t => {
	t.is(fn.extract('http://foo.bar/?abc=def&hij=klm'), 'abc=def&hij=klm');
	t.is(fn.extract('http://foo.bar/?'), '');
});

test('should handle strings not containing query string', t => {
	t.is(fn.extract('http://foo.bar/'), '');
	t.is(fn.extract(''), '');
});

test('should throw for invalid values', t => {
	t.throws(fn.extract.bind(fn, null), TypeError);
	t.throws(fn.extract.bind(fn, undefined), TypeError);
});
