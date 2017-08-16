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

using System;

namespace UniversalDetector.Core
{
    public class SBCSGroupProber : CharsetProber
    {
        private const int PROBERS_NUM = 13;
        private CharsetProber[] probers = new CharsetProber[PROBERS_NUM];        
        private bool[] isActive = new bool[PROBERS_NUM];
        private int bestGuess;
        private int activeNum;
        
        public SBCSGroupProber()
        {
            probers[0] = new SingleByteCharSetProber(new Win1251Model());
            probers[1] = new SingleByteCharSetProber(new Koi8rModel());
            probers[2] = new SingleByteCharSetProber(new Latin5Model());
            probers[3] = new SingleByteCharSetProber(new MacCyrillicModel());
            probers[4] = new SingleByteCharSetProber(new Ibm866Model());
            probers[5] = new SingleByteCharSetProber(new Ibm855Model());
            probers[6] = new SingleByteCharSetProber(new Latin7Model());
            probers[7] = new SingleByteCharSetProber(new Win1253Model());
            probers[8] = new SingleByteCharSetProber(new Latin5BulgarianModel());
            probers[9] = new SingleByteCharSetProber(new Win1251BulgarianModel());
            HebrewProber hebprober = new HebrewProber();
            probers[10] = hebprober;
            // Logical  
            probers[11] = new SingleByteCharSetProber(new Win1255Model(), false, hebprober); 
            // Visual
            probers[12] = new SingleByteCharSetProber(new Win1255Model(), true, hebprober); 
            hebprober.SetModelProbers(probers[11], probers[12]);
            // disable latin2 before latin1 is available, otherwise all latin1 
            // will be detected as latin2 because of their similarity.
            //probers[13] = new SingleByteCharSetProber(new Latin2HungarianModel());
            //probers[14] = new SingleByteCharSetProber(new Win1250HungarianModel());            
            Reset();
        }
  
        public override ProbingState HandleData(byte[] buf, int offset, int len) 
        {
            ProbingState st = ProbingState.NotMe;
            
            //apply filter to original buffer, and we got new buffer back
            //depend on what script it is, we will feed them the new buffer 
            //we got after applying proper filter
            //this is done without any consideration to KeepEnglishLetters
            //of each prober since as of now, there are no probers here which
            //recognize languages with English characters.
            byte[] newBuf = FilterWithoutEnglishLetters(buf, offset, len);
            if (newBuf.Length == 0)
                return state; // Nothing to see here, move on.
            
            for (int i = 0; i < PROBERS_NUM; i++) {
                if (!isActive[i])
                    continue;
                st = probers[i].HandleData(newBuf, 0, newBuf.Length);
                
                if (st == ProbingState.FoundIt) {
                    bestGuess = i;
                    state = ProbingState.FoundIt;
                    break;
                } else if (st == ProbingState.NotMe) {
                    isActive[i] = false;
                    activeNum--;
                    if (activeNum <= 0) {
                        state = ProbingState.NotMe;
                        break;
                    }
                }
            }
            return state;
        }

        public override float GetConfidence()
        {
            float bestConf = 0.0f, cf;
            switch (state) {
            case ProbingState.FoundIt:
                return 0.99f; //sure yes
            case ProbingState.NotMe:
                return 0.01f;  //sure no
            default:
                for (int i = 0; i < PROBERS_NUM; i++)
                {
                    if (!isActive[i])
                        continue;
                    cf = probers[i].GetConfidence();
                    if (bestConf < cf)
                    {
                        bestConf = cf;
                        bestGuess = i;
                    }
                }
                break;
            }
            return bestConf;
        }

        public override void DumpStatus()
        {
            float cf = GetConfidence();
           // Console.WriteLine(" SBCS Group Prober --------begin status");
            for (int i = 0; i < PROBERS_NUM; i++) {
                if (isActive[i])
                    probers[i].DumpStatus();
                //else
                //Console.WriteLine(" inactive: [{0}] (i.e. confidence is too low).", probers[i].GetCharsetName());

            }
            //Console.WriteLine(" SBCS Group found best match [{0}] confidence {1}.",  probers[bestGuess].GetCharsetName(), cf);
        }

        public override void Reset ()
        {
            int activeNum = 0;
            for (int i = 0; i < PROBERS_NUM; i++) {
                if (probers[i] != null) {
                    probers[i].Reset();
                    isActive[i] = true;
                    activeNum++;
                } else {
                    isActive[i] = false;
                }
            }
            bestGuess = -1;
            state = ProbingState.Detecting;
        }

        public override string GetCharsetName()
        {
            //if we have no answer yet
            if (bestGuess == -1) {
                GetConfidence();
                //no charset seems positive
                if (bestGuess == -1)
                    bestGuess = 0;
            }
            return probers[bestGuess].GetCharsetName();
        }

    }
}
