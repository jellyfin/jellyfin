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
 * The Original Code is Mozilla Universal charset detector code.
 *
 * The Initial Developer of the Original Code is
 * Netscape Communications Corporation.
 * Portions created by the Initial Developer are Copyright (C) 2001
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *          Shy Shalom <shooshX@gmail.com>
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
    public class UTF8SMModel : SMModel
    {
        private readonly static int[] UTF8_cls = {
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 00 - 07
            BitPackage.Pack4bits(1,1,1,1,1,1,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 10 - 17 
            BitPackage.Pack4bits(1,1,1,0,1,1,1,1),  // 18 - 1f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 20 - 27 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 28 - 2f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 30 - 37 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 38 - 3f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 40 - 47 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 48 - 4f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 50 - 57 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 58 - 5f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 60 - 67 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 68 - 6f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 70 - 77 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 78 - 7f 
            BitPackage.Pack4bits(2,2,2,2,3,3,3,3),  // 80 - 87 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 88 - 8f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 90 - 97 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 98 - 9f 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // a0 - a7 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // a8 - af 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // b0 - b7 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // b8 - bf 
            BitPackage.Pack4bits(0,0,6,6,6,6,6,6),  // c0 - c7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // c8 - cf 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // d0 - d7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // d8 - df 
            BitPackage.Pack4bits(7,8,8,8,8,8,8,8),  // e0 - e7 
            BitPackage.Pack4bits(8,8,8,8,8,9,8,8),  // e8 - ef 
            BitPackage.Pack4bits(10,11,11,11,11,11,11,11),  // f0 - f7 
            BitPackage.Pack4bits(12,13,13,13,14,15,0,0)   // f8 - ff 
        };

        private readonly static int[] UTF8_st = {
            BitPackage.Pack4bits(ERROR,START,ERROR,ERROR,ERROR,ERROR,   12,   10),//00-07 
            BitPackage.Pack4bits(    9,   11,    8,    7,    6,    5,    4,    3),//08-0f 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//10-17 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//18-1f 
            BitPackage.Pack4bits(ITSME,ITSME,ITSME,ITSME,ITSME,ITSME,ITSME,ITSME),//20-27 
            BitPackage.Pack4bits(ITSME,ITSME,ITSME,ITSME,ITSME,ITSME,ITSME,ITSME),//28-2f 
            BitPackage.Pack4bits(ERROR,ERROR,    5,    5,    5,    5,ERROR,ERROR),//30-37 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//38-3f 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,    5,    5,    5,ERROR,ERROR),//40-47 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//48-4f 
            BitPackage.Pack4bits(ERROR,ERROR,    7,    7,    7,    7,ERROR,ERROR),//50-57 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//58-5f 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,    7,    7,ERROR,ERROR),//60-67 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//68-6f 
            BitPackage.Pack4bits(ERROR,ERROR,    9,    9,    9,    9,ERROR,ERROR),//70-77 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//78-7f 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,    9,ERROR,ERROR),//80-87 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//88-8f 
            BitPackage.Pack4bits(ERROR,ERROR,   12,   12,   12,   12,ERROR,ERROR),//90-97 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//98-9f 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,   12,ERROR,ERROR),//a0-a7 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//a8-af 
            BitPackage.Pack4bits(ERROR,ERROR,   12,   12,   12,ERROR,ERROR,ERROR),//b0-b7 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR),//b8-bf 
            BitPackage.Pack4bits(ERROR,ERROR,START,START,START,START,ERROR,ERROR),//c0-c7 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ERROR) //c8-cf  
        };

        private readonly static int[] UTF8CharLenTable = 
            {0, 1, 0, 0, 0, 0, 2, 3, 3, 3, 4, 4, 5, 5, 6, 6 };
        
        public UTF8SMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, UTF8_cls),
                         16,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, UTF8_st),
              UTF8CharLenTable, "UTF-8")
        {

        }
    }
    
    public class GB18030SMModel : SMModel
    {
        private readonly static int[] GB18030_cls = {
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 00 - 07 
            BitPackage.Pack4bits(1,1,1,1,1,1,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 10 - 17 
            BitPackage.Pack4bits(1,1,1,0,1,1,1,1),  // 18 - 1f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 20 - 27 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 28 - 2f 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // 30 - 37 
            BitPackage.Pack4bits(3,3,1,1,1,1,1,1),  // 38 - 3f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 40 - 47 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 48 - 4f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 50 - 57 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 58 - 5f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 60 - 67 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 68 - 6f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 70 - 77 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,4),  // 78 - 7f 
            BitPackage.Pack4bits(5,6,6,6,6,6,6,6),  // 80 - 87 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // 88 - 8f 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // 90 - 97 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // 98 - 9f 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // a0 - a7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // a8 - af 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // b0 - b7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // b8 - bf 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // c0 - c7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // c8 - cf 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // d0 - d7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // d8 - df 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // e0 - e7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // e8 - ef 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,6),  // f0 - f7 
            BitPackage.Pack4bits(6,6,6,6,6,6,6,0)   // f8 - ff 
        };

        private readonly static int[] GB18030_st = {
            BitPackage.Pack4bits(ERROR,START,START,START,START,START,    3,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ITSME,ITSME),//08-0f 
            BitPackage.Pack4bits(ITSME,ITSME,ITSME,ITSME,ITSME,ERROR,ERROR,START),//10-17 
            BitPackage.Pack4bits(    4,ERROR,START,START,ERROR,ERROR,ERROR,ERROR),//18-1f 
            BitPackage.Pack4bits(ERROR,ERROR,    5,ERROR,ERROR,ERROR,ITSME,ERROR),//20-27 
            BitPackage.Pack4bits(ERROR,ERROR,START,START,START,START,START,START) //28-2f 
        };

        // To be accurate, the length of class 6 can be either 2 or 4. 
        // But it is not necessary to discriminate between the two since 
        // it is used for frequency analysis only, and we are validating 
        // each code range there as well. So it is safe to set it to be 
        // 2 here. 
        private readonly static int[] GB18030CharLenTable = {0, 1, 1, 1, 1, 1, 2};
        
        public GB18030SMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, GB18030_cls),
                         7,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, GB18030_st),
              GB18030CharLenTable, "GB18030")
        {

        }
    }
    
    public class BIG5SMModel : SMModel
    {
        private readonly static int[] BIG5_cls = {
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 00 - 07
            BitPackage.Pack4bits(1,1,1,1,1,1,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 10 - 17 
            BitPackage.Pack4bits(1,1,1,0,1,1,1,1),  // 18 - 1f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 20 - 27 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 28 - 2f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 30 - 37 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 38 - 3f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 40 - 47 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 48 - 4f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 50 - 57 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 58 - 5f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 60 - 67 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 68 - 6f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 70 - 77 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,1),  // 78 - 7f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 80 - 87 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 88 - 8f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 90 - 97 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 98 - 9f 
            BitPackage.Pack4bits(4,3,3,3,3,3,3,3),  // a0 - a7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // a8 - af 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // b0 - b7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // b8 - bf 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // c0 - c7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // c8 - cf 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // d0 - d7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // d8 - df 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // e0 - e7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // e8 - ef 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // f0 - f7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,0)   // f8 - ff 
        };

        private readonly static int[] BIG5_st = {
            BitPackage.Pack4bits(ERROR,START,START,    3,ERROR,ERROR,ERROR,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ITSME,ITSME,ITSME,ITSME,ITSME,ERROR),//08-0f 
            BitPackage.Pack4bits(ERROR,START,START,START,START,START,START,START) //10-17 
        };

        private readonly static int[] BIG5CharLenTable = {0, 1, 1, 2, 0};
        
        public BIG5SMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, BIG5_cls),
                         5,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, BIG5_st),
              BIG5CharLenTable, "Big5")
        {

        }
    }
    
    public class EUCJPSMModel : SMModel
    {
        private readonly static int[] EUCJP_cls = {
            //BitPacket.Pack4bits(5,4,4,4,4,4,4,4),  // 00 - 07 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 00 - 07 
            BitPackage.Pack4bits(4,4,4,4,4,4,5,5),  // 08 - 0f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 10 - 17 
            BitPackage.Pack4bits(4,4,4,5,4,4,4,4),  // 18 - 1f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 20 - 27 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 28 - 2f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 30 - 37 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 38 - 3f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 40 - 47 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 48 - 4f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 50 - 57 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 58 - 5f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 60 - 67 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 68 - 6f 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 70 - 77 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // 78 - 7f 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // 80 - 87 
            BitPackage.Pack4bits(5,5,5,5,5,5,1,3),  // 88 - 8f 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // 90 - 97 
            BitPackage.Pack4bits(5,5,5,5,5,5,5,5),  // 98 - 9f 
            BitPackage.Pack4bits(5,2,2,2,2,2,2,2),  // a0 - a7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // a8 - af 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // b0 - b7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // b8 - bf 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // c0 - c7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // c8 - cf 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // d0 - d7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // d8 - df 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // e0 - e7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // e8 - ef 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // f0 - f7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,5)   // f8 - ff 
        };

        private readonly static int[] EUCJP_st = {
            BitPackage.Pack4bits(    3,    4,    3,    5,START,ERROR,ERROR,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ITSME,ITSME,ITSME,ITSME),//08-0f 
            BitPackage.Pack4bits(ITSME,ITSME,START,ERROR,START,ERROR,ERROR,ERROR),//10-17 
            BitPackage.Pack4bits(ERROR,ERROR,START,ERROR,ERROR,ERROR,    3,ERROR),//18-1f 
            BitPackage.Pack4bits(    3,ERROR,ERROR,ERROR,START,START,START,START) //20-27 
        };

        private readonly static int[] EUCJPCharLenTable = { 2, 2, 2, 3, 1, 0 };
        
        public EUCJPSMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, EUCJP_cls),
                         6,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, EUCJP_st),
              EUCJPCharLenTable, "EUC-JP")
        {

        }
    }
    
    public class EUCKRSMModel : SMModel
    {
        private readonly static int[] EUCKR_cls = {
            //BitPacket.Pack4bits(0,1,1,1,1,1,1,1),  // 00 - 07 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 00 - 07 
            BitPackage.Pack4bits(1,1,1,1,1,1,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 10 - 17 
            BitPackage.Pack4bits(1,1,1,0,1,1,1,1),  // 18 - 1f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 20 - 27 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 28 - 2f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 30 - 37 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 38 - 3f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 40 - 47 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 48 - 4f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 50 - 57 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 58 - 5f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 60 - 67 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 68 - 6f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 70 - 77 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 78 - 7f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 80 - 87 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 88 - 8f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 90 - 97 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 98 - 9f 
            BitPackage.Pack4bits(0,2,2,2,2,2,2,2),  // a0 - a7 
            BitPackage.Pack4bits(2,2,2,2,2,3,3,3),  // a8 - af 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // b0 - b7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // b8 - bf 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // c0 - c7 
            BitPackage.Pack4bits(2,3,2,2,2,2,2,2),  // c8 - cf 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // d0 - d7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // d8 - df 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // e0 - e7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // e8 - ef 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // f0 - f7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,0)   // f8 - ff 
        };

        private readonly static int[] EUCKR_st = {
            BitPackage.Pack4bits(ERROR,START,    3,ERROR,ERROR,ERROR,ERROR,ERROR),//00-07 
            BitPackage.Pack4bits(ITSME,ITSME,ITSME,ITSME,ERROR,ERROR,START,START) //08-0f 
        };

        private readonly static int[] EUCKRCharLenTable = { 0, 1, 2, 0 };
        
        public EUCKRSMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, EUCKR_cls),
                         4,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, EUCKR_st),
              EUCKRCharLenTable, "EUC-KR")
        {

        }
    }
    
    public class EUCTWSMModel : SMModel
    {
        private readonly static int[] EUCTW_cls = {
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 00 - 07 
            BitPackage.Pack4bits(2,2,2,2,2,2,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 10 - 17 
            BitPackage.Pack4bits(2,2,2,0,2,2,2,2),  // 18 - 1f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 20 - 27 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 28 - 2f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 30 - 37 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 38 - 3f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 40 - 47 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 48 - 4f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 50 - 57 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 58 - 5f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 60 - 67 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 68 - 6f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 70 - 77 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 78 - 7f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 80 - 87 
            BitPackage.Pack4bits(0,0,0,0,0,0,6,0),  // 88 - 8f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 90 - 97 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 98 - 9f 
            BitPackage.Pack4bits(0,3,4,4,4,4,4,4),  // a0 - a7 
            BitPackage.Pack4bits(5,5,1,1,1,1,1,1),  // a8 - af 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // b0 - b7 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // b8 - bf 
            BitPackage.Pack4bits(1,1,3,1,3,3,3,3),  // c0 - c7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // c8 - cf 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // d0 - d7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // d8 - df 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // e0 - e7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // e8 - ef 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // f0 - f7 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,0)   // f8 - ff 
        };

        private readonly static int[] EUCTW_st = {
            BitPackage.Pack4bits(ERROR,ERROR,START,    3,    3,    3,    4,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ERROR,ERROR,ITSME,ITSME),//08-0f 
            BitPackage.Pack4bits(ITSME,ITSME,ITSME,ITSME,ITSME,ERROR,START,ERROR),//10-17 
            BitPackage.Pack4bits(START,START,START,ERROR,ERROR,ERROR,ERROR,ERROR),//18-1f 
            BitPackage.Pack4bits(    5,ERROR,ERROR,ERROR,START,ERROR,START,START),//20-27 
            BitPackage.Pack4bits(START,ERROR,START,START,START,START,START,START) //28-2f 
        };

        private readonly static int[] EUCTWCharLenTable = { 0, 0, 1, 2, 2, 2, 3 };
        
        public EUCTWSMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, EUCTW_cls),
                         7,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, EUCTW_st),
              EUCTWCharLenTable, "EUC-TW")
        {

        }
    }    
    
    public class SJISSMModel : SMModel
    {
        private readonly static int[] SJIS_cls = {
            //BitPacket.Pack4bits(0,1,1,1,1,1,1,1),  // 00 - 07 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 00 - 07 
            BitPackage.Pack4bits(1,1,1,1,1,1,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 10 - 17 
            BitPackage.Pack4bits(1,1,1,0,1,1,1,1),  // 18 - 1f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 20 - 27 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 28 - 2f 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 30 - 37 
            BitPackage.Pack4bits(1,1,1,1,1,1,1,1),  // 38 - 3f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 40 - 47 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 48 - 4f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 50 - 57 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 58 - 5f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 60 - 67 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 68 - 6f 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // 70 - 77 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,1),  // 78 - 7f 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // 80 - 87 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // 88 - 8f 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // 90 - 97 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // 98 - 9f 
            //0xa0 is illegal in sjis encoding, but some pages does 
            //contain such byte. We need to be more error forgiven.
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // a0 - a7     
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // a8 - af 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // b0 - b7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // b8 - bf 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // c0 - c7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // c8 - cf 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // d0 - d7 
            BitPackage.Pack4bits(2,2,2,2,2,2,2,2),  // d8 - df 
            BitPackage.Pack4bits(3,3,3,3,3,3,3,3),  // e0 - e7 
            BitPackage.Pack4bits(3,3,3,3,3,4,4,4),  // e8 - ef 
            BitPackage.Pack4bits(4,4,4,4,4,4,4,4),  // f0 - f7 
            BitPackage.Pack4bits(4,4,4,4,4,0,0,0)   // f8 - ff 
        };

        private readonly static int[] SJIS_st = {
            BitPackage.Pack4bits(ERROR,START,START,    3,ERROR,ERROR,ERROR,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ITSME,ITSME,ITSME,ITSME),//08-0f 
            BitPackage.Pack4bits(ITSME,ITSME,ERROR,ERROR,START,START,START,START) //10-17        
        };

        private readonly static int[] SJISCharLenTable = { 0, 1, 1, 2, 0, 0 };
        
        public SJISSMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, SJIS_cls),
                         6,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, SJIS_st),
              SJISCharLenTable, "Shift_JIS")
        {

        }
    }
    
    public class UCS2BESMModel : SMModel
    {
        private readonly static int[] UCS2BE_cls = {
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 00 - 07 
            BitPackage.Pack4bits(0,0,1,0,0,2,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 10 - 17 
            BitPackage.Pack4bits(0,0,0,3,0,0,0,0),  // 18 - 1f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 20 - 27 
            BitPackage.Pack4bits(0,3,3,3,3,3,0,0),  // 28 - 2f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 30 - 37 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 38 - 3f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 40 - 47 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 48 - 4f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 50 - 57 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 58 - 5f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 60 - 67 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 68 - 6f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 70 - 77 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 78 - 7f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 80 - 87 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 88 - 8f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 90 - 97 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 98 - 9f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // a0 - a7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // a8 - af 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // b0 - b7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // b8 - bf 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // c0 - c7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // c8 - cf 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // d0 - d7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // d8 - df 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // e0 - e7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // e8 - ef 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // f0 - f7 
            BitPackage.Pack4bits(0,0,0,0,0,0,4,5)   // f8 - ff 
        };

        private readonly static int[] UCS2BE_st = {
            BitPackage.Pack4bits(    5,    7,    7,ERROR,    4,    3,ERROR,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ITSME,ITSME,ITSME,ITSME),//08-0f 
            BitPackage.Pack4bits(ITSME,ITSME,    6,    6,    6,    6,ERROR,ERROR),//10-17 
            BitPackage.Pack4bits(    6,    6,    6,    6,    6,ITSME,    6,    6),//18-1f 
            BitPackage.Pack4bits(    6,    6,    6,    6,    5,    7,    7,ERROR),//20-27 
            BitPackage.Pack4bits(    5,    8,    6,    6,ERROR,    6,    6,    6),//28-2f 
            BitPackage.Pack4bits(    6,    6,    6,    6,ERROR,ERROR,START,START) //30-37 
        };

        private readonly static int[] UCS2BECharLenTable = { 2, 2, 2, 0, 2, 2 };
        
        public UCS2BESMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, UCS2BE_cls),
                         6,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, UCS2BE_st),
              UCS2BECharLenTable, "UTF-16BE")
        {

        }
    }
    
    public class UCS2LESMModel : SMModel
    {
        private readonly static int[] UCS2LE_cls = {
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 00 - 07 
            BitPackage.Pack4bits(0,0,1,0,0,2,0,0),  // 08 - 0f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 10 - 17 
            BitPackage.Pack4bits(0,0,0,3,0,0,0,0),  // 18 - 1f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 20 - 27 
            BitPackage.Pack4bits(0,3,3,3,3,3,0,0),  // 28 - 2f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 30 - 37 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 38 - 3f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 40 - 47 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 48 - 4f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 50 - 57 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 58 - 5f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 60 - 67 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 68 - 6f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 70 - 77 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 78 - 7f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 80 - 87 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 88 - 8f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 90 - 97 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // 98 - 9f 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // a0 - a7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // a8 - af 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // b0 - b7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // b8 - bf 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // c0 - c7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // c8 - cf 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // d0 - d7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // d8 - df 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // e0 - e7 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // e8 - ef 
            BitPackage.Pack4bits(0,0,0,0,0,0,0,0),  // f0 - f7 
            BitPackage.Pack4bits(0,0,0,0,0,0,4,5)   // f8 - ff 
        };

        private readonly static int[] UCS2LE_st = {
            BitPackage.Pack4bits(    6,    6,    7,    6,    4,    3,ERROR,ERROR),//00-07 
            BitPackage.Pack4bits(ERROR,ERROR,ERROR,ERROR,ITSME,ITSME,ITSME,ITSME),//08-0f 
            BitPackage.Pack4bits(ITSME,ITSME,    5,    5,    5,ERROR,ITSME,ERROR),//10-17 
            BitPackage.Pack4bits(    5,    5,    5,ERROR,    5,ERROR,    6,    6),//18-1f 
            BitPackage.Pack4bits(    7,    6,    8,    8,    5,    5,    5,ERROR),//20-27 
            BitPackage.Pack4bits(    5,    5,    5,ERROR,ERROR,ERROR,    5,    5),//28-2f 
            BitPackage.Pack4bits(    5,    5,    5,ERROR,    5,ERROR,START,START) //30-37 
        };

        private readonly static int[] UCS2LECharLenTable = { 2, 2, 2, 2, 2, 2 };
        
        public UCS2LESMModel() : base(
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, UCS2LE_cls),
                         6,
              new BitPackage(BitPackage.INDEX_SHIFT_4BITS, 
                         BitPackage.SHIFT_MASK_4BITS, 
                         BitPackage.BIT_SHIFT_4BITS,
                         BitPackage.UNIT_MASK_4BITS, UCS2LE_st),
              UCS2LECharLenTable, "UTF-16LE")
        {

        }
    }
    
    
}
