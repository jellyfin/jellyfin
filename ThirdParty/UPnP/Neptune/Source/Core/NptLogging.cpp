/*****************************************************************
|
|   Neptune - Logging Support
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
/** @file
* Implementation file for logging
*/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include <stdarg.h>

#include "NptLogging.h"
#include "NptList.h"
#include "NptStreams.h"
#include "NptSockets.h"
#include "NptUtils.h"
#include "NptFile.h"
#include "NptSystem.h"
#include "NptConsole.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
//NPT_SET_LOCAL_LOGGER("neptune.logging")

/*----------------------------------------------------------------------
|   types
+---------------------------------------------------------------------*/
class NPT_LogConsoleHandler : public NPT_LogHandler {
public:
    // enums
    enum {
        OUTPUT_TO_CONSOLE = 1,
        OUTPUT_TO_DEBUG   = 2
    };

    // class methods
    static NPT_Result Create(const char* logger_name, NPT_LogHandler*& handler);

    // methods
    void Log(const NPT_LogRecord& record);

private:
    // members
    NPT_UInt32 m_Outputs;
    bool       m_UseColors;
    NPT_Flags  m_FormatFilter;
};

class NPT_LogFileHandler : public NPT_LogHandler {
public:
    // class methods
    static NPT_Result Create(const char* logger_name, NPT_LogHandler*& handler);

    // methods
    void Log(const NPT_LogRecord& record);

private:
    NPT_Result Open(bool append = true);
    
private:
    // members
    bool                      m_Flush;
    bool                      m_Append;
    NPT_String                m_Filename;
    NPT_Flags                 m_FormatFilter;
    NPT_LargeSize             m_MaxFilesize;
    NPT_OutputStreamReference m_Stream;
};

class NPT_LogTcpHandler : public NPT_LogHandler {
public:
    // class methods
    static void       FormatRecord(const NPT_LogRecord& record, NPT_String& msg);
    static NPT_Result Create(const char* logger_name, NPT_LogHandler*& handler);

    // methods
    void Log(const NPT_LogRecord& record);

private:
    // methods
    NPT_Result Connect();

    // members
    NPT_String                m_Host;
    NPT_UInt16                m_Port;
    NPT_OutputStreamReference m_Stream;
};

class NPT_LogUdpHandler : public NPT_LogHandler {
public:
    // class methods
    static NPT_Result Create(const char* logger_name, NPT_LogHandler*& handler);

    // methods
    void Log(const NPT_LogRecord& record);

private:
    // members
    NPT_UdpSocket     m_Socket;
    NPT_SocketAddress m_Target;
};

class NPT_LogNullHandler : public NPT_LogHandler {
public:
    // class methods
    static NPT_Result Create(NPT_LogHandler*& handler);

    // methods
    void Log(const NPT_LogRecord& record);
};

class NPT_LogCustomHandler : public NPT_LogHandler {
public:
    // class methods
    static NPT_Result SetCustomHandlerFunction(CustomHandlerExternalFunction function);
    static NPT_Result Create(NPT_LogHandler*& handler);
    
    // methods
    void Log(const NPT_LogRecord& record);
    
private:
    static CustomHandlerExternalFunction s_ExternalFunction;
};

/*----------------------------------------------------------------------
|   constants
+---------------------------------------------------------------------*/
#define NPT_LOG_HEAP_BUFFER_INCREMENT 4096
#define NPT_LOG_STACK_BUFFER_MAX_SIZE 512
#define NPT_LOG_HEAP_BUFFER_MAX_SIZE  65536

#if !defined(NPT_CONFIG_LOG_CONFIG_ENV)
#define NPT_CONFIG_LOG_CONFIG_ENV "NEPTUNE_LOG_CONFIG"
#endif

#if !defined(NPT_CONFIG_DEFAULT_LOG_CONFIG_SOURCE)
#define NPT_CONFIG_DEFAULT_LOG_CONFIG_SOURCE "file:neptune-logging.properties"
#endif

#if !defined(NPT_CONFIG_DEFAULT_LOG_LEVEL)
#define NPT_CONFIG_DEFAULT_LOG_LEVEL NPT_LOG_LEVEL_OFF
#endif
#define NPT_LOG_ROOT_DEFAULT_HANDLER   "ConsoleHandler"
#if !defined(NPT_CONFIG_DEFAULT_FILE_HANDLER_FILENAME)
#define NPT_CONFIG_DEFAULT_LOG_FILE_HANDLER_FILENAME "_neptune.log"
#endif

#define NPT_LOG_TCP_HANDLER_DEFAULT_PORT            7723
#define NPT_LOG_TCP_HANDLER_DEFAULT_CONNECT_TIMEOUT 5000 /* 5 seconds */

#define NPT_LOG_UDP_HANDLER_DEFAULT_PORT            7724

#if defined(_WIN32) || defined(_WIN32_WCE) || defined(__APPLE__)
#define NPT_LOG_CONSOLE_HANDLER_DEFAULT_COLOR_MODE false
#else
#define NPT_LOG_CONSOLE_HANDLER_DEFAULT_COLOR_MODE true
#endif

#define NPT_LOG_FILE_HANDLER_MIN_RECYCLE_SIZE   1000000

#define NPT_LOG_FORMAT_FILTER_NO_SOURCE         1
#define NPT_LOG_FORMAT_FILTER_NO_TIMESTAMP      2
#define NPT_LOG_FORMAT_FILTER_NO_FUNCTION_NAME  4
#define NPT_LOG_FORMAT_FILTER_NO_LOGGER_NAME    8
#define NPT_LOG_FORMAT_FILTER_NO_SOURCEPATH    16
#define NPT_LOG_FORMAT_FILTER_NO_THREAD_ID     32

/*----------------------------------------------------------------------
|   globals
+---------------------------------------------------------------------*/
static NPT_LogManager LogManager;

/*----------------------------------------------------------------------
|   NPT_LogManagerAutoDisabler
+---------------------------------------------------------------------*/
class NPT_LogManagerAutoDisabler
{
public:
    NPT_LogManagerAutoDisabler() : m_WasEnabled(LogManager.IsEnabled()) {
        LogManager.SetEnabled(false);
    }
    ~NPT_LogManagerAutoDisabler() {
        LogManager.SetEnabled(m_WasEnabled);
    }
private:
    bool m_WasEnabled;
};

