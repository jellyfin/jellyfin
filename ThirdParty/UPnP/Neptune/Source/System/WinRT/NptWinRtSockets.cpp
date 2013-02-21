/*****************************************************************
|
|   Neptune - Sockets :: WinRT Implementation
|
|   (c) 2001-2012 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptWinRtPch.h"

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Networking;
using namespace Windows::Networking::Sockets;
using namespace Windows::Storage::Streams;
using namespace Concurrency;

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.sockets.winrt")

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
const DWORD NPT_WINRT_SOCKET_DEFAULT_READ_TIMEOUT  = 30000;
const DWORD NPT_WINRT_SOCKET_DEFAULT_WRITE_TIMEOUT = 30000;

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket
+---------------------------------------------------------------------*/
class NPT_WinRtTcpClientSocket : public NPT_SocketInterface
{
 public:
    // constructors and destructor
             NPT_WinRtTcpClientSocket();
    virtual ~NPT_WinRtTcpClientSocket();

    // NPT_SocketInterface methods
    NPT_Result Bind(const NPT_SocketAddress& address, bool reuse_address = true);
    NPT_Result Connect(const NPT_SocketAddress& address, NPT_Timeout timeout);
    NPT_Result GetInputStream(NPT_InputStreamReference& stream);
    NPT_Result GetOutputStream(NPT_OutputStreamReference& stream);
    NPT_Result GetInfo(NPT_SocketInfo& info);
    NPT_Result SetReadTimeout(NPT_Timeout timeout);
    NPT_Result SetWriteTimeout(NPT_Timeout timeout);
    NPT_Result Cancel(bool shutdown);

protected:
	StreamSocket^ m_Socket;
	HostName^     m_RemoteHostName;
	HANDLE        m_WaitEvent;
	NPT_Timeout   m_ReadTimeout;
	NPT_Timeout   m_WriteTimeout;
};

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream
+---------------------------------------------------------------------*/
class NPT_WinRtSocketInputStream : public NPT_InputStream
{
public:
    // constructors and destructor
    NPT_WinRtSocketInputStream(StreamSocket^ socket, NPT_Timeout timeout);
	virtual ~NPT_WinRtSocketInputStream();

    // NPT_InputStream methods
    NPT_Result Read(void*     buffer, 
                    NPT_Size  bytes_to_read, 
                    NPT_Size* bytes_read);
    NPT_Result Seek(NPT_Position offset);
    NPT_Result Tell(NPT_Position& where);
    NPT_Result GetSize(NPT_LargeSize& size);
    NPT_Result GetAvailable(NPT_LargeSize& available);

private:
	StreamSocket^ m_Socket;
	IInputStream^ m_InputStream;
	DataReader^   m_Reader;
	HANDLE        m_WaitEvent;
	NPT_Timeout   m_Timeout; 
};

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream
+---------------------------------------------------------------------*/
class NPT_WinRtSocketOutputStream : public NPT_OutputStream
{
public:
    // constructors and destructor
    NPT_WinRtSocketOutputStream(StreamSocket^ socket, NPT_Timeout timeout);
	virtual ~NPT_WinRtSocketOutputStream();

    // NPT_OutputStream methods
    NPT_Result Write(const void* buffer, 
                     NPT_Size    bytes_to_write, 
                     NPT_Size*   bytes_written);
    NPT_Result Seek(NPT_Position offset);
    NPT_Result Tell(NPT_Position& where);
    NPT_Result Flush();

private:
	StreamSocket^  m_Socket;
	IOutputStream^ m_OutputStream;
	DataWriter^    m_Writer;
	HANDLE         m_WaitEvent;
	NPT_Timeout    m_Timeout;
};

