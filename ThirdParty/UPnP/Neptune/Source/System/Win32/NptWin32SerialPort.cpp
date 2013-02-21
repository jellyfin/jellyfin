/*****************************************************************
|
|   Neptune - Serial Ports :: Win32 Implementation
|
|   (c) 2001-2007 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <windows.h>

#include "NptUtils.h"
#include "NptSerialPort.h"
#include "NptStrings.h"
#include "NptLogging.h"

/*----------------------------------------------------------------------
|   NPT_Win32HandletWrapper
+---------------------------------------------------------------------*/
class NPT_Win32HandleWrapper
{
public:
    // constructors and destructor
    NPT_Win32HandleWrapper(HANDLE handle) : m_Handle(handle) {}
    ~NPT_Win32HandleWrapper() {
        CloseHandle(m_Handle);
    }

    // methods
    HANDLE GetHandle() { return m_Handle; }

private:
    // members
    HANDLE m_Handle;
};

typedef NPT_Reference<NPT_Win32HandleWrapper> NPT_Win32HandleReference;

/*----------------------------------------------------------------------
|   NPT_Win32SerialPortStream
+---------------------------------------------------------------------*/
class NPT_Win32SerialPortStream
{
public:
    // constructors and destructor
    NPT_Win32SerialPortStream(NPT_Win32HandleReference handle) :
      m_HandleReference(handle) {}

protected:
    // constructors and destructors
    virtual ~NPT_Win32SerialPortStream() {}

    // members
    NPT_Win32HandleReference m_HandleReference;
};

/*----------------------------------------------------------------------
|   NPT_Win32SerialPortInputStream
+---------------------------------------------------------------------*/
class NPT_Win32SerialPortInputStream : public NPT_InputStream,
                                       private NPT_Win32SerialPortStream
                                