/*----------------------------------------------------------------------
|   NPT_LogManagerAutoLocker
+---------------------------------------------------------------------*/
class NPT_LogManagerAutoLocker
{
 public:
    // methods
     NPT_LogManagerAutoLocker(NPT_LogManager &manager) : m_Manager(manager)   {
        m_Manager.Lock();
    }
    ~NPT_LogManagerAutoLocker() {
        m_Manager.Unlock(); 
    }
        
 private:
    // members
    NPT_LogManager& m_Manager;
};

/*----------------------------------------------------------------------
|   NPT_GetSystemLogConfig
+---------------------------------------------------------------------*/
#if !defined(NPT_CONFIG_HAVE_SYSTEM_LOG_CONFIG)
NPT_Result NPT_GetSystemLogConfig(NPT_String& /*config*/)
{
    return NPT_ERROR_NOT_SUPPORTED;
}
#endif

/*----------------------------------------------------------------------
|   NPT_LogHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogHandler::Create(const char*      logger_name,
                       const char*      handler_name,
                       NPT_LogHandler*& handler)
{
    handler = NULL;

    if (NPT_StringsEqual(handler_name, "NullHandler")) {
        return NPT_LogNullHandler::Create(handler);
    } else if (NPT_StringsEqual(handler_name, "FileHandler")) {
        return NPT_LogFileHandler::Create(logger_name, handler);
    } else if (NPT_StringsEqual(handler_name, "ConsoleHandler")) {
        return NPT_LogConsoleHandler::Create(logger_name, handler);
    } else if (NPT_StringsEqual(handler_name, "TcpHandler")) {
        return NPT_LogTcpHandler::Create(logger_name, handler);
    } else if (NPT_StringsEqual(handler_name, "UdpHandler")) {
        return NPT_LogUdpHandler::Create(logger_name, handler);
    } else if (NPT_StringsEqual(handler_name, "CustomHandler")) {
        return NPT_LogCustomHandler::Create(handler);
    }

    return NPT_ERROR_NO_SUCH_CLASS;
}

/*----------------------------------------------------------------------
|   NPT_LogHandler::SetCustomHandlerFunction
+---------------------------------------------------------------------*/
NPT_Result 
NPT_LogHandler::SetCustomHandlerFunction(CustomHandlerExternalFunction function)
{
    return NPT_LogCustomHandler::SetCustomHandlerFunction(function);
}

/*----------------------------------------------------------------------
|   NPT_Log::GetLogLevel
+---------------------------------------------------------------------*/
int 
NPT_Log::GetLogLevel(const char* name)
{
    if (       NPT_StringsEqual(name, "FATAL")) {
        return NPT_LOG_LEVEL_FATAL;
    } else if (NPT_StringsEqual(name, "SEVERE")) {
        return NPT_LOG_LEVEL_SEVERE;
    } else if (NPT_StringsEqual(name, "WARNING")) {
        return NPT_LOG_LEVEL_WARNING;
    } else if (NPT_StringsEqual(name, "INFO")) {
        return NPT_LOG_LEVEL_INFO;
    } else if (NPT_StringsEqual(name, "FINE")) {
        return NPT_LOG_LEVEL_FINE;
    } else if (NPT_StringsEqual(name, "FINER")) {
        return NPT_LOG_LEVEL_FINER;
    } else if (NPT_StringsEqual(name, "FINEST")) {
        return NPT_LOG_LEVEL_FINEST;
    } else if (NPT_StringsEqual(name, "ALL")) {
        return NPT_LOG_LEVEL_ALL;
    } else if (NPT_StringsEqual(name, "OFF")) {
        return NPT_LOG_LEVEL_OFF;
    } else {
        return -1;
    }
}

/*----------------------------------------------------------------------
|   NPT_Log::GetLogLevelName
+---------------------------------------------------------------------*/
const char*
NPT_Log::GetLogLevelName(int level)
{
    switch (level) {
        case NPT_LOG_LEVEL_FATAL:   return "FATAL";
        case NPT_LOG_LEVEL_SEVERE:  return "SEVERE";
        case NPT_LOG_LEVEL_WARNING: return "WARNING";
        case NPT_LOG_LEVEL_INFO:    return "INFO";
        case NPT_LOG_LEVEL_FINE:    return "FINE";
        case NPT_LOG_LEVEL_FINER:   return "FINER";
        case NPT_LOG_LEVEL_FINEST:  return "FINEST";
        case NPT_LOG_LEVEL_OFF:     return "OFF";
        default:                    return "";
    }
}

