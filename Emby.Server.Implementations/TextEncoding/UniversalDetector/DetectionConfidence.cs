/* -*- Mode: C; tab-width: 4; indent-tabs-mode: nil; c-basic-offset: 2 -*- */
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
 * either of the GNU General Public License Version 2 or later (the "GPL"),
 * or the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
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

namespace UniversalDetector
{
    /// <summary>
    /// Indicate how confident the detection module about the return result.
    ///     
    ///  NoAnswerYet: the detector have not find out a answer yet based on 
    ///  the data it received.
    /// 
    ///  BestAnswer: the answer the detector returned is the best one within 
    ///     the knowledge of the detector. In other words, the test to all 
    ///     other candidates fail.
    ///     For example, the (Shift_JIS/EUC-JP/ISO-2022-JP) detection
    ///     module may return this with answer "Shift_JIS " if it receive 
    ///     bytes > 0x80 (which make ISO-2022-JP test failed) and byte 
    ///     0x82 (which may EUC-JP test failed)
    ///
    ///  SureAnswer: the detector is 100% sure about the  answer.
    ///  
    ///  Example 1: the Shift_JIS/ISO-2022-JP/EUC-JP detector return
    ///    this w/ ISO-2022-JP when it hit one of the following ESC seq
    ///     ESC ( J
    ///     ESC $ @
    ///     ESC $ B
    /// 
    ///  Example 2: the detector which can detect UCS2 return w/ UCS2
    ///     when the first 2 byte are BOM mark.
    ///  Example 3: the Korean detector return ISO-2022-KR when it
    ///     hit ESC $ ) C
    /// </summary>
    public enum DetectionConfidence
    {
        NoAnswerYet = 0,
        BestAnswer,
        SureAnswer,
        NoAnswerMatch
    }
}
