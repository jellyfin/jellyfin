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
 *          Shy Shalom <shooshX@gmail.com>
 *          Kohei TAKETA <k-tak@void.in> (Java port)
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
    /// Parallel state machine for the Coding Scheme Method
    /// </summary>
    public class CodingStateMachine
    {
        private int currentState;
        private SMModel model;
        private int currentCharLen;
        private int currentBytePos;
        
        public CodingStateMachine(SMModel model) 
        {
            this.currentState = SMModel.START;
            this.model = model;
        }

        public int NextState(byte b)
        {
            // for each byte we get its class, if it is first byte, 
            // we also get byte length
            int byteCls = model.GetClass(b);
            if (currentState == SMModel.START) { 
                currentBytePos = 0;
                currentCharLen = model.charLenTable[byteCls];
            }
            
            // from byte's class and stateTable, we get its next state            
            currentState = model.stateTable.Unpack(
                currentState * model.ClassFactor + byteCls);
            currentBytePos++;
            return currentState;
        }
  
        public void Reset() 
        { 
            currentState = SMModel.START; 
        }

        public int CurrentCharLen 
        { 
            get { return currentCharLen; } 
        }

        public string ModelName 
        { 
            get { return model.Name; } 
        }
    }
}
