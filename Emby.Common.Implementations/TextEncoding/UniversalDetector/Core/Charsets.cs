/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is mozilla.org code.
 *
 * The Initial Developer of the Original Code is
 * Netscape Communications Corporation.
 * Portions created by the Initial Developer are Copyright (C) 1998
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *          Rudi Pettazzi <rudi.pettazzi@gmail.com> (C# port)
 * 
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */

namespace UniversalDetector.Core
{
    public static class Charsets
    {
        public const string ASCII = "ASCII";
        
        public const string UTF8 = "UTF-8";
        
        public const string UTF16_LE = "UTF-16LE";
        
        public const string UTF16_BE = "UTF-16BE";
        
        public const string UTF32_BE = "UTF-32BE";
        
        public const string UTF32_LE = "UTF-32LE";

        /// <summary>
        /// Unusual BOM (3412 order)
        /// </summary>
        public const string UCS4_3412 = "X-ISO-10646-UCS-4-3412";
        
        /// <summary>
        /// Unusual BOM (2413 order)
        /// </summary>
        public const string UCS4_2413 = "X-ISO-10646-UCS-4-2413";
       
        /// <summary>
        /// Cyrillic (based on bulgarian and russian data)
        /// </summary>
        public const string WIN1251 = "windows-1251";
        
        /// <summary>
        /// Latin-1, almost identical to ISO-8859-1
        /// </summary>
        public const string WIN1252 = "windows-1252";
        
        /// <summary>
        /// Greek
        /// </summary>
        public const string WIN1253 = "windows-1253";
        
        /// <summary>
        /// Logical hebrew (includes ISO-8859-8-I and most of x-mac-hebrew)
        /// </summary>
        public const string WIN1255 = "windows-1255";
        
        /// <summary>
        /// Traditional chinese
        /// </summary>
        public const string BIG5 = "Big-5";

        public const string EUCKR = "EUC-KR";

        public const string EUCJP = "EUC-JP";
        
        public const string EUCTW = "EUC-TW";

        /// <summary>
        /// Note: gb2312 is a subset of gb18030
        /// </summary>
        public const string GB18030 = "gb18030";

        public const string ISO2022_JP = "ISO-2022-JP";
        
        public const string ISO2022_CN = "ISO-2022-CN";
        
        public const string ISO2022_KR = "ISO-2022-KR";
        
        /// <summary>
        /// Simplified chinese
        /// </summary>
        public const string HZ_GB_2312 = "HZ-GB-2312";

        public const string SHIFT_JIS = "Shift-JIS";

        public const string MAC_CYRILLIC = "x-mac-cyrillic";
        
        public const string KOI8R = "KOI8-R";
        
        public const string IBM855 = "IBM855";
        
        public const string IBM866 = "IBM866";

        /// <summary>
        /// East-Europe. Disabled because too similar to windows-1252 
        /// (latin-1). Should use tri-grams models to discriminate between
        /// these two charsets.
        /// </summary>
        public const string ISO8859_2 = "ISO-8859-2";

        /// <summary>
        /// Cyrillic
        /// </summary>
        public const string ISO8859_5 = "ISO-8859-5";

        /// <summary>
        /// Greek
        /// </summary>
        public const string ISO_8859_7 = "ISO-8859-7";

        /// <summary>
        /// Visual Hebrew
        /// </summary>
        public const string ISO8859_8 = "ISO-8859-8";

        /// <summary>
        /// Thai. This recognizer is not enabled yet. 
        /// </summary>
        public const string TIS620 = "TIS620";
        
    }
}
