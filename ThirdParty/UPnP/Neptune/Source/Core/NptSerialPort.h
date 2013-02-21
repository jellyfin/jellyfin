/*****************************************************************
|
|   Neptune - Serial Ports
|
| Copyright (c) 2002-2008, Axiomatic Systems, LLC.
| All rights reserved.
|
| Redistribution and use in source and binary forms, with or without
| modification, are permitted provided that the following conditions are met:
|     * Redistributions of source code must retain the above copyright
|       notice, this list of conditions and the following disclaimer.
|     * Redistributions in binary form must reproduce the above copyright
|       notice, this list of conditions and the following disclaimer in the
|       documentation and/or other materials provided with the distribution.
|     * Neither the name of Axiomatic Systems nor the
|       names of its contributors may be used to endorse or promote products
|       derived from this software without specific prior written permission.
|
| THIS SOFTWARE IS PROVIDED BY AXIOMATIC SYSTEMS ''AS IS'' AND ANY
| EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
| WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
| DISCLAIMED. IN NO EVENT SHALL AXIOMATIC SYSTEMS BE LIABLE FOR ANY
| DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
| (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
| ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
| (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
| SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
|
 ****************************************************************/

#ifndef _NPT_SERIAL_PORT_H_
#define _NPT_SERIAL_PORT_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptStreams.h"

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const int NPT_ERROR_NO_SUCH_SERIAL_PORT      = NPT_ERROR_BASE_SERIAL_PORT - 0;
const int NPT_ERROR_SERIAL_PORT_NOT_OPEN     = NPT_ERROR_BASE_SERIAL_PORT - 1;
const int NPT_ERROR_SERIAL_PORT_ALREADY_OPEN = NPT_ERROR_BASE_SERIAL_PORT - 2;
const int NPT_ERROR_SERIAL_PORT_BUSY         = NPT_ERROR_BASE_SERIAL_PORT - 3;

typedef enum {
    NPT_SERIAL_PORT_PARITY_NONE,
    NPT_SERIAL_PORT_PARITY_EVEN,
    NPT_SERIAL_PORT_PARITY_ODD,
    NPT_SERIAL_PORT_PARITY_MARK
} NPT_SerialPortParity;

typedef enum {
    NPT_SERIAL_PORT_STOP_BITS_1,
    NPT_SERIAL_PORT_STOP_BITS_1_5,
    NPT_SERIAL_PORT_STOP_BITS_2
} NPT_SerialPortStopBits;

typedef enum {
    NPT_SERIAL_PORT_FLOW_CONTROL_NONE,
    NPT_SERIAL_PORT_FLOW_CONTROL_HARDWARE,
    NPT_SERIAL_PORT_FLOW_CONTROL_XON_XOFF
} NPT_SerialPortFlowControl;

/*----------------------------------------------------------------------
|   NPT_SerialPortInterface
+---------------------------------------------------------------------*/
class NPT_SerialPortInterface
{
public:
    // constructors and destructor
    virtual ~NPT_SerialPortInterface() {}

    // methods
    virtual NPT_Result Open(unsigned int              speed, 
                            NPT_SerialPortStopBits    stop_bits,
                            NPT_SerialPortFlowControl flow_control,
                            NPT_SerialPortParity      parity) = 0;
    virtual NPT_Result Close() = 0;
    virtual NPT_Result GetInputStream(NPT_InputStreamReference& stream) = 0;
    virtual NPT_Result GetOutputStream(NPT_OutputStreamReference& stream) = 0;
};

/*----------------------------------------------------------------------
|   NPT_SerialPort
+---------------------------------------------------------------------*/
class NPT_SerialPort : public NPT_SerialPortInterface
{
public:
    // constructors and destructor
    NPT_SerialPort(const char* name);
   ~NPT_SerialPort() { delete m_Delegate; }

    // NPT_SerialPortInterface methods
    NPT_Result Open(unsigned int              speed, 
                    NPT_SerialPortStopBits    stop_bits = NPT_SERIAL_PORT_STOP_BITS_1,
                    NPT_SerialPortFlowControl flow_control = NPT_SERIAL_PORT_FLOW_CONTROL_NONE,
                    NPT_SerialPortParity      parity = NPT_SERIAL_PORT_PARITY_NONE) {
        return m_Delegate->Open(speed, stop_bits, flow_control, parity);
    }
    NPT_Result Close() {
        return m_Delegate->Close();
    }
    NPT_Result GetInputStream(NPT_InputStreamReference& stream) {
        return m_Delegate->GetInputStream(stream);
    }
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream) {
        return m_Delegate->GetOutputStream(stream);
    }

protected:
    // members
    NPT_SerialPortInterface* m_Delegate;
};

#endif // _NPT_SERIAL_PORT_H_ 
