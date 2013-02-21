#!/usr/bin/env python
from struct import *
from socket import *
from optparse import OptionParser

UDP_ADDR = "0.0.0.0"
UDP_MULTICAST_ADDR = "239.255.255.100"
UDP_PORT = 7724
BUFFER_SIZE = 65536
#HEADER_KEYS = ['Logger', 'Level', 'Source-File', 'Source-Function', 'Source-Line', 'TimeStamp']
HEADER_KEYS = {
    'mini': ('Level'),
    'standard': ('Logger', 'Level', 'Source-Function'),
    'long': ('Logger', 'Level', 'Source-File', 'Source-Line', 'Source-Function'),
    'all': ('Logger', 'Level', 'Source-File', 'Source-Line', 'Source-Function', 'TimeStamp'),
    'custom': ()
}

Senders = {}

class LogRecord:
    def __init__(self, data):
        offset = 0
        self.headers = {}
        for line in data.split("\r\n"):
            offset += len(line)+2
            if ':' not in line: break
            key,value=line.split(":",1)
            self.headers[key] = value.strip()
        self.body = data[offset:]
        
    def __getitem__(self, index):
        return self.headers[index]
        
    def format(self, sender_index, keys):
        parts = ['['+str(sender_index)+']']
        if 'Level' in keys:
            parts.append('['+self.headers['Level']+']')
        if 'Logger' in keys:
            parts.append(self.headers['Logger'])
        if 'TimeStamp' in keys:
            parts.append(self.headers['TimeStamp'])
        if 'Source-File' in keys:
            if 'Source-Line' in keys:
                parts.append(self.headers['Source-File']+':'+self.headers['Source-Line'])
            else:
                parts.append(self.headers['Source-File'])
        if 'TimeStamp' in keys:
            parts.append(self.headers['TimeStamp'])
        if 'Source-Function' in keys:
            parts.append(self.headers['Source-Function'])
        parts.append(self.body)
        return ' '.join(parts)
    
class Listener:
    def __init__(self, format='standard', port=UDP_PORT):
        self.socket = socket(AF_INET,SOCK_DGRAM)
	self.socket.setsockopt(SOL_SOCKET, SO_REUSEADDR, 1)
	mreq = pack("4sl", inet_aton(UDP_MULTICAST_ADDR), INADDR_ANY)
	self.socket.setsockopt(IPPROTO_IP, IP_ADD_MEMBERSHIP, mreq)
        self.socket.bind((UDP_ADDR, port))
        self.format_keys = HEADER_KEYS[format]
        
    def listen(self):
        while True:
            data,addr = self.socket.recvfrom(BUFFER_SIZE)
            sender_index = len(Senders.keys())
            if addr in Senders:
                sender_index = Senders[addr]
            else:
                print "### NEW SENDER:", addr
                Senders[addr] = sender_index
            
            record = LogRecord(data)
            print record.format(sender_index, self.format_keys)
        

### main
parser = OptionParser(usage="%prog [options]")
parser.add_option("-p", "--port", dest="port", help="port number to listen on", type="int", default=UDP_PORT)
parser.add_option("-f", "--format", dest="format", help="log format (mini, standard, long, or all)", choices=('mini', 'standard', 'long', 'all'), default='standard')
(options, args) = parser.parse_args()

print "Listening on port", options.port
l = Listener(format=options.format, port=options.port)
l.listen()
