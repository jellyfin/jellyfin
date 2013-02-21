#!/bin/bash

#git remote add platinum gitplut@plutinosoft.com:Platinum
#git subtree add -P ThirdParty/Platinum -m "added Platinum as subproject" platinum/master
git subtree split -P ThirdParty/Platinum -b backport_platinum
git push platinum backport_platinum:master
