// Jellyfin.Versioning/AssemblyExtendedVersion.cs
// Part of the Jellyfin project (https://jellyfin.media)
//
//    All copyright belongs to the Jellyfin contributors; a full list can
//    be found in the file CONTRIBUTORS.md
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Jellyfin.Versioning
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class AssemblyExtendedVersion : Attribute
    {
        public ExtendedVersion ExtendedVersion { get; }

        public AssemblyExtendedVersion(ExtendedVersion ExtendedVersion)
        {
            this.ExtendedVersion = ExtendedVersion;
        }
        
        public AssemblyExtendedVersion(string apiVersion, bool readResource = true)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Versioning.jellyfin_version.ini";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                ExtendedVersion = new ExtendedVersion(new Version(apiVersion), stream);
            }
        }
    }
}
