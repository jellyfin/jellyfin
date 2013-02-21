import urllib2
import simplejson

# This example request includes an optional API key which you will need to
words = open('/usr/share/dict/words').readlines()
for word in words:
	word = word.rstrip()
	url = ('http://ajax.googleapis.com/ajax/services/search/web?v=1.0&q='+word+'+https&userip=67.169.84.240')
	#print url
	#print word.rstrip()
	request = urllib2.Request(url, None, {'Referer': 'http://test.com'})
	response = urllib2.urlopen(request)

	results = simplejson.load(response)
	for result in results['responseData']['results']:
		print result['unescapedUrl']
