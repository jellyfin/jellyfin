/*****************************************************************
|
|   Neptune - Serial Ports :: Null Implementation
|
|   (c) 2001-2007 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptUtils.h"
#include "NptSerialPort.h"

/*----------------------------------------------------------------------
|   NPT_NullSerialPort
+---------------------------------------------------------------------*/
class NPT_NullSerialPort: public NPT_SerialPortInterface
{
public:
    // NPT_SerialPortInterface methods
    NPT_Result Open(unsigned int              speed, 
                    NPT_SerialPortStopBits    stop_bits = NPT_SERIAL_PORT_STOP_BITS_1,
                    NPT_SerialPortFlowControl flow_control = NPT_SERIAL_PORT_FLOW_CONTROL_NONE,
                    NPT_SerialPortParity      parity = NPT_SERIAL_PORT_PARITY_NONE);
    NPT_Result Close();
    NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream);
};

/*----------------------------------------------------------------------
|   NPT_NullSerialPort::Open
+---------------------------------------------------------------------*/
NPT_Result
NPT_NullSerialPort::Open(unsigned int              /* speed        */, 
                         NPT_SerialPortStopBits    /* stop_bits    */,
                         NPT_SerialPortFlowControl /* flow_control */,
                         NPT_SerialPortParity      /* parity       */)
{
    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_NullSerialPort::Close
+---------------------------------------------------------------------*/
NPT_Result
NPT_NullSerialPort::Close()
{
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_NullSerialPort::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_NullSerialPort::GetInputStream(NPT_InputStreamReference& stream)
{
    // default value
    stream = NULL;

    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_NullSerialPort::GetOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_NullSerialPort::GetOutputStream(NPT_OutputStreamReference& stream)
{
    // default value
    stream = NULL;

    return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_SerialPort::NPT_SerialPort
+---------------------------------------------------------------------*/
NPT_SerialPort::NPT_SerialPort(const char* /* name */)
{
    m_Delegate = new NPT_NullSerialPort();
}
