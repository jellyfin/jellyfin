/*****************************************************************
|
|   Platinum - Managed NeptuneLoggingBridge
|
| Copyright (c) 2004-2010, Plutinosoft, LLC.
| All rights reserved.
| http://www.plutinosoft.com
|
| This program is free software; you can redistribute it and/or
| modify it under the terms of the GNU General Public License
| as published by the Free Software Foundation; either version 2
| of the License, or (at your option) any later version.
|
| OEMs, ISVs, VARs and other distributors that combine and 
| distribute commercially licensed software with Platinum software
| and do not wish to distribute the source code for the commercially
| licensed software under version 2, or (at your option) any later
| version, of the GNU General Public License (the "GPL") must enter
| into a commercial license agreement with Plutinosoft, LLC.
| 
| This program is distributed in the hope that it will be useful,
| but WITHOUT ANY WARRANTY; without even the implied warranty of
| MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
| GNU General Public License for more details.
|
| You should have received a copy of the GNU General Public License
| along with this program; see the file LICENSE.txt. If not, write to
| the Free Software Foundation, Inc., 
| 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
| http://www.gnu.org/licenses/gpl-2.0.html
|
****************************************************************/
#pragma once

using namespace log4net;

namespace Platinum
{

/*----------------------------------------------------------------------
|   NeptuneLoggingBridge
+---------------------------------------------------------------------*/
class NeptuneLoggingBridge : NPT_LogHandler
{
private:

	NPT_Mutex m_SetLoggerNameLock;
	gcroot<String^> m_pFormatString;
	gcroot<ILog^> m_pLogger;

public:

	static void Configure()
	{
		static NPT_Mutex lock;

		lock.Lock();

		static NeptuneLoggingBridge instance;

		// clear config
		NPT_LogManager::GetDefault().Configure("plist:.level=ALL;.handlers=;platinum.level=ALL;platinum.handlers=");

		// get root logger
		NPT_Logger* rootLogger = NPT_LogManager::GetLogger("platinum");

		if (rootLogger)
		{
			// set handler
			rootLogger->AddHandler(&instance, false);
		}

		lock.Unlock();
	}

public:

	virtual void Log(const NPT_LogRecord& record)
	{
		gcroot<ILog^> log = SetLoggerName(record.m_LoggerName);

		switch (record.m_Level)
		{
			case NPT_LOG_LEVEL_FATAL:
				if (log->IsFatalEnabled)
				{
					log->FatalFormat(
						m_pFormatString,
						marshal_as<String^>(NPT_Log::GetLogLevelName(record.m_Level)),
						marshal_as<String^>(record.m_Message),
						marshal_as<String^>(record.m_SourceFile),
						UInt32(record.m_SourceLine)
						);
				}

				break;

			case NPT_LOG_LEVEL_SEVERE:
				if (log->IsErrorEnabled)
				{
					log->ErrorFormat(
						m_pFormatString,
						marshal_as<String^>(NPT_Log::GetLogLevelName(record.m_Level)),
						marshal_as<String^>(record.m_Message),
						marshal_as<String^>(record.m_SourceFile),
						UInt32(record.m_SourceLine)
						);
				}

				break;

			case NPT_LOG_LEVEL_WARNING:
				if (log->IsWarnEnabled)
				{
					log->WarnFormat(
						m_pFormatString,
						marshal_as<String^>(NPT_Log::GetLogLevelName(record.m_Level)),
						marshal_as<String^>(record.m_Message),
						marshal_as<String^>(record.m_SourceFile),
						UInt32(record.m_SourceLine)
						);
				}

				break;

			case NPT_LOG_LEVEL_INFO:
				if (log->IsInfoEnabled)
				{
					log->InfoFormat(
						m_pFormatString,
						marshal_as<String^>(NPT_Log::GetLogLevelName(record.m_Level)),
						marshal_as<String^>(record.m_Message),
						marshal_as<String^>(record.m_SourceFile),
						UInt32(record.m_SourceLine)
						);
				}

				break;

			case NPT_LOG_LEVEL_FINE:
			case NPT_LOG_LEVEL_FINER:
			case NPT_LOG_LEVEL_FINEST:
				if (log->IsDebugEnabled)
				{
					log->DebugFormat(
						m_pFormatString,
						marshal_as<String^>(NPT_Log::GetLogLevelName(record.m_Level)),
						marshal_as<String^>(record.m_Message),
						marshal_as<String^>(record.m_SourceFile),
						UInt32(record.m_SourceLine)
						);
				}

				break;

			default:
				if (log->IsWarnEnabled)
				{
					log->WarnFormat(
						m_pFormatString,
						marshal_as<String^>("UNKNOWN_LOG_LEVEL"),
						marshal_as<String^>(record.m_Message),
						marshal_as<String^>(record.m_SourceFile),
						UInt32(record.m_SourceLine)
						);
				}

				break;
		}
	}

private:

	gcroot<ILog^> SetLoggerName(const char* name)
	{
		m_SetLoggerNameLock.Lock();

		gcroot<String^> loggerName = gcnew String(name);
		gcroot<ILog^> logger = m_pLogger;

		if (logger->Logger->Name != loggerName)
		{
			logger = LogManager::GetLogger(loggerName);

			m_pLogger = logger;
		}

		m_SetLoggerNameLock.Unlock();

		return logger;
	}

public:

	NeptuneLoggingBridge()
	{
		m_pLogger = LogManager::GetLogger(gcnew String("NeptuneLoggingBridge"));
		m_pFormatString = gcnew String("{0}: {2}:{3}: {1}");
	}

	virtual ~NeptuneLoggingBridge()
	{
	}

};

}
