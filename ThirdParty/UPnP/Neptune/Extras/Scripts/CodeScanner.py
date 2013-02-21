#! /usr/bin/python

import os
import os.path
import re
import sys

ErrorPattern = re.compile('([A-Z]{3}_ERROR_[A-Z_0-9]+)\s+=?\s*\(?([A-Z_0-9-][A-Z_0-9-+ ]+[A-Z_0-9])')
LoggerPattern = re.compile('NPT_SET_LOCAL_LOGGER\s*\([ "]*(\S+)[ "]*\)')
NakedErrorPattern = re.compile('return.*[ \(]..._FAILURE')
FilePatternH = re.compile('^.*\.h$')
FilePatternC = re.compile('^.*\.(c|cpp)$')

Errors = {}
Codes = {}
Loggers = []

def ResolveErrors():
    keep_going = True
    while keep_going:
        keep_going = False
        for key in Errors.keys():
            value = Errors[key]
            if type(value) is str:
                elements = [x.strip() for x in value.split('-')]
                if len(elements[0]) == 0:
                    first = 0
                else:
                    first = elements[0]
                if Errors.has_key(first):
                    first = Errors[first]
                if not type(first) is str:
                    second = int(elements[1])
                    Errors[key] = first-second
                    keep_going = True
            
    
def AnalyzeErrorCodes(file):
    input = open(file)
    for line in input.readlines():
        m = ErrorPattern.search(line)
        if m:
            Errors[m.group(1)] = m.group(2)
    input.close()
    
def ScanErrorCodes(top):
    for root, dirs, files in os.walk(top):
        for file in files:
            if FilePatternH.match(file):
                 AnalyzeErrorCodes(os.path.join(root, file))
        
    ResolveErrors()
    for key in Errors:
        #print key,"==>",Errors[key]
        if (key.find("ERROR_BASE") > 1): continue
        if Codes.has_key(Errors[key]):
            raise Exception("duplicate error code: "+ str(key) +" --> " + str(Errors[key]) + "=" + Codes[Errors[key]])
        Codes[Errors[key]] = key
        
    sorted_keys = Codes.keys()
    sorted_keys.sort()
    sorted_keys.reverse()
    last = 0
    for code in sorted_keys:
        if type(code) != int:
        	continue
        if code != last-1:
            print 
        print code,"==>", Codes[code]
        last = code

def AnalyzeLoggers(file):
    input = open(file)
    for line in input.readlines():
        m = LoggerPattern.search(line)
        if m:
            if m.group(1) not in Loggers:
                Loggers.append(m.group(1))
    input.close()
            
def ScanLoggers(top):
    for root, dirs, files in os.walk(top):
        for file in files:
            if FilePatternC.match(file):
                 AnalyzeLoggers(os.path.join(root, file))
        
    Loggers.sort()
    for logger in Loggers:
        print logger

def AnalyzeNakedErrors(file, prefix):
    line_number = 0
    input = open(file)
    for line in input.readlines():
        line_number += 1
        m = NakedErrorPattern.search(line)
        if m:
            print file[len(prefix):],line_number," --> ", line,
    input.close()

def ScanNakedErrors(top):
    for root, dirs, files in os.walk(top):
        for file in files:
            if FilePatternC.match(file):
                 AnalyzeNakedErrors(os.path.join(root, file), top)

def FindTabsInFile(file):
    input = open(file)
    for line in input.readlines():
        if line.find('\t') >= 0:
            print "TAB found in", file
            input.close()
            return
    input.close()
    
def FindTabs(top):
    for root, dirs, files in os.walk(top):
        for file in files:
            if FilePatternC.match(file) or FilePatternH.match(file):
                 FindTabsInFile(os.path.join(root, file))
    
####################################################
# main
####################################################
sys.argv.reverse()
sys.argv.pop()
action = None
top = None
while len(sys.argv):
    arg = sys.argv.pop()
    if arg == '--list-error-codes':
        action = ScanErrorCodes
    elif arg == '--list-loggers':
        action = ScanLoggers
    elif arg == '--list-naked-errors':
        action = ScanNakedErrors
    elif arg == '--find-tabs':
        action = FindTabs
    elif top == None:
        top = arg
    else:
        raise "unexpected argument " + arg

if not action or not top:
    print "CodeScanner {--list-error-codes | --list-loggers | --find-tabs} <directory-root>"
    sys.exit(1)

action(top)
    
    
