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
    public class EscCharsetProber : CharsetProber
    {
        private const int CHARSETS_NUM = 4;
        private string detectedCharset;
        private CodingStateMachine[] codingSM; 
        int activeSM;

        public EscCharsetProber()
        {
            codingSM = new CodingStateMachine[CHARSETS_NUM]; 
            codingSM[0] = new CodingStateMachine(new HZSMModel());
            codingSM[1] = new CodingStateMachine(new ISO2022CNSMModel());
            codingSM[2] = new CodingStateMachine(new ISO2022JPSMModel());
            codingSM[3] = new CodingStateMachine(new ISO2022KRSMModel());
            Reset();
        }
        
        public override void Reset()
        {
            state = ProbingState.Detecting;
            for (int i = 0; i < CHARSETS_NUM; i++)
                codingSM[i].Reset();
            activeSM = CHARSETS_NUM;
            detectedCharset = null;
        }

        public override ProbingState HandleData(byte[] buf, int offset, int len)
        {
            int max = offset + len;
            
            for (int i = offset; i < max && state == ProbingState.Detecting; i++) {
                for (int j = activeSM - 1; j >= 0; j--) {
                    // byte is feed to all active state machine
                    int codingState = codingSM[j].NextState(buf[i]);
                    if (codingState == SMModel.ERROR)  {
                        // got negative answer for this state machine, make it inactive
                        activeSM--;
                        if (activeSM == 0) {
                            state = ProbingState.NotMe;
                            return state;
                        } else if (j != activeSM) {
                            CodingStateMachine t = codingSM[activeSM];
                            codingSM[activeSM] = codingSM[j];
                            codingSM[j] = t;
                        }
                    } else if (codingState == SMModel.ITSME) {
                        state = ProbingState.FoundIt;
                        detectedCharset = codingSM[j].ModelName;
                        return state;
                    }
                }
            }
            return state;
        }

        public override string GetCharsetName()
        {
            return detectedCharset;        
        }
        
        public override float GetConfidence()
        {
            return 0.99f;
        }           
    }
}
