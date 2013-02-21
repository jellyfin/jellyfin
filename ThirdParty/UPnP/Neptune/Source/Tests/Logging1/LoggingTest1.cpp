/*****************************************************************
|
|   File: LoggingTest.c
|
|   Atomix Tests - Logging Test
|
|   (c) 2002-2006 Gilles Boccon-Gibod
|   Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|  includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

NPT_DEFINE_LOGGER(MyLogger, "neptune.test.my")
NPT_DEFINE_LOGGER(FooLogger, "neptune.test.foo")
NPT_SET_LOCAL_LOGGER("neptune.test")

/*----------------------------------------------------------------------
|  TestCheck functions
+---------------------------------------------------------------------*/
static NPT_Result TestCheck(void)
{
    NPT_CHECK_L(NPT_LOG_LEVEL_WARNING, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckSevere(void)
{
    NPT_CHECK_SEVERE(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckWarning(void)
{
    NPT_CHECK_WARNING(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckInfo(void)
{
    NPT_CHECK_INFO(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckFine(void)
{
    NPT_CHECK_FINE(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckFiner(void)
{
    NPT_CHECK_FINER(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckFinest(void)
{
    NPT_CHECK_FINEST(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}

static NPT_Result TestCheckL(void)
{
    NPT_CHECK_LL(FooLogger, NPT_LOG_LEVEL_WARNING, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckSevereL(void)
{
    NPT_CHECK_SEVERE(NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckWarningL(void)
{
    NPT_CHECK_WARNING_L(FooLogger, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckInfoL(void)
{
    NPT_CHECK_INFO_L(FooLogger, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckFineL(void)
{
    NPT_CHECK_FINE_L(FooLogger, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckFinerL(void)
{
    NPT_CHECK_FINER_L(FooLogger, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}
static NPT_Result TestCheckFinestL(void)
{
    NPT_CHECK_FINEST_L(FooLogger, NPT_FAILURE);
    NPT_LOG_SEVERE("###");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|  TestLargeBuffer
+---------------------------------------------------------------------*/
static void
TestLargeBuffer(void)
{
    char* buffer = new char[32768];
    int i;
    for (i=0; i<32768; i++) {
        buffer[i] = 'a';
    }
    buffer[32767] = 0;
    NPT_LOG_SEVERE(buffer);
    delete[] buffer;
}

/*----------------------------------------------------------------------
|  main
+---------------------------------------------------------------------*/
int 
main(int, char**)
{
    NPT_LOG_L(MyLogger, NPT_LOG_LEVEL_WARNING, "blabla");
    NPT_LOG_L2(MyLogger, NPT_LOG_LEVEL_WARNING, "blabla %d %d", 8, 9);

    NPT_LOG(NPT_LOG_LEVEL_WARNING, "blibli");
    NPT_LOG_2(NPT_LOG_LEVEL_INFO, "fofo %d %d", 5, 7);

    NPT_LOG_SEVERE("this is severe!");
    NPT_LOG_SEVERE_1("this is severe (%d)", 9);

    NPT_LOG_SEVERE_L(MyLogger, "this is severe!");
    NPT_LOG_SEVERE_L1(MyLogger, "this is severe (%d)", 9);

    NPT_LOG_SEVERE_L(FooLogger, "this is severe!");
    NPT_LOG_SEVERE_L1(FooLogger, "this is severe (%d)", 9);

    NPT_LOG_SEVERE("severe");
    NPT_LOG_WARNING("warning");
    NPT_LOG_INFO("info");
    NPT_LOG_FINE("fine");
    NPT_LOG_FINER("finer");
    NPT_LOG_FINEST("finest");

    NPT_LOG_SEVERE_L(FooLogger, "severe");
    NPT_LOG_WARNING_L(FooLogger, "warning");
    NPT_LOG_INFO_L(FooLogger, "info");
    NPT_LOG_FINE_L(FooLogger, "fine");
    NPT_LOG_FINER_L(FooLogger, "finer");
    NPT_LOG_FINEST_L(FooLogger, "finest");

    TestLargeBuffer();

    TestCheck();
    TestCheckSevere();
    TestCheckWarning();
    TestCheckInfo();
    TestCheckFine();
    TestCheckFiner();
    TestCheckFinest();

    TestCheckL();
    TestCheckSevereL();
    TestCheckWarningL();
    TestCheckInfoL();
    TestCheckFineL();
    TestCheckFinerL();
    TestCheckFinestL();

    return 0;
}

