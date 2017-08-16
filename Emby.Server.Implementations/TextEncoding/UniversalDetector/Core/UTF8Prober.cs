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
    public class UTF8Prober : CharsetProber
    {
        private static float ONE_CHAR_PROB = 0.50f;
        private CodingStateMachine codingSM;
        private int numOfMBChar;

        public UTF8Prober()
        {
            numOfMBChar = 0; 
            codingSM = new CodingStateMachine(new UTF8SMModel());
            Reset();
        }
        
        public override string GetCharsetName() {
            return "UTF-8";
        }

        public override void Reset()
        {
            codingSM.Reset();
            numOfMBChar = 0;
            state = ProbingState.Detecting;
        }

        public override ProbingState HandleData(byte[] buf, int offset, int len)
        {
            int codingState = SMModel.START;
            int max = offset + len;
            
            for (int i = offset; i < max; i++) {

                codingState = codingSM.NextState(buf[i]);

                if (codingState == SMModel.ERROR) {
                    state = ProbingState.NotMe;
                    break;
                }

                if (codingState == SMModel.ITSME) {
                    state = ProbingState.FoundIt;
                    break;
                }

                if (codingState == SMModel.START) {
                    if (codingSM.CurrentCharLen >= 2)
                        numOfMBChar++;
                }
            }

            if (state == ProbingState.Detecting)
                if (GetConfidence() > SHORTCUT_THRESHOLD)
                    state = ProbingState.FoundIt;
            return state;
        }

        public override float GetConfidence()
        {
            float unlike = 0.99f;
            float confidence = 0.0f;
            
            if (numOfMBChar < 6) {
                for (int i = 0; i < numOfMBChar; i++)
                    unlike *= ONE_CHAR_PROB;
                confidence = 1.0f - unlike;
            } else {
                confidence = 0.99f;
            }
            return confidence;

        }
    }
}