{
public:
    // constructors and destructor
    NPT_Win32SerialPortInputStream(NPT_Win32HandleReference& handle) :
        NPT_Win32SerialPortStream(handle) {}

    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read);
    NPT_Result Seek(NPT_Position /* offset */) {
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result Tell(NPT_Position& /* offset */) {
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result GetSize(NPT_LargeSize& /* size */) {
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result GetAvailable(NPT_LargeSize& /* available */) {
        return NPT_ERROR_NOT_SUPPORTED;
    }
};

/*----------------------------------------------------------------------
|   NPT_Win32SerialPortInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32SerialPortInputStream::Read(void*     buffer, 
                                     NPT_Size  bytes_to_read, 
                                     NPT_Size* bytes_read)
{
    DWORD nb_read = 0;
    BOOL result = ReadFile(m_HandleReference->GetHandle(), 
                           buffer, 
                           bytes_to_read, 
                           &nb_read, 
                           NULL);
    if (result == TRUE) {
        if (bytes_read) *bytes_read = nb_read;
        return NPT_SUCCESS;
    } else {
        if (bytes_read) *bytes_read = 0;
        return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPortOutputStream
+---------------------------------------------------------------------*/
class NPT_Win32SerialPortOutputStream : public NPT_OutputStream,
                                        private NPT_Win32SerialPortStream
{
public:
    // constructors and destructor
    NPT_Win32SerialPortOutputStream(NPT_Win32HandleReference& handle) :
        NPT_Win32SerialPortStream(handle) {}

    // NPT_InputStream methods
    NPT_Result Write(const void* buffer, 
                     NPT_Size    bytes_to_write, 
                     NPT_Size*   bytes_written);
    NPT_Result Seek(NPT_Position /* offset */) {
        return NPT_ERROR_NOT_SUPPORTED;
    }
    NPT_Result Tell(NPT_Position& /* offset */) {
        return NPT_ERROR_NOT_SUPPORTED;
    }
};

/*----------------------------------------------------------------------
|   NPT_Win32SerialPortOutputStream::Write
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32SerialPortOutputStream::Write(const void* buffer, 
                                       NPT_Size    bytes_to_write, 
                                       NPT_Size*   bytes_written)
{
    DWORD nb_written = 0;

    BOOL result = WriteFile(m_HandleReference->GetHandle(), 
                            buffer, 
                            bytes_to_write, 
                            &nb_written, 
                            NULL);
    if (result == TRUE) {
        if (bytes_written) *bytes_written = nb_written;
        return NPT_SUCCESS;
    } else {
        if (bytes_written) *bytes_written = 0;
        return NPT_FAILURE;
    }
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort
+---------------------------------------------------------------------*/
class NPT_Win32SerialPort: public NPT_SerialPortInterface
{
public:
    // constructors and destructor
    NPT_Win32SerialPort(const char* name);
   ~NPT_Win32SerialPort();

    // NPT_SerialPortInterface methods
    NPT_Result Open(unsigned int              speed, 
                    NPT_SerialPortStopBits    stop_bits = NPT_SERIAL_PORT_STOP_BITS_1,
                    NPT_SerialPortFlowControl flow_control = NPT_SERIAL_PORT_FLOW_CONTROL_NONE,
                    NPT_SerialPortParity      parity = NPT_SERIAL_PORT_PARITY_NONE);
    NPT_Result Close();
    NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream);

private:
    // members
    NPT_String               m_Name;
    NPT_Win32HandleReference m_HandleReference;
};

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort::NPT_Win32SerialPort
+---------------------------------------------------------------------*/
NPT_Win32SerialPort::NPT_Win32SerialPort(const char* name) :
    m_Name(name)
{
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort::~NPT_Win32SerialPort
+---------------------------------------------------------------------*/
NPT_Win32SerialPort::~NPT_Win32SerialPort()
{
    Close();
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort::Open
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32SerialPort::Open(unsigned int              speed, 
                          NPT_SerialPortStopBits    stop_bits,
                          NPT_SerialPortFlowControl flow_control,
                          NPT_SerialPortParity      parity)
{
    // check if we're already open
    if (!m_HandleReference.IsNull()) {
        return NPT_ERROR_SERIAL_PORT_ALREADY_OPEN;
    }

    HANDLE handle = CreateFile(m_Name,  
                               GENERIC_READ | GENERIC_WRITE, 
                               0, 
                               0, 
                               OPEN_EXISTING,
                               0,
                               0);
    if (handle == INVALID_HANDLE_VALUE) {
        return NPT_ERROR_NO_SUCH_SERIAL_PORT;
    }

    // set the parameters
    DCB dcb;
    NPT_SetMemory(&dcb, 0, sizeof(dcb));
    dcb.DCBlength = sizeof(DCB);
    if (!GetCommState(handle, &dcb)) {
        CloseHandle(handle);
        return NPT_FAILURE;
    }
    dcb.fBinary = TRUE;
    dcb.BaudRate = speed;
    switch (stop_bits) {
        case NPT_SERIAL_PORT_STOP_BITS_1: dcb.StopBits   = ONESTOPBIT; break;
        case NPT_SERIAL_PORT_STOP_BITS_1_5: dcb.StopBits = ONE5STOPBITS; break;
        case NPT_SERIAL_PORT_STOP_BITS_2: dcb.StopBits   = TWOSTOPBITS; break;
    }
    switch (flow_control) {
        case NPT_SERIAL_PORT_FLOW_CONTROL_NONE:
            dcb.fOutX = dcb.fOutxCtsFlow = dcb.fOutxDsrFlow = FALSE;
            dcb.fInX = dcb.fDsrSensitivity = FALSE;
            dcb.fRtsControl = RTS_CONTROL_DISABLE;
            dcb.fDtrControl = DTR_CONTROL_DISABLE;
            break;

        case NPT_SERIAL_PORT_FLOW_CONTROL_HARDWARE:
            dcb.fOutX = dcb.fOutxDsrFlow = FALSE;
            dcb.fOutxCtsFlow = TRUE;
            dcb.fInX = dcb.fDsrSensitivity = FALSE;
            dcb.fRtsControl = RTS_CONTROL_HANDSHAKE;
            dcb.fDtrControl = DTR_CONTROL_DISABLE;
            break;

        case NPT_SERIAL_PORT_FLOW_CONTROL_XON_XOFF:
            dcb.fOutX = TRUE;
            dcb.fOutxCtsFlow = dcb.fOutxDsrFlow = FALSE;
            dcb.fInX = TRUE;
            dcb.fDsrSensitivity = FALSE;
            dcb.fRtsControl = RTS_CONTROL_DISABLE;
            dcb.fDtrControl = DTR_CONTROL_DISABLE;
            break;
    }
    switch (parity) {
        case NPT_SERIAL_PORT_PARITY_NONE: dcb.fParity = FALSE; dcb.Parity = NOPARITY; break;
        case NPT_SERIAL_PORT_PARITY_EVEN: dcb.fParity = TRUE;  dcb.Parity = EVENPARITY; break;
        case NPT_SERIAL_PORT_PARITY_ODD: dcb.fParity  = TRUE;  dcb.Parity = ODDPARITY; break;
        case NPT_SERIAL_PORT_PARITY_MARK: dcb.fParity = TRUE;  dcb.Parity = MARKPARITY; break;
    }
    if (!SetCommState(handle, &dcb)) {
        CloseHandle(handle);
        return NPT_FAILURE;
    }

    // create a reference to the FILE object
    m_HandleReference = new NPT_Win32HandleWrapper(handle);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort::Close
+---------------------------------------------------------------------*/
NPT_Result
NPT_Win32SerialPort::Close()
{
    // release the file reference
    m_HandleReference = NULL;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Win32SerialPort::GetInputStream(NPT_InputStreamReference& stream)
{
    // default value
    stream = NULL;

    // check that the file is open
    if (m_HandleReference.IsNull()) return NPT_ERROR_SERIAL_PORT_NOT_OPEN;

    // create a stream
    stream = new NPT_Win32SerialPortInputStream(m_HandleReference);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Win32SerialPort::GetOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_Win32SerialPort::GetOutputStream(NPT_OutputStreamReference& stream)
{
    // default value
    stream = NULL;

    // check that the file is open
    if (m_HandleReference.IsNull()) return NPT_ERROR_SERIAL_PORT_NOT_OPEN;

    // create a stream
    stream = new NPT_Win32SerialPortOutputStream(m_HandleReference);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_SerialPort::NPT_SerialPort
+---------------------------------------------------------------------*/
NPT_SerialPort::NPT_SerialPort(const char* name)
{
    m_Delegate = new NPT_Win32SerialPort(name);
}
