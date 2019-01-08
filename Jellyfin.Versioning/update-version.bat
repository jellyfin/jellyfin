@ECHO OFF
goto licenseblock
update-version.bat
Part of the Jellyfin project (https://jellyfin.media)

   All copyright belongs to the Jellyfin contributors; a full list can
   be found in the file CONTRIBUTORS.md

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 2 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program. If not, see <https://www.gnu.org/licenses/>.
:licenseblock

powershell.exe -executionpolicy Bypass -file update-version.ps1