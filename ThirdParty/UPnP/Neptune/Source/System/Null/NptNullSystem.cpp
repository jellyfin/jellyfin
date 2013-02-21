/*****************************************************************
|
|      Neptune - System :: Null Implementation
|
|      (c) 2001-2003 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptSystem.h"
#include "NptResults.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|       globals
+---------------------------------------------------------------------*/
NPT_System System;

/*----------------------------------------------------------------------
|       NPT_NullSystem
+---------------------------------------------------------------------*/
class NPT_NullSystem : public NPT_SystemInterface
{
public:
    // methods
    NPT_NullSystem() {}
   ~NPT_NullSystem(){}
    NPT_Result  GetProcessId(NPT_Integer& id) { 
        id = 0; 
        return NPT_SUCCESS; 
    }
    NPT_Result  GetCurrentTimeStamp(NPT_TimeStamp& now) {
        now = 0.0f;
        return NPT_SUCCESS;
    }
    NPT_Result  Sleep(const NPT_TimeInterval& /*duration*/) {
        return NPT_FAILURE;
    }
    NPT_Result  SleepUntil(const NPT_TimeStamp& /*when*/) {
        return NPT_FAILURE;
    }
    NPT_Result  SetRandomSeed(unsigned int /*seed*/) {
        return NPT_SUCCESS;
    }
    NPT_Integer GetRandomInteger() {
        return 0;
    }
};

/*----------------------------------------------------------------------
|       NPT_System::NPT_System
+---------------------------------------------------------------------*/
NPT_System::NPT_System()
{
    m_Delegate = new NPT_NullSystem();
}

