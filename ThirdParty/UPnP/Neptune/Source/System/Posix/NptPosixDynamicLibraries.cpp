/*****************************************************************
|
|      Neptune - Dynamic Libraries :: Posix Implementation
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptLogging.h"
#include "NptDynamicLibraries.h"

#include <dlfcn.h>
#ifndef RTLD_LOCAL
#define RTLD_LOCAL 0
#endif

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
NPT_SET_LOCAL_LOGGER("neptune.posix.dynamic-libraries")

/*----------------------------------------------------------------------
|   NPT_PosixDynamicLibrary
+---------------------------------------------------------------------*/
class NPT_PosixDynamicLibrary : public NPT_DynamicLibraryInterface
{
public:
    // constructor and destructor
    NPT_PosixDynamicLibrary(void* library, const char* name) : 
        m_Library(library), m_Name(name) {}
    
    // NPT_DynamicLibraryInterface methods
    virtual NPT_Result FindSymbol(const char* name, void*& symbol);
    virtual NPT_Result Unload();
    
private:
    // members
    void*      m_Library;
    NPT_String m_Name;
};

/*----------------------------------------------------------------------
|   NPT_DynamicLibrary::Load
+---------------------------------------------------------------------*/
NPT_Result 
NPT_DynamicLibrary::Load(const char* name, NPT_Flags flags, NPT_DynamicLibrary*& library)
{
    if (name == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    
    // default return value
    library = NULL;
    
    // compute the mode
    int mode = RTLD_LOCAL;
    if (flags & NPT_DYANMIC_LIBRARY_LOAD_FLAG_NOW) {
        mode |= RTLD_NOW;
    } else {
        mode |= RTLD_LAZY;
    }

    // load the lib
    NPT_LOG_FINE_3("loading library %s, flags=%x, mode=%x", name, flags, mode);
    void* handle = dlopen(name, mode);
    if (handle == NULL) {
#if defined(NPT_CONFIG_ENABLE_LOGGING)
        const char* error = dlerror();
        NPT_LOG_FINE_1("library cannot be loaded (%s)", error?error:"");
#endif
        return NPT_FAILURE;
    }
    
    // instantiate the object
    NPT_LOG_FINE_1("library %s loaded", name);
    library = new NPT_DynamicLibrary(new NPT_PosixDynamicLibrary(handle, name));
    
    return NPT_SUCCESS;
}
    
/*----------------------------------------------------------------------
|   NPT_PosixDynamicLibrary::FindSymbol
+---------------------------------------------------------------------*/
NPT_Result 
NPT_PosixDynamicLibrary::FindSymbol(const char* name, void*& symbol)
{
    if (name == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    symbol = NULL;
    if (m_Library == NULL) return NPT_ERROR_NO_SUCH_ITEM;
    
    NPT_LOG_FINE_1("finding symbol %s", name);
    symbol = dlsym(m_Library, name);
    return symbol?NPT_SUCCESS:NPT_ERROR_NO_SUCH_ITEM;
}

/*----------------------------------------------------------------------
|   NPT_PosixDynamicLibrary::Unload
+---------------------------------------------------------------------*/
NPT_Result
NPT_PosixDynamicLibrary::Unload()
{
    NPT_LOG_FINE_1("unloading library %s", (const char*)m_Name);
    int result = dlclose(m_Library);
    if (result == 0) {
        return NPT_SUCCESS;
    } else {
        return NPT_FAILURE;
    }
}
