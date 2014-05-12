OpenSubtitlesHandler
====================
This project is for OpenSubtitles.org integration‏. The point is to allow user to access OpenSubtitles.org database directly
within ASM without the need to open internet browser.
The plan: Implement the "OSDb protocol" http://trac.opensubtitles.org/projects/opensubtitles/wiki/OSDb

Copyright:
=========
This library ann all its content are written by Ala Ibrahim Hadid.
Copyright © Ala Ibrahim Hadid 2013
mailto:ahdsoftwares@hotmail.com

Resources:
==========
* GetHash.dll: this dll is used to compute hash for movie. 
  For more information please visit http://trac.opensubtitles.org/projects/opensubtitles/wiki/HashSourceCodes#C2

XML_RPC:
========
This class is created to generate XML-RPC requests as XML String. All you need is to call XML_RPC.Generate() method.