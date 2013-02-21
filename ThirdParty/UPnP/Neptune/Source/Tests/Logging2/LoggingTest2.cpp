/*****************************************************************
|
|   File: LoggingTest2.c
|
|   Atomix Tests - Logging Test
|
|   (c) 2002-2008 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|  includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

/*----------------------------------------------------------------------
|  main
+---------------------------------------------------------------------*/
int 
main(int, char**)
{
    NPT_LogManager::GetDefault().Configure("plist:hop.level=INFO;");

    NPT_HttpLoggerConfigurator* server = new NPT_HttpLoggerConfigurator();
    server->Start();

    NPT_Logger* loggers[16];
    loggers[ 0] = NPT_LogManager::GetLogger("test.log.one");
    loggers[ 1] = NPT_LogManager::GetLogger("test.log.two");
    loggers[ 2] = NPT_LogManager::GetLogger("test.log.three");
    loggers[ 3] = NPT_LogManager::GetLogger("test.foo.bla.bli");
    loggers[ 4] = NPT_LogManager::GetLogger("test.bar");
    loggers[ 5] = NPT_LogManager::GetLogger("test.bar.one");
    loggers[ 6] = NPT_LogManager::GetLogger("test.bar.two");
    loggers[ 7] = NPT_LogManager::GetLogger("test.bar.three");
    loggers[ 8] = NPT_LogManager::GetLogger("hop");
    loggers[ 9] = NPT_LogManager::GetLogger("hop.hop.hop.boom");
    loggers[10] = NPT_LogManager::GetLogger("kiki");
    loggers[11] = NPT_LogManager::GetLogger("koko");
    loggers[12] = NPT_LogManager::GetLogger("kaka.coucou");
    loggers[13] = NPT_LogManager::GetLogger("kaka.test.coucou");
    loggers[14] = NPT_LogManager::GetLogger("kaka.kaka");
    loggers[15] = NPT_LogManager::GetLogger("kuku");

    for (;;) {
        NPT_System::Sleep(NPT_TimeInterval(1.0f));
        for (unsigned int i=0; i<sizeof(loggers)/sizeof(loggers[0]); i++) {
            NPT_LoggerReference logger = { loggers[i], "test" };
            int level = NPT_System::GetRandomInteger()%800;
            NPT_LOG_L2(logger, level, "hello from logger %d, level %d", i, level);
        }
    }

    server->Wait();
    delete server;

    return 0;
}