/*----------------------------------------------------------------------
|   StringFromUTF8
+---------------------------------------------------------------------*/
static String^ 
StringFromUTF8(const char* utf)
{
	unsigned int utf_len = NPT_StringLength(utf);
	unsigned int wide_len = utf_len;
	wchar_t* wide = new wchar_t[wide_len+1];
	int result = MultiByteToWideChar(CP_UTF8,
									 0,
									 utf,
									 utf_len+1,
									 wide,
									 wide_len+1);
	String^ str;
	if (result) {
		str = ref new String(wide);
	} else {
		str = ref new String();
	}
	delete[] wide;
	return str;
}

/*----------------------------------------------------------------------
|   TranslateHResult
+---------------------------------------------------------------------*/
static NPT_Result
TranslateHResult(HResult result)
{
	switch (HRESULT_FACILITY(result.Value)) {
		case FACILITY_WIN32:
			switch (HRESULT_CODE(result.Value)) {
			case WSAHOST_NOT_FOUND:
				return NPT_ERROR_HOST_UNKNOWN;

			case WSAETIMEDOUT:
				return NPT_ERROR_TIMEOUT;

			case WSAECONNREFUSED:
				return NPT_ERROR_CONNECTION_REFUSED;

			case WSAEWOULDBLOCK:
				return NPT_ERROR_WOULD_BLOCK;

			case WSAECONNABORTED:
				return NPT_ERROR_CONNECTION_ABORTED;

			case WSAECONNRESET:
			case WSAENETRESET:
				return NPT_ERROR_CONNECTION_RESET;

			case WSAEADDRINUSE:
				return NPT_ERROR_ADDRESS_IN_USE;

			case WSAENETDOWN:
				return NPT_ERROR_NETWORK_DOWN;

			case WSAENETUNREACH:
				return NPT_ERROR_NETWORK_UNREACHABLE;

			case WSAEINTR:
				return NPT_ERROR_INTERRUPTED;

			case WSAENOTCONN:
				return NPT_ERROR_NOT_CONNECTED;

			default:
				return NPT_FAILURE;
			}
			break;

		/* TODO: map error codes */
	default:
		return NPT_FAILURE;
	}
}

/*----------------------------------------------------------------------
|   WaitForAsyncAction
+---------------------------------------------------------------------*/
static NPT_Result
WaitForAsyncAction(IAsyncAction^ action, 
                   HANDLE        wait_event, 
				   DWORD         timeout = INFINITE)
{
	NPT_Result result = NPT_ERROR_INTERNAL;

	NPT_LOG_FINEST("waiting for async action...");
	ResetEvent(wait_event);

	action->Completed = ref new AsyncActionCompletedHandler
        ([&](IAsyncAction^ action_, AsyncStatus status) {
		switch (status) {
			case AsyncStatus::Canceled:
				result = NPT_ERROR_TIMEOUT;
				break;

			case AsyncStatus::Completed:
				result = NPT_SUCCESS;
				break;

			case AsyncStatus::Error:
				NPT_LOG_FINE_1("AsyncAction error %x", action_->ErrorCode.Value);
				result = TranslateHResult(action_->ErrorCode);
				break;

			default:
				result = NPT_ERROR_INTERNAL;
				break;
		}
		SetEvent(wait_event);
	});

	DWORD wait_result = WaitForSingleObjectEx(wait_event, timeout, FALSE);
	if (wait_result != WAIT_OBJECT_0) {
		NPT_LOG_FINE("action timed out, canceling...");
		action->Cancel();
		WaitForSingleObjectEx(wait_event, INFINITE, FALSE);
	}
	NPT_LOG_FINEST("done waiting for async action");

	return result;
}