/*----------------------------------------------------------------------
|   NPT_Log::GetLogLevelAnsiColor
+---------------------------------------------------------------------*/
const char*
NPT_Log::GetLogLevelAnsiColor(int level)
{
    switch (level) {
        case NPT_LOG_LEVEL_FATAL:   return "31";
        case NPT_LOG_LEVEL_SEVERE:  return "31";
        case NPT_LOG_LEVEL_WARNING: return "33";
        case NPT_LOG_LEVEL_INFO:    return "32";
        case NPT_LOG_LEVEL_FINE:    return "34";
        case NPT_LOG_LEVEL_FINER:   return "35";
        case NPT_LOG_LEVEL_FINEST:  return "36";
        default:                    return NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_Log::FormatRecordToStream
+---------------------------------------------------------------------*/
void
NPT_Log::FormatRecordToStream(const NPT_LogRecord& record,
                              NPT_OutputStream&    stream,
                              bool                 use_colors,
                              NPT_Flags            format_filter)
{
    const char* level_name = GetLogLevelName(record.m_Level);
    NPT_String  level_string;

    /* format and emit the record */
    if (level_name[0] == '\0') {
        level_string = NPT_String::FromInteger(record.m_Level);
        level_name = level_string;
    }
    if ((format_filter & NPT_LOG_FORMAT_FILTER_NO_SOURCE) == 0) {
        unsigned int start = 0;
        /* remove source file path if requested */
        if (format_filter & NPT_LOG_FORMAT_FILTER_NO_SOURCEPATH) {
            for (start = NPT_StringLength(record.m_SourceFile);
                 start;
                 --start) {
                if (record.m_SourceFile[start-1] == '\\' ||
                    record.m_SourceFile[start-1] == '/') {
                    break;
                }
            }
        }
        stream.WriteString(record.m_SourceFile + start);
        stream.Write("(", 1, NULL);
        stream.WriteString(NPT_String::FromIntegerU(record.m_SourceLine));
        stream.Write("): ", 3, NULL);
    }
    if ((format_filter & NPT_LOG_FORMAT_FILTER_NO_LOGGER_NAME) == 0) {
        stream.Write("[", 1, NULL);
        stream.WriteString(record.m_LoggerName);
        stream.Write("] ", 2, NULL);
    }
    if ((format_filter & NPT_LOG_FORMAT_FILTER_NO_TIMESTAMP) == 0) {
        NPT_String ts = NPT_DateTime(record.m_TimeStamp, true).ToString(NPT_DateTime::FORMAT_W3C, 
                                                                        NPT_DateTime::FLAG_EMIT_FRACTION | 
                                                                        NPT_DateTime::FLAG_EXTENDED_PRECISION);
        stream.WriteString(ts.GetChars());
        stream.Write(" ", 1);
    }
    if ((format_filter & NPT_LOG_FORMAT_FILTER_NO_FUNCTION_NAME) == 0) {
        stream.WriteFully("[",1);
        if (record.m_SourceFunction) {
            stream.WriteString(record.m_SourceFunction);
        }
        stream.WriteFully("] ",2);
    }
    if ((format_filter & NPT_LOG_FORMAT_FILTER_NO_THREAD_ID) == 0) {
        stream.Write("(", 1, NULL);
        stream.WriteString(NPT_String::FromIntegerU(record.m_ThreadId));
        stream.Write(") ", 2, NULL);
    }
    const char* ansi_color = NULL;
    if (use_colors) {
        ansi_color = GetLogLevelAnsiColor(record.m_Level);
        if (ansi_color) {
            stream.Write("\033[", 2, NULL);
            stream.WriteString(ansi_color);
            stream.Write(";1m", 3, NULL);
        }
    }
    stream.WriteString(level_name);
    if (use_colors && ansi_color) {
        stream.Write("\033[0m", 4, NULL);
    }
    stream.Write(": ", 2, NULL);
    stream.WriteString(record.m_Message);
    stream.Write("\r\n", 2, NULL);
}

/*----------------------------------------------------------------------
|   NPT_LogManager::NPT_LogManager
+---------------------------------------------------------------------*/
NPT_LogManager::NPT_LogManager() :
    m_LockOwner(0),
    m_Enabled(true),
    m_Configured(false),
    m_Root(NULL)
{
}

/*----------------------------------------------------------------------
|   NPT_LogManager::~NPT_LogManager
+---------------------------------------------------------------------*/
NPT_LogManager::~NPT_LogManager()
{
    /* destroy everything we've created */
    for (NPT_List<NPT_Logger*>::Iterator i = m_Loggers.GetFirstItem();
         i;
         ++i) {
        NPT_Logger* logger = *i;
        delete logger;
    }

    /* destroy the root logger */
    delete m_Root;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::GetDefault
+---------------------------------------------------------------------*/
NPT_LogManager&
NPT_LogManager::GetDefault()
{
    return LogManager;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::Lock
+---------------------------------------------------------------------*/
void
NPT_LogManager::Lock()
{
    NPT_Thread::ThreadId me = NPT_Thread::GetCurrentThreadId();
    if (m_LockOwner == me) return;
    m_Lock.Lock();
    m_LockOwner = me;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::Unlock
+---------------------------------------------------------------------*/
void
NPT_LogManager::Unlock()
{
    m_LockOwner = 0;
    m_Lock.Unlock();
}

/*----------------------------------------------------------------------
|   NPT_LogManager::Configure
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogManager::Configure(const char* config_sources) 
{
    //NPT_AutoLock lock(LogManager.m_Lock);
    
    // exit if we're already initialized
    if (m_Configured) return NPT_SUCCESS;

    // we need to be disabled while we configure ourselves
    NPT_LogManagerAutoDisabler autodisabler;
    
    // set some default config values
    SetConfigValue(".handlers", NPT_LOG_ROOT_DEFAULT_HANDLER);

    // see if the config sources have been set to non-default values
    if (config_sources == NULL) {
        config_sources = NPT_CONFIG_DEFAULT_LOG_CONFIG_SOURCE;
    }
    NPT_String config_sources_system;
    if (NPT_SUCCEEDED(NPT_GetSystemLogConfig(config_sources_system))) {
        config_sources = config_sources_system;
    }
    NPT_String config_sources_env;
    if (NPT_SUCCEEDED(NPT_Environment::Get(NPT_CONFIG_LOG_CONFIG_ENV, config_sources_env))) {
        config_sources = config_sources_env;
    }

    /* load all configs */
    NPT_String config_source;
    const char* cursor = config_sources; 
    const char* source = config_sources;
    for (;;) {
        if (*cursor == '\0' || *cursor == '|') {
            if (cursor != source) {
                config_source.Assign(source, (NPT_Size)(cursor-source));
                config_source.Trim(" \t");
                ParseConfigSource(config_source);
            }
            if (*cursor == '\0') break;
        }
        cursor++;
    }

    /* create the root logger */
    LogManager.m_Root = new NPT_Logger("", *this);
    LogManager.m_Root->m_Level = NPT_CONFIG_DEFAULT_LOG_LEVEL;
    LogManager.m_Root->m_LevelIsInherited = false;
    ConfigureLogger(LogManager.m_Root);

    // we're initialized now
    m_Configured = true;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::ConfigValueIsBooleanTrue
+---------------------------------------------------------------------*/
bool
NPT_LogManager::ConfigValueIsBooleanTrue(NPT_String& value)
{
    return 
        value.Compare("true", true) == 0 ||
        value.Compare("yes",  true) == 0 ||
        value.Compare("on",   true) == 0 ||
        value.Compare("1",    true) == 0;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::ConfigValueIsBooleanFalse
+---------------------------------------------------------------------*/
bool
NPT_LogManager::ConfigValueIsBooleanFalse(NPT_String& value)
{
    return 
        value.Compare("false", true) == 0  ||
        value.Compare("no",    true) == 0  ||
        value.Compare("off",   true) == 0  ||
        value.Compare("0",     true) == 0;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::GetConfigValue
+---------------------------------------------------------------------*/
NPT_String*
NPT_LogManager::GetConfigValue(const char* prefix, const char* suffix)
{
    NPT_Size prefix_length = prefix?NPT_StringLength(prefix):0;
    NPT_Size suffix_length = suffix?NPT_StringLength(suffix):0;
    NPT_Size key_length    = prefix_length+suffix_length;
    for (NPT_List<NPT_LogConfigEntry>::Iterator i = LogManager.m_Config.GetFirstItem();
         i;
         ++i) {
        NPT_LogConfigEntry& entry = *i;
        if ((entry.m_Key.GetLength() == key_length) &&
            (prefix == NULL || entry.m_Key.StartsWith(prefix)) &&
            (suffix == NULL || entry.m_Key.EndsWith(suffix  )) ) {
            return &entry.m_Value;
        }
    }

    // not found
    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::SetConfigValue
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogManager::SetConfigValue(const char* key, const char* value)
{
    NPT_String* value_string = GetConfigValue(key, NULL);
    if (value_string) {
        /* the key already exists, replace the value */
        *value_string = value;
    } else {
        /* the value does not already exist, create a new one */
        NPT_CHECK(LogManager.m_Config.Add(NPT_LogConfigEntry(key, value)));
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::ParseConfig
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogManager::ParseConfig(const char* config,
                            NPT_Size    config_size) 
{
    const char* cursor    = config;
    const char* line      = config;
    const char* separator = NULL;
    NPT_String  key;
    NPT_String  value;

    /* parse all entries */
    while (cursor <= config+config_size) {
        /* separators are newlines, ';' or end of buffer */
        if ( cursor == config+config_size ||
            *cursor == '\n'              || 
            *cursor == '\r'              || 
            *cursor == ';') {
            /* newline or end of buffer */
            if (separator && line[0] != '#') {
                /* we have a property */
                key.Assign(line, (NPT_Size)(separator-line));
                value.Assign(line+(separator+1-line), (NPT_Size)(cursor-(separator+1)));
                key.Trim(" \t");
                value.Trim(" \t");
            
                SetConfigValue((const char*)key, (const char*)value);
            }
            line = cursor+1;
            separator = NULL;
        } else if (*cursor == '=' && separator == NULL) {
            separator = cursor;
        }
        cursor++;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::ParseConfigFile
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogManager::ParseConfigFile(const char* filename) 
{
    NPT_Result result;

    /* load the file */
    NPT_DataBuffer buffer;
    result = NPT_File::Load(filename, buffer);
    if (NPT_FAILED(result)) return result;

    /* parse the config */
    return ParseConfig((const char*)buffer.GetData(), buffer.GetDataSize());
}

/*----------------------------------------------------------------------
|   NPT_LogManager::ParseConfigSource
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogManager::ParseConfigSource(NPT_String& source) 
{
    if (source.StartsWith("file:")) {
        /* file source */
        ParseConfigFile(source.GetChars()+5);
    } else if (source.StartsWith("plist:")) {
        /* property list source */
        ParseConfig(source.GetChars()+6, source.GetLength()-6);
    } else if (source.StartsWith("http:port=")) {
        /* http configurator */
        unsigned int port = 0;
        NPT_Result result = NPT_ParseInteger(source.GetChars()+10, port, true);
        if (NPT_FAILED(result)) return result;
        new NPT_HttpLoggerConfigurator(port);
    } else {
        return NPT_ERROR_INVALID_SYNTAX;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::HaveLoggerConfig
+---------------------------------------------------------------------*/
bool
NPT_LogManager::HaveLoggerConfig(const char* name)
{
    NPT_Size name_length = NPT_StringLength(name);
    for (NPT_List<NPT_LogConfigEntry>::Iterator i = m_Config.GetFirstItem();
         i;
         ++i) {
        NPT_LogConfigEntry& entry = *i;
        if (entry.m_Key.StartsWith(name)) {
            const char* suffix = entry.m_Key.GetChars()+name_length;
            if (NPT_StringsEqual(suffix, ".level") ||
                NPT_StringsEqual(suffix, ".handlers") ||
                NPT_StringsEqual(suffix, ".forward")) {
                return true;
            }
        }
    }

    /* no config found */
    return false;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::ConfigureLogger
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogManager::ConfigureLogger(NPT_Logger* logger)
{
    /* configure the level */
    NPT_String* level_value = GetConfigValue(logger->m_Name,".level");
    if (level_value) {
        NPT_Int32 value;
        /* try a symbolic name */
        value = NPT_Log::GetLogLevel(*level_value);
        if (value < 0) {
            /* try a numeric value */
            if (NPT_FAILED(level_value->ToInteger(value, false))) {
                value = -1;
            }
        }
        if (value >= 0) {
            logger->m_Level = value;
            logger->m_LevelIsInherited = false;
        }
    }

    /* remove any existing handlers */
    logger->DeleteHandlers();

    /* configure the handlers */
    NPT_String* handlers = GetConfigValue(logger->m_Name,".handlers");
    if (handlers) {
        const char*     handlers_list = handlers->GetChars();
        const char*     cursor = handlers_list;
        const char*     name_start = handlers_list;
        NPT_String      handler_name;
        NPT_LogHandler* handler;
        for (;;) {
            if (*cursor == '\0' || *cursor == ',') {
                if (cursor != name_start) {
                    handler_name.Assign(name_start, (NPT_Size)(cursor-name_start));
                    handler_name.Trim(" \t");
                    
                    /* create a handler */
                    if (NPT_SUCCEEDED(
                        NPT_LogHandler::Create(logger->m_Name, handler_name, handler))) {
                        logger->AddHandler(handler);
                    }

                }
                if (*cursor == '\0') break;
                name_start = cursor+1;
            }
            ++cursor;
        }
    }

    /* configure the forwarding */
    NPT_String* forward = GetConfigValue(logger->m_Name,".forward");
    if (forward && !ConfigValueIsBooleanTrue(*forward)) {
        logger->m_ForwardToParent = false;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::FindLogger
+---------------------------------------------------------------------*/
NPT_Logger*
NPT_LogManager::FindLogger(const char* name)
{
    for (NPT_List<NPT_Logger*>::Iterator i = LogManager.m_Loggers.GetFirstItem();
         i;
         ++i) {
        NPT_Logger* logger = *i;
        if (logger->m_Name == name) {
            return logger;
        }
    }

    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_LogManager::GetLogger
+---------------------------------------------------------------------*/
NPT_Logger*
NPT_LogManager::GetLogger(const char* name)
{
    // exit now if the log manager is disabled
    if (!LogManager.m_Enabled) return NULL;

    // auto lock until we return from this method
    NPT_LogManagerAutoLocker lock(LogManager);

    /* check that the manager is initialized */
    if (!LogManager.m_Configured) {
        /* init the manager */
        LogManager.Configure();
        NPT_ASSERT(LogManager.m_Configured);
    }

    /* check if this logger is already configured */
    NPT_Logger* logger = LogManager.FindLogger(name);
    if (logger) return logger;

    /* create a new logger */
    logger = new NPT_Logger(name, LogManager);
    if (logger == NULL) return NULL;

    /* configure the logger */
    LogManager.ConfigureLogger(logger);

    /* find which parent to attach to */
    NPT_Logger* parent = LogManager.m_Root;
    NPT_String  parent_name = name;
    for (;;) {
        NPT_Logger* candidate_parent;

        /* find the last dot */
        int dot = parent_name.ReverseFind('.');
        if (dot < 0) break;
        parent_name.SetLength(dot);
        
        /* see if the parent exists */
        candidate_parent = LogManager.FindLogger(parent_name);
        if (candidate_parent) {
            parent = candidate_parent;
            break;
        }

        /* this parent name does not exist, see if we need to create it */
        if (LogManager.HaveLoggerConfig(parent_name)) {
            parent = GetLogger(parent_name);
            break;
        }
    }

    /* attach to the parent */
    logger->SetParent(parent);

    /* add this logger to the list */
    LogManager.m_Loggers.Add(logger);

    return logger;
}

/*----------------------------------------------------------------------
|   NPT_Logger::NPT_Logger
+---------------------------------------------------------------------*/
NPT_Logger::NPT_Logger(const char* name, NPT_LogManager& manager) :
    m_Manager(manager),
    m_Name(name),
    m_Level(NPT_LOG_LEVEL_OFF),
    m_LevelIsInherited(true),
    m_ForwardToParent(true),
    m_Parent(NULL)
{
}

/*----------------------------------------------------------------------
|   NPT_Logger::~NPT_Logger
+---------------------------------------------------------------------*/
NPT_Logger::~NPT_Logger()
{
	/* remove external handlers before cleaning up */
	m_Handlers.Remove(m_ExternalHandlers, true);

    /* delete all handlers */
    m_Handlers.Apply(NPT_ObjectDeleter<NPT_LogHandler>());
}

/*----------------------------------------------------------------------
|   NPT_Logger::DeleteHandlers
+---------------------------------------------------------------------*/
NPT_Result
NPT_Logger::DeleteHandlers()
{
	/* remove external handlers before cleaning up */
	m_Handlers.Remove(m_ExternalHandlers, true);

    /* delete all handlers and empty the list */
    if (m_Handlers.GetItemCount()) {
        m_Handlers.Apply(NPT_ObjectDeleter<NPT_LogHandler>());
        m_Handlers.Clear();
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_Logger::Log
+---------------------------------------------------------------------*/
void
NPT_Logger::Log(int          level, 
                const char*  source_file,
                unsigned int source_line,
                const char*  source_function,
                const char*  msg, 
                             ...)
{
    // this is a no-op if the log manager is disabled
    if (!LogManager.IsEnabled()) return;
    
    /* check the log level (in case filtering has not already been done) */
    if (level < m_Level) return;

    /* format the message */
    char     buffer[NPT_LOG_STACK_BUFFER_MAX_SIZE];
    NPT_Size buffer_size = sizeof(buffer);
    char*    message = buffer;
    int      result;        
    va_list  args;
    for(;;) {
        /* try to format the message (it might not fit) */
        va_start(args, msg);
        result = NPT_FormatStringVN(message, buffer_size-1, msg, args);
        va_end(args);
        if (result >= (int)(buffer_size-1)) result = -1;
        message[buffer_size-1] = 0; /* force a NULL termination */
        if (result >= 0) break;

        /* the buffer was too small, try something bigger */
        buffer_size = (buffer_size+NPT_LOG_HEAP_BUFFER_INCREMENT)*2;
        if (buffer_size > NPT_LOG_HEAP_BUFFER_MAX_SIZE) break;
        if (message != buffer) delete[] message;
        message = new char[buffer_size];
        if (message == NULL) return;
    }

    /* the message is formatted, publish it to the handlers */
    NPT_LogRecord record;
    NPT_Logger*   logger = this;
    
    /* setup the log record */
    record.m_LoggerName     = logger->m_Name,
    record.m_Level          = level;
    record.m_Message        = message;
    record.m_SourceFile     = source_file;
    record.m_SourceLine     = source_line;
    record.m_SourceFunction = source_function;
    NPT_System::GetCurrentTimeStamp(record.m_TimeStamp);
    record.m_ThreadId       = NPT_Thread::GetCurrentThreadId();

    /* call all handlers for this logger and parents */
    m_Manager.Lock();
    //m_Manager.SetEnabled(false); // prevent recursion
    while (logger) {
        /* call all handlers for the current logger */
        for (NPT_List<NPT_LogHandler*>::Iterator i = logger->m_Handlers.GetFirstItem();
             i;
             ++i) {
            NPT_LogHandler* handler = *i;
            handler->Log(record);
        }

        /* forward to the parent unless this logger does not forward */
        if (logger->m_ForwardToParent) {
            logger = logger->m_Parent;
        } else {
            break;
        }
    }
    //m_Manager.SetEnabled(true);
    m_Manager.Unlock();

    /* free anything we may have allocated */
    if (message != buffer) delete[] message;
}

/*----------------------------------------------------------------------
|   NPT_Logger::AddHandler
+---------------------------------------------------------------------*/
NPT_Result
NPT_Logger::AddHandler(NPT_LogHandler* handler, bool transfer_ownership /* = true */)
{
    /* check parameters */
    if (handler == NULL) return NPT_ERROR_INVALID_PARAMETERS;

	/* keep track of what handlers we won't cleanup */
	if (!transfer_ownership) m_ExternalHandlers.Add(handler);

    return m_Handlers.Add(handler);
}

/*----------------------------------------------------------------------
|   NPT_Logger::SetParent
+---------------------------------------------------------------------*/
NPT_Result
NPT_Logger::SetParent(NPT_Logger* parent)
{
    /* set our new parent */
    m_Parent = parent;

    /* find the first ancestor with its own log level */
    NPT_Logger* logger = this;
    while (logger->m_LevelIsInherited && logger->m_Parent) {
        logger = logger->m_Parent;
    }
    if (logger != this) m_Level = logger->m_Level;

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogNullHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogNullHandler::Create(NPT_LogHandler*& handler)
{
    handler = new NPT_LogNullHandler();
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogNullHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogNullHandler::Log(const NPT_LogRecord& /*record*/)
{
}


NPT_LogHandler::CustomHandlerExternalFunction NPT_LogCustomHandler::s_ExternalFunction = NULL;
/*----------------------------------------------------------------------
|   NPT_LogCustomHandler::SetCustomHandlerFunction
+---------------------------------------------------------------------*/
NPT_Result 
NPT_LogCustomHandler::SetCustomHandlerFunction(CustomHandlerExternalFunction function)
{
    s_ExternalFunction = function;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogCustomHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogCustomHandler::Create(NPT_LogHandler*& handler)
{
    /* allocate a new object */
    NPT_LogCustomHandler* instance = new NPT_LogCustomHandler();
    handler = instance;
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogCustomHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogCustomHandler::Log(const NPT_LogRecord& record)
{
    if (s_ExternalFunction) {
        (*s_ExternalFunction)(&record);
    }
}

/*----------------------------------------------------------------------
|   NPT_LogConsoleHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogConsoleHandler::Create(const char*      logger_name,
                              NPT_LogHandler*& handler)
{
    /* compute a prefix for the configuration of this handler */
    NPT_String logger_prefix = logger_name;
    logger_prefix += ".ConsoleHandler";

    /* allocate a new object */
    NPT_LogConsoleHandler* instance = new NPT_LogConsoleHandler();
    handler = instance;

    /* configure the object */
    NPT_String* colors;
    instance->m_UseColors = NPT_LOG_CONSOLE_HANDLER_DEFAULT_COLOR_MODE;
    colors = LogManager.GetConfigValue(logger_prefix,".colors");
    if (colors) {
        if (NPT_LogManager::ConfigValueIsBooleanTrue(*colors)) {
            instance->m_UseColors = true;
        } else if (NPT_LogManager::ConfigValueIsBooleanFalse(*colors)) {
            instance->m_UseColors = false;
        }
    }

    NPT_String* outputs;
    instance->m_Outputs = OUTPUT_TO_DEBUG;
    outputs = LogManager.GetConfigValue(logger_prefix,".outputs");
    if (outputs) {
        outputs->ToInteger(instance->m_Outputs, true);
    }

    NPT_String* filter;
    instance->m_FormatFilter = 0;
    filter = LogManager.GetConfigValue(logger_prefix,".filter");
    if (filter) {
        filter->ToInteger(instance->m_FormatFilter, true);
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogConsoleHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogConsoleHandler::Log(const NPT_LogRecord& record)
{
    NPT_MemoryStream memory_stream(4096);

    NPT_Log::FormatRecordToStream(record, memory_stream, m_UseColors, m_FormatFilter);
    memory_stream.Write("\0", 1);
    if (m_Outputs & OUTPUT_TO_CONSOLE) {
        NPT_Console::Output((const char*)memory_stream.GetData());
    } 
    if (m_Outputs & OUTPUT_TO_DEBUG) {
        NPT_DebugOutput((const char*)memory_stream.GetData());
    } 
}

/*----------------------------------------------------------------------
|   NPT_LogFileHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogFileHandler::Log(const NPT_LogRecord& record)
{
    if (m_MaxFilesize > 0) {
        /* get current file size */
        NPT_LargeSize size;
        NPT_File::GetSize(m_Filename, size);

        /* time to recycle ? */
        if (size > m_MaxFilesize) {
            /* release stream to force a reopen later 
               and to be able to rename file */
            m_Stream = NULL;

            /* rename file using current time */
            NPT_TimeStamp now;
            NPT_System::GetCurrentTimeStamp(now);
            NPT_String suffix = NPT_DateTime(now, true).ToString(NPT_DateTime::FORMAT_W3C);
            suffix.Replace(':', '_');
            NPT_String new_name = NPT_FilePath::Create(
                NPT_FilePath::DirName(m_Filename),
                NPT_FilePath::BaseName(m_Filename, false) + 
                    "-" + 
                    suffix + 
                    NPT_FilePath::FileExtension(m_Filename));

            NPT_File::Rename(m_Filename, new_name);
        }
    }
    
    /* try to reopen the file if it failed to open 
       previously or if we rotated it */
    if (m_Stream.IsNull()) {
        Open(m_Append);
    }
    
    if (m_Stream.AsPointer()) {
        NPT_Log::FormatRecordToStream(record, *m_Stream, false, m_FormatFilter);
        if (m_Flush) m_Stream->Flush();
    }
}

/*----------------------------------------------------------------------
|   NPT_LogFileHandler::Open
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogFileHandler::Open(bool append /* = true */)
{
    /* reset stream just in case */
    m_Stream = NULL;
            
    /* open the log file */
    NPT_File file(m_Filename);
    NPT_Result result = file.Open(NPT_FILE_OPEN_MODE_CREATE |
                                  NPT_FILE_OPEN_MODE_READ   |
                                  NPT_FILE_OPEN_MODE_WRITE  |
                                  (append?NPT_FILE_OPEN_MODE_APPEND:NPT_FILE_OPEN_MODE_TRUNCATE));
    if (NPT_FAILED(result)) return result;

    NPT_CHECK(file.GetOutputStream(m_Stream));
    /* seek to end */
    if (append) {
        NPT_LargeSize size;
        NPT_CHECK(NPT_File::GetSize(m_Filename, size));
        NPT_CHECK(m_Stream->Seek(size));
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogFileHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogFileHandler::Create(const char*      logger_name,
                           NPT_LogHandler*& handler)
{
    /* compute a prefix for the configuration of this handler */
    NPT_String logger_prefix = logger_name;
    logger_prefix += ".FileHandler";

    /* allocate a new object */
    NPT_LogFileHandler* instance = new NPT_LogFileHandler();
    handler = instance;

    /* filename */
    NPT_String* filename_conf = LogManager.GetConfigValue(logger_prefix, ".filename");
    if (filename_conf) {
        instance->m_Filename = *filename_conf;
    } else if (logger_name[0]) {
        NPT_String filename_synth = logger_name;
        filename_synth += ".log";
        instance->m_Filename = filename_synth;
    } else {
        /* default name for the root logger */
        instance->m_Filename = NPT_CONFIG_DEFAULT_LOG_FILE_HANDLER_FILENAME;
    }

    /* always flush flag */
    NPT_String* flush = LogManager.GetConfigValue(logger_prefix, ".flush");
    if (flush && NPT_LogManager::ConfigValueIsBooleanTrue(*flush)) {
        instance->m_Flush = true;
    } else {
        instance->m_Flush = false;
    }

    /* append mode */
    instance->m_Append = true;
    NPT_String* append_mode = LogManager.GetConfigValue(logger_prefix, ".append");
    if (append_mode && NPT_LogManager::ConfigValueIsBooleanFalse(*append_mode)) {
        instance->m_Append = false;
    }

    /* filter */
    NPT_String* filter;
    instance->m_FormatFilter = 0;
    filter = LogManager.GetConfigValue(logger_prefix,".filter");
    if (filter) {
        filter->ToInteger(instance->m_FormatFilter, true);
    }

    /* recycle */
    NPT_String* recycle;
    instance->m_MaxFilesize = 0;
    recycle = LogManager.GetConfigValue(logger_prefix,".recycle");
    if (recycle) {
        NPT_ParseInteger64(*recycle, instance->m_MaxFilesize, true);
        if (instance->m_MaxFilesize < NPT_LOG_FILE_HANDLER_MIN_RECYCLE_SIZE) {
            instance->m_MaxFilesize = NPT_LOG_FILE_HANDLER_MIN_RECYCLE_SIZE;
        }
    }

    /* open the log file */
    return instance->Open(instance->m_Append);
}

/*----------------------------------------------------------------------
|   NPT_LogTcpHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogTcpHandler::Create(const char* logger_name, NPT_LogHandler*& handler)
{
    /* compute a prefix for the configuration of this handler */
    NPT_String logger_prefix = logger_name;
    logger_prefix += ".TcpHandler";

    /* allocate a new object */
    NPT_LogTcpHandler* instance = new NPT_LogTcpHandler();
    handler = instance;

    /* configure the object */
    const NPT_String* hostname = LogManager.GetConfigValue(logger_prefix, ".hostname");
    if (hostname) {
        instance->m_Host = *hostname;
    } else {
        /* default hostname */
        instance->m_Host = "localhost";
    }
    const NPT_String* port = LogManager.GetConfigValue(logger_prefix, ".port");
    if (port) {
        NPT_UInt32 port_int;
        if (NPT_SUCCEEDED(port->ToInteger(port_int, true))) {
            instance->m_Port = (NPT_UInt16)port_int;
        } else {
            instance->m_Port = NPT_LOG_TCP_HANDLER_DEFAULT_PORT;
        }
    } else {
        /* default port */
        instance->m_Port = NPT_LOG_TCP_HANDLER_DEFAULT_PORT;
    }

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogTcpHandler::Connect
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogTcpHandler::Connect()
{
    /* create a socket */
    NPT_TcpClientSocket tcp_socket;

    /* connect to the host */
    NPT_IpAddress ip_address;
    NPT_CHECK(ip_address.ResolveName(m_Host));
    NPT_Result result = tcp_socket.Connect(NPT_SocketAddress(ip_address, m_Port), 
                                           NPT_LOG_TCP_HANDLER_DEFAULT_CONNECT_TIMEOUT);
    if (NPT_FAILED(result)) {
        return result;
    }

    /* get the stream */
    return tcp_socket.GetOutputStream(m_Stream);
}

/*----------------------------------------------------------------------
|   NPT_LogTcpHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogTcpHandler::FormatRecord(const NPT_LogRecord& record, NPT_String& msg)
{
    /* format the record */
    const char* level_name = NPT_Log::GetLogLevelName(record.m_Level);
    NPT_String  level_string;

    /* format and emit the record */
    if (level_name[0] == '\0') {
        level_string = NPT_String::FromIntegerU(record.m_Level);
        level_name = level_string;
    }
    msg.Reserve(2048);
    msg += "Logger: ";
    msg += record.m_LoggerName;
    msg += "\r\nLevel: ";
    msg += level_name;
    msg += "\r\nSource-File: ";
    msg += record.m_SourceFile;
    msg += "\r\nSource-Function: ";
    msg += record.m_SourceFunction;
    msg += "\r\nSource-Line: ";
    msg += NPT_String::FromIntegerU(record.m_SourceLine);
    msg += "\r\nThread-Id: ";
    msg += NPT_String::FromIntegerU(record.m_ThreadId);
    msg += "\r\nTimeStamp: ";
    msg += NPT_DateTime(record.m_TimeStamp, true).ToString(NPT_DateTime::FORMAT_W3C, 
                                                           NPT_DateTime::FLAG_EMIT_FRACTION | 
                                                           NPT_DateTime::FLAG_EXTENDED_PRECISION);
    msg += "\r\nContent-Length: ";
    msg += NPT_String::FromIntegerU(NPT_StringLength(record.m_Message));
    msg += "\r\n\r\n";
    msg += record.m_Message;
}

/*----------------------------------------------------------------------
|   NPT_LogTcpHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogTcpHandler::Log(const NPT_LogRecord& record)
{
    // ensure we're connected
    if (m_Stream.IsNull()) {
        if (NPT_FAILED(Connect())) return;
    }

    // format the record
    NPT_String msg;
    FormatRecord(record, msg);
    
    // log, and disconnect if this fails
    if (NPT_FAILED(m_Stream->WriteString(msg))) {
        m_Stream = NULL;
    }
}

/*----------------------------------------------------------------------
|   NPT_LogUdpHandler::Create
+---------------------------------------------------------------------*/
NPT_Result
NPT_LogUdpHandler::Create(const char* logger_name, NPT_LogHandler*& handler)
{
    /* compute a prefix for the configuration of this handler */
    NPT_String logger_prefix = logger_name;
    logger_prefix += ".UdpHandler";

    /* allocate a new object */
    NPT_LogUdpHandler* instance = new NPT_LogUdpHandler();
    handler = instance;

    /* configure the object */
    const char*       hostname = "localhost";
    const NPT_String* hostname_prop = LogManager.GetConfigValue(logger_prefix, ".hostname");
    if (hostname_prop) {
        hostname = hostname_prop->GetChars();
    } 
    NPT_UInt32 port = NPT_LOG_UDP_HANDLER_DEFAULT_PORT;
    const NPT_String* port_prop = LogManager.GetConfigValue(logger_prefix, ".port");
    if (port_prop) {
        if (NPT_FAILED(port_prop->ToInteger(port, true))) {
            port = NPT_LOG_UDP_HANDLER_DEFAULT_PORT;
        }
    } 

    // resolve the target hostname
    NPT_IpAddress target_ip;
    target_ip.ResolveName(hostname);
    instance->m_Target.SetIpAddress(target_ip);
    instance->m_Target.SetPort(port);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_LogUdpHandler::Log
+---------------------------------------------------------------------*/
void
NPT_LogUdpHandler::Log(const NPT_LogRecord& record)
{
    // format the record
    NPT_String msg;
    NPT_LogTcpHandler::FormatRecord(record, msg);
    
    // send it in a datagram
    NPT_DataBuffer buffer(msg.GetChars(), msg.GetLength()+1, false);
    m_Socket.Send(buffer, &m_Target);
}

/*----------------------------------------------------------------------
|   NPT_HttpLoggerConfigurator::NPT_HttpLoggerConfigurator
+---------------------------------------------------------------------*/
NPT_HttpLoggerConfigurator::NPT_HttpLoggerConfigurator(NPT_UInt16 port, bool detached) :
    NPT_Thread(detached)
{
    // create the server
    m_Server = new NPT_HttpServer(port);

    // attach a handler to response to the requests
    m_Server->AddRequestHandler(this, "/", true);
}

/*----------------------------------------------------------------------
|   NPT_HttpLoggerConfigurator::~NPT_HttpLoggerConfigurator
+---------------------------------------------------------------------*/
NPT_HttpLoggerConfigurator::~NPT_HttpLoggerConfigurator()
{
    // TODO: send a command to the server to tell it to abort
    
    // cleanup
    delete m_Server;
}

/*----------------------------------------------------------------------
|   NPT_HttpLoggerConfigurator::SetupResponse
+---------------------------------------------------------------------*/
NPT_Result
NPT_HttpLoggerConfigurator::SetupResponse(NPT_HttpRequest&              request,
                                          const NPT_HttpRequestContext& /*context*/,
                                          NPT_HttpResponse&             response)
{
    // we only support GET here
    if (request.GetMethod() != NPT_HTTP_METHOD_GET) return NPT_ERROR_HTTP_METHOD_NOT_SUPPORTED;

    // construct the response message
    NPT_String msg;
    
    msg = "<ul>";
    NPT_List<NPT_LogConfigEntry>& config = LogManager.GetConfig();
    NPT_List<NPT_LogConfigEntry>::Iterator cit = config.GetFirstItem();
    for (; cit; ++cit) {
        NPT_LogConfigEntry& entry = (*cit);
        msg += "<li>";
        msg += entry.m_Key;
        msg += "=";
        msg += entry.m_Value;
        msg += "</li>";
    }
    msg += "</ul>";

    msg += "<ul>";
    NPT_List<NPT_Logger*>& loggers = LogManager.GetLoggers();
    NPT_List<NPT_Logger*>::Iterator lit = loggers.GetFirstItem();
    for (;lit;++lit) {
        NPT_Logger* logger = (*lit);
        msg += "<li>";
        msg += logger->GetName();
        msg += ", level=";
        msg += NPT_String::FromInteger(logger->GetLevel());

        NPT_List<NPT_LogHandler*>& handlers = logger->GetHandlers();
        NPT_List<NPT_LogHandler*>::Iterator hit = handlers.GetFirstItem();
        msg += ", handlers=";
        for (;hit;++hit) {
            NPT_LogHandler* handler = (*hit);
            msg += handler->ToString();
        }
        msg += "</li>";
    }
    msg += "</ul>";

    // setup the response body
    NPT_HttpEntity* entity = response.GetEntity();
    entity->SetContentType("text/html");
    entity->SetInputStream(msg);

    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_HttpLoggerConfigurator::Run
+---------------------------------------------------------------------*/
void
NPT_HttpLoggerConfigurator::Run()
{
    for (;;) {
        NPT_Result result;
        result = m_Server->Loop();
        if (NPT_FAILED(result)) {
            break;
        }
    }
}
