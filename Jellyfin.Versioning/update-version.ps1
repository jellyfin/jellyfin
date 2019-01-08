# Jellyfin.Versioning/update-version.ps1
# Part of the Jellyfin project (https://jellyfin.media)
#
#    All copyright belongs to the Jellyfin contributors; a full list can
#    be found in the file CONTRIBUTORS.md
#
#    This program is free software: you can redistribute it and/or modify
#    it under the terms of the GNU General Public License as published by
#    the Free Software Foundation, either version 2 of the License, or
#    (at your option) any later version.
#
#    This program is distributed in the hope that it will be useful,
#    but WITHOUT ANY WARRANTY; without even the implied warranty of
#    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#    GNU General Public License for more details.
#
#    You should have received a copy of the GNU General Public License
#    along with this program. If not, see <https://www.gnu.org/licenses/>.

if(Test-Path -Path '..\.git' ){
   $commit = (git rev-parse HEAD)
   $count = (git rev-list HEAD --count)
   $branch = (git rev-parse --abbrev-ref HEAD)
   $desc = (git describe --tags --always --long)
   $remote = (git config --get remote.origin.url) 
   Set-Content -Path "jellyfin_version.ini" -Value "commit=$commit`r`nrevision=$count`r`nbranch=$branch`r`ntagdesc=$desc`r`nremote=$remote"
   Write-Host Updated build version in jellyfin_version.ini
   Write-Host "commit=$commit`r`nrevision=$count`r`nbranch=$branch`r`ntagdesc=$desc`r`nremote=$remote`r`n"
} else { 
   Write-Host Did not update build version because there was no .git directory.
}