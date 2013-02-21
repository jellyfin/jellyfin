#!/bin/bash

#git remote add platinum gitplut@plutinosoft.com:Platinum
#git subtree add -P ThirdParty/Platinum -m "added Platinum as subproject" platinum/master
git fetch platinum
git subtree merge -P ThirdParty/Platinum -m "merged Platinum changes" platinum/master

