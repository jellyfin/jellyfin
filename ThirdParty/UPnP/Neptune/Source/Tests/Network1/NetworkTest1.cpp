/*****************************************************************
|
|      Network Test Program 1
|
|      (c) 2001-2012 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "Neptune.h"

#if defined(WIN32) && defined(_DEBUG)
#include <crtdbg.h>
#endif

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#define CHECK(x) do { if (!(x)) NPT_Console::OutputF("FAILED line %d\n", __LINE__); } while(0)

/*----------------------------------------------------------------------
|   functions
+---------------------------------------------------------------------*/
static volatile bool NeedToStop = false;

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
class Resolver : public NPT_Thread
{
public:
    Resolver(const char* name, NPT_IpAddress addr) : m_Result(NPT_SUCCESS), m_Name(name), m_Addr(addr) {}
    
    virtual void Run() {
        while (!NeedToStop) {
            NPT_IpAddress addr;
            m_Result = addr.ResolveName(m_Name);
            if (NPT_FAILED(m_Result)) {
                NPT_Console::OutputF("ERROR: ResolveName failed (%d)\n", m_Result);
                return;
            }
            if (!(addr == m_Addr)) {
                m_Result = NPT_FAILURE;
                NPT_Console::OutputF("ERROR: wrong IP address (%s instead of %s for %s)\n", addr.ToString().GetChars(), m_Addr.ToString().GetChars(), m_Name.GetChars());
                return;
            }
        }
    }
    
private:
    NPT_Result    m_Result;
    NPT_String    m_Name;
    NPT_IpAddress m_Addr;
};

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    // setup debugging
#if defined(WIN32) && defined(_DEBUG)
    int flags = _crtDbgFlag       | 
        _CRTDBG_ALLOC_MEM_DF      |
        _CRTDBG_DELAY_FREE_MEM_DF |
        _CRTDBG_CHECK_ALWAYS_DF;

    _CrtSetDbgFlag(flags);
    //AllocConsole();
    //freopen("CONOUT$", "w", stdout);
#endif 

    
    NPT_IpAddress addr;
    NPT_Result result;
    
    result = addr.ResolveName("www.perdu.com");
    CHECK(NPT_SUCCEEDED(result));
    Resolver resolver1("www.perdu.com", addr);

    result = addr.ResolveName("zebulon.bok.net");
    CHECK(NPT_SUCCEEDED(result));
    Resolver resolver2("zebulon.bok.net", addr);
    
    resolver1.Start();
    resolver2.Start();
    
    NPT_System::Sleep(10.0);
    NeedToStop = true;
    resolver1.Wait();
    resolver2.Wait();
    
#if defined(WIN32) && defined(_DEBUG)
    _CrtDumpMemoryLeaks();
#endif

    return 0;
}
