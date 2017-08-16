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
    /// <summary>
    /// for S-JIS encoding, observe characteristic:
    /// 1, kana character (or hankaku?) often have hight frequency of appereance
    /// 2, kana character often exist in group
    /// 3, certain combination of kana is never used in japanese language
    /// </summary>
    public class SJISProber : CharsetProber
    {
        private CodingStateMachine codingSM;
        private SJISContextAnalyser contextAnalyser;
        private SJISDistributionAnalyser distributionAnalyser;
        private byte[] lastChar = new byte[2];
    
        public SJISProber()
        {
            codingSM = new CodingStateMachine(new SJISSMModel());
            distributionAnalyser = new SJISDistributionAnalyser();
            contextAnalyser = new SJISContextAnalyser(); 
            Reset();
        }
        
        public override string GetCharsetName()
        {
            return "Shift-JIS";        
        }
        
        public override ProbingState HandleData(byte[] buf, int offset, int len)
        {
            int codingState;
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
                    int charLen = codingSM.CurrentCharLen;
                    if (i == offset) {
                        lastChar[1] = buf[offset];
                        contextAnalyser.HandleOneChar(lastChar, 2-charLen, charLen);
                        distributionAnalyser.HandleOneChar(lastChar, 0, charLen);
                    } else {
                        contextAnalyser.HandleOneChar(buf, i+1-charLen, charLen);
                        distributionAnalyser.HandleOneChar(buf, i-1, charLen);
                    }
                }
            } 
            lastChar[0] = buf[max-1];
            if (state == ProbingState.Detecting)
                if (contextAnalyser.GotEnoughData() && GetConfidence() > SHORTCUT_THRESHOLD)
                    state = ProbingState.FoundIt;
            return state;
        }

        public override void Reset()
        {
            codingSM.Reset(); 
            state = ProbingState.Detecting;
            contextAnalyser.Reset();
            distributionAnalyser.Reset();
        }
        
        public override float GetConfidence()
        {
            float contxtCf = contextAnalyser.GetConfidence();
            float distribCf = distributionAnalyser.GetConfidence();
            return (contxtCf > distribCf ? contxtCf : distribCf);
        }
    }
}
