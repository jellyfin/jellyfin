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

    public class SingleByteCharSetProber : CharsetProber
    {
        private const int SAMPLE_SIZE = 64;
        private const int SB_ENOUGH_REL_THRESHOLD = 1024;
        private const float POSITIVE_SHORTCUT_THRESHOLD = 0.95f;
        private const float NEGATIVE_SHORTCUT_THRESHOLD = 0.05f;
        private const int SYMBOL_CAT_ORDER = 250;
        private const int NUMBER_OF_SEQ_CAT = 4;
        private const int POSITIVE_CAT = NUMBER_OF_SEQ_CAT-1;
        private const int NEGATIVE_CAT = 0;
        
        protected SequenceModel model;
        
        // true if we need to reverse every pair in the model lookup        
        bool reversed; 

        // char order of last character
        byte lastOrder;

        int totalSeqs;
        int totalChar;
        int[] seqCounters = new int[NUMBER_OF_SEQ_CAT];
        
        // characters that fall in our sampling range
        int freqChar;
  
        // Optional auxiliary prober for name decision. created and destroyed by the GroupProber
        CharsetProber nameProber; 
                    
        public SingleByteCharSetProber(SequenceModel model) 
            : this(model, false, null)
        {
            
        }
    
        public SingleByteCharSetProber(SequenceModel model, bool reversed, 
                                       CharsetProber nameProber)
        {
            this.model = model;
            this.reversed = reversed;
            this.nameProber = nameProber;
            Reset();            
        }

        public override ProbingState HandleData(byte[] buf, int offset, int len)
        {
            int max = offset + len;
            
            for (int i = offset; i < max; i++) {
                byte order = model.GetOrder(buf[i]);

                if (order < SYMBOL_CAT_ORDER)
                    totalChar++;
                    
                if (order < SAMPLE_SIZE) {
                    freqChar++;

                    if (lastOrder < SAMPLE_SIZE) {
                        totalSeqs++;
                        if (!reversed)
                            ++(seqCounters[model.GetPrecedence(lastOrder*SAMPLE_SIZE+order)]);
                        else // reverse the order of the letters in the lookup
                            ++(seqCounters[model.GetPrecedence(order*SAMPLE_SIZE+lastOrder)]);
                    }
                }
                lastOrder = order;
            }

            if (state == ProbingState.Detecting) {
                if (totalSeqs > SB_ENOUGH_REL_THRESHOLD) {
                    float cf = GetConfidence();
                    if (cf > POSITIVE_SHORTCUT_THRESHOLD)
                        state = ProbingState.FoundIt;
                    else if (cf < NEGATIVE_SHORTCUT_THRESHOLD)
                        state = ProbingState.NotMe;
                }
            }
            return state;
        }
                
        public override void DumpStatus()
        {
            //Console.WriteLine("  SBCS: {0} [{1}]", GetConfidence(), GetCharsetName());
        }

        public override float GetConfidence()
        {
            /*
            NEGATIVE_APPROACH
            if (totalSeqs > 0) {
                if (totalSeqs > seqCounters[NEGATIVE_CAT] * 10)
                    return (totalSeqs - seqCounters[NEGATIVE_CAT] * 10)/totalSeqs * freqChar / mTotalChar;
            }
            return 0.01f;
            */
            // POSITIVE_APPROACH
            float r = 0.0f;

            if (totalSeqs > 0) {
                r = 1.0f * seqCounters[POSITIVE_CAT] / totalSeqs / model.TypicalPositiveRatio;
                r = r * freqChar / totalChar;
                if (r >= 1.0f)
                    r = 0.99f;
                return r;
            }
            return 0.01f;            
        }
        
        public override void Reset()
        {
            state = ProbingState.Detecting;
            lastOrder = 255;
            for (int i = 0; i < NUMBER_OF_SEQ_CAT; i++)
                seqCounters[i] = 0;
            totalSeqs = 0;
            totalChar = 0;
            freqChar = 0;
        }
        
        public override string GetCharsetName() 
        {
            return (nameProber == null) ? model.CharsetName
                                        : nameProber.GetCharsetName();
        }
        
    }
}