/*----------------------------------------------------------------------
|   WaitForAsyncOperation
+---------------------------------------------------------------------*/
static NPT_Result
WaitForAsyncOperation(IAsyncOperation<unsigned int>^ operation, 
                      HANDLE                         wait_event,
					  unsigned int&                  return_value,
					  DWORD                          timeout = INFINITE)
{
	NPT_Result result = NPT_ERROR_INTERNAL;

	NPT_LOG_FINEST("waiting for async operation...");
	return_value = 0;
	ResetEvent(wait_event);

	operation->Completed = ref new AsyncOperationCompletedHandler<unsigned int> 
		([&](IAsyncOperation<unsigned int>^ operation_, AsyncStatus status) {
		switch (status) {
			case AsyncStatus::Canceled:
				result = NPT_ERROR_TIMEOUT;
				break;

			case AsyncStatus::Completed:
				return_value = operation_->GetResults();
				result = NPT_SUCCESS;
				break;

			case AsyncStatus::Error:
				NPT_LOG_FINE_1("AsyncOperation error %x", operation_->ErrorCode.Value);
				result = TranslateHResult(operation_->ErrorCode);
				break;

			default:
				result = NPT_ERROR_INTERNAL;
				break;
		}
		operation_->Close();
		SetEvent(wait_event);
	});

	DWORD wait_result = WaitForSingleObjectEx(wait_event, timeout, FALSE);
	if (wait_result != WAIT_OBJECT_0) {
		NPT_LOG_FINE("operation timed out, canceling...");
		operation->Cancel();
		WaitForSingleObjectEx(wait_event, INFINITE, FALSE);
	}
	NPT_LOG_FINEST("done waiting for async operation");

	return result;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::NPT_WinRtSocketInputStream
+---------------------------------------------------------------------*/
NPT_WinRtSocketInputStream::NPT_WinRtSocketInputStream(StreamSocket^ socket, 
													   NPT_Timeout   timeout) :
    m_Socket(socket),
	m_Timeout(timeout)
{
	m_InputStream = socket->InputStream;
	m_Reader = ref new DataReader(m_InputStream);
	m_Reader->InputStreamOptions = InputStreamOptions::Partial;
	m_WaitEvent = CreateEventExW(NULL, L"", 0, EVENT_ALL_ACCESS);
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::~NPT_WinRtSocketInputStream
+---------------------------------------------------------------------*/
NPT_WinRtSocketInputStream::~NPT_WinRtSocketInputStream()
{
	m_Reader->DetachStream();
	CloseHandle(m_WaitEvent);
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::Read
+---------------------------------------------------------------------*/
NPT_Result
NPT_WinRtSocketInputStream::Read(void*     buffer, 
                                 NPT_Size  bytes_to_read, 
                                 NPT_Size* bytes_read)
{
	// init and shortcut
	if (bytes_read) *bytes_read = 0;
	if (bytes_to_read == 0) return NPT_SUCCESS;

	NPT_LOG_FINER_1("reading %d bytes", bytes_to_read);
	auto operation = m_Reader->LoadAsync(bytes_to_read);
	
	unsigned int return_value = 0;
	NPT_Result result = WaitForAsyncOperation(operation, m_WaitEvent, return_value, m_Timeout);

	if (NPT_SUCCEEDED(result)) {
		if (return_value) {
			unsigned int bytes_available = m_Reader->UnconsumedBufferLength;
			Array<unsigned char>^ bytes = ref new Array<unsigned char>(bytes_available);
			m_Reader->ReadBytes(bytes);
			NPT_CopyMemory(buffer, bytes->Data, bytes_available);
			if (bytes_read) *bytes_read = bytes_available;
			return NPT_SUCCESS;
		} else {
			return NPT_ERROR_EOS;
		}
	}

	return result;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::Seek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketInputStream::Seek(NPT_Position offset)
{
	return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::Tell
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketInputStream::Tell(NPT_Position& where)
{
	where = 0;
	return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::GetSize
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketInputStream::GetSize(NPT_LargeSize& size)
{
	size = 0;
	return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketInputStream::GetAvailable
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketInputStream::GetAvailable(NPT_LargeSize& available)
{
	available = 0;
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream::NPT_WinRtSocketOutputStream
+---------------------------------------------------------------------*/
NPT_WinRtSocketOutputStream::NPT_WinRtSocketOutputStream(StreamSocket^ socket,
														 NPT_Timeout   timeout) :
    m_Socket(socket),
	m_Timeout(timeout)
{
	m_OutputStream = socket->OutputStream;
	m_Writer = ref new DataWriter(m_OutputStream);
	m_WaitEvent = CreateEventExW(NULL, L"", 0, EVENT_ALL_ACCESS);
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream::~NPT_WinRtSocketOutputStream
+---------------------------------------------------------------------*/
NPT_WinRtSocketOutputStream::~NPT_WinRtSocketOutputStream()
{
	m_Writer->DetachStream();
	CloseHandle(m_WaitEvent);
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream::Write
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketOutputStream::Write(const void* buffer, 
                                   NPT_Size    bytes_to_write, 
                                   NPT_Size*   bytes_written)
{
	NPT_LOG_FINER_1("writing %d bytes", bytes_to_write);

	Array<unsigned char>^ bytes = ref new Array<unsigned char>(bytes_to_write);
	NPT_CopyMemory(bytes->Data, buffer, bytes_to_write);
	m_Writer->WriteBytes(bytes);
	auto operation = m_Writer->StoreAsync();
	unsigned int return_value = 0;
	
	NPT_Result result = WaitForAsyncOperation(operation, m_WaitEvent, return_value, m_Timeout);
	if (bytes_written) *bytes_written = return_value;

	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream::Seek
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketOutputStream::Seek(NPT_Position offset)
{
	return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream::Tell
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketOutputStream::Tell(NPT_Position& where)
{
	where = 0;
	return NPT_ERROR_NOT_SUPPORTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtSocketOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtSocketOutputStream::Flush()
{
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::NPT_WinRtTcpClientSocket
+---------------------------------------------------------------------*/
NPT_WinRtTcpClientSocket::NPT_WinRtTcpClientSocket() :
	m_ReadTimeout(NPT_WINRT_SOCKET_DEFAULT_READ_TIMEOUT),
	m_WriteTimeout(NPT_WINRT_SOCKET_DEFAULT_WRITE_TIMEOUT)
{
	 m_Socket = ref new StreamSocket();
	 m_WaitEvent = CreateEventExW(NULL, L"", 0, EVENT_ALL_ACCESS);
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::NPT_WinRtTcpClientSocket
+---------------------------------------------------------------------*/
NPT_WinRtTcpClientSocket::~NPT_WinRtTcpClientSocket()
{
	CloseHandle(m_WaitEvent);
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::Bind
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::Bind(const NPT_SocketAddress& address, bool reuse_address)
{
	return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::Connect
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::Connect(const NPT_SocketAddress& address, NPT_Timeout timeout)
{
	try {
		NPT_LOG_FINE_1("connecting to %s", address.GetIpAddress().m_HostName.GetChars());

		m_RemoteHostName = ref new HostName(StringFromUTF8(address.GetIpAddress().m_HostName.GetChars()));
		String^ remote_service = ref new String();
		NPT_String port = NPT_String::FromIntegerU(address.GetPort());
		IAsyncAction^ connection = m_Socket->ConnectAsync(m_RemoteHostName, StringFromUTF8(port.GetChars()));

		// wait for the connection to be established
		NPT_Result result = WaitForAsyncAction(connection, m_WaitEvent, timeout);
		if (NPT_FAILED(result)) {
			NPT_LOG_FINE_1("connection failed (%d)", result);
		} else {
			NPT_LOG_FINE("connected");
		}
		return result;
	} catch (Exception^ e) {
		NPT_LOG_FINE("exception caught");
		return NPT_FAILURE; 
	}
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::GetInputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::GetInputStream(NPT_InputStreamReference& stream)
{
	stream = new NPT_WinRtSocketInputStream(m_Socket, m_ReadTimeout);
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::GetOutputStream
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::GetOutputStream(NPT_OutputStreamReference& stream)
{
	stream = new NPT_WinRtSocketOutputStream(m_Socket, m_WriteTimeout);
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::GetInfo
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::GetInfo(NPT_SocketInfo& info)
{
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::SetReadTimeout
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::SetReadTimeout(NPT_Timeout timeout)
{
	m_ReadTimeout = timeout;
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::SetWriteTimeout
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::SetWriteTimeout(NPT_Timeout timeout)
{
	m_WriteTimeout = timeout;
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_WinRtTcpClientSocket::Cancel
+---------------------------------------------------------------------*/
NPT_Result 
NPT_WinRtTcpClientSocket::Cancel(bool shutdown)
{
	return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Socket::~NPT_Socket
+---------------------------------------------------------------------*/
NPT_Socket::~NPT_Socket()
{
    delete m_SocketDelegate;
}

/*----------------------------------------------------------------------
|   NPT_UdpSocket::NPT_UdpSocket
+---------------------------------------------------------------------*/
NPT_UdpSocket::NPT_UdpSocket(NPT_Flags flags)
{
    m_SocketDelegate    = NULL;
    m_UdpSocketDelegate = NULL;
}

/*----------------------------------------------------------------------
|   NPT_UdpSocket::NPT_UdpSocket
+---------------------------------------------------------------------*/
NPT_UdpSocket::NPT_UdpSocket(NPT_UdpSocketInterface* delegate) :
    m_UdpSocketDelegate(delegate)
{
}

/*----------------------------------------------------------------------
|   NPT_UdpSocket::~NPT_UdpSocket
+---------------------------------------------------------------------*/
NPT_UdpSocket::~NPT_UdpSocket()
{
    m_UdpSocketDelegate = NULL;
    m_SocketDelegate    = NULL;
}

/*----------------------------------------------------------------------
|   NPT_UdpMulticastSocket::NPT_UdpMulticastSocket
+---------------------------------------------------------------------*/
NPT_UdpMulticastSocket::NPT_UdpMulticastSocket(NPT_Flags flags) :
    NPT_UdpSocket((NPT_UdpSocketInterface*)0)
{
    m_SocketDelegate             = NULL;
    m_UdpSocketDelegate          = NULL;
    m_UdpMulticastSocketDelegate = NULL;
}

/*----------------------------------------------------------------------
|   NPT_UdpMulticastSocket::~NPT_UdpMulticastSocket
+---------------------------------------------------------------------*/
NPT_UdpMulticastSocket::~NPT_UdpMulticastSocket()
{
    m_SocketDelegate             = NULL;
    m_UdpSocketDelegate          = NULL;
    m_UdpMulticastSocketDelegate = NULL;
}

/*----------------------------------------------------------------------
|   NPT_TcpClientSocket::NPT_TcpClientSocket
+---------------------------------------------------------------------*/
NPT_TcpClientSocket::NPT_TcpClientSocket(NPT_Flags flags) :
    NPT_Socket(NULL)
{
	m_SocketDelegate = new NPT_WinRtTcpClientSocket();
}

/*----------------------------------------------------------------------
|   NPT_TcpClientSocket::NPT_TcpClientSocket
+---------------------------------------------------------------------*/
NPT_TcpClientSocket::~NPT_TcpClientSocket()
{
    delete m_SocketDelegate;

    m_SocketDelegate = NULL;
}

/*----------------------------------------------------------------------
|   NPT_TcpServerSocket::NPT_TcpServerSocket
+---------------------------------------------------------------------*/
NPT_TcpServerSocket::NPT_TcpServerSocket(NPT_Flags flags)
{
    m_SocketDelegate          = NULL;
    m_TcpServerSocketDelegate = NULL;
}

/*----------------------------------------------------------------------
|   NPT_TcpServerSocket::NPT_TcpServerSocket
+---------------------------------------------------------------------*/
NPT_TcpServerSocket::~NPT_TcpServerSocket()
{
    m_SocketDelegate          = NULL;
    m_TcpServerSocketDelegate = NULL;
}
