/*****************************************************************
|
|      Streams Test Program 1
|
|      (c) 2005-2006 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "Neptune.h"
#include "NptDebug.h"

/*----------------------------------------------------------------------
|       main
+---------------------------------------------------------------------*/
int
main(int /*argc*/, char** /*argv*/)
{
    NPT_StringOutputStream sos;
    NPT_ASSERT(sos.GetString().GetLength() == 0);
    sos.WriteString("hello");
    NPT_ASSERT(sos.GetString() == "hello");
    sos.Reset();
    NPT_ASSERT(sos.GetString().GetLength() == 0);
    
    NPT_MemoryStream mems;
    NPT_ASSERT(mems.GetDataSize() == 0);
    NPT_LargeSize available;
    NPT_ASSERT(mems.GetAvailable(available) == NPT_SUCCESS);
    NPT_ASSERT(available == 0);
    NPT_COMPILER_UNUSED(available);
    
    mems.WriteUI08(1);
    mems.WriteUI16(0x0203);
    mems.WriteUI24(0x040506);
    mems.WriteUI32(0x0708090A);
    mems.WriteUI64( (((NPT_UInt64)0x0B0C0D0E)<<32) | (((NPT_UInt64)0x0F000102)) );
    NPT_ASSERT(mems.GetAvailable(available) == NPT_SUCCESS);
    NPT_ASSERT(available == 18);
    NPT_ASSERT(mems.GetSize(available) == NPT_SUCCESS);
    NPT_ASSERT(available == 18);
    NPT_ASSERT(mems.GetDataSize() == 18);

    for (int i=0; i<18; i++) {
        NPT_ASSERT(mems.GetData()[i] == (1+i)%16);
    }
    
    NPT_UInt64 i64;
    NPT_UInt32 i32;
    NPT_UInt32 i24;
    NPT_UInt16 i16;
    NPT_UInt8  i8;
    mems.ReadUI08(i8);
    mems.ReadUI16(i16);
    mems.ReadUI24(i24);
    mems.ReadUI32(i32);
    mems.ReadUI64(i64);
    NPT_ASSERT(i8  == 1);
    NPT_ASSERT(i16 == 0x0203);
    NPT_ASSERT(i24 == 0x040506);
    NPT_ASSERT(i32 == 0x0708090A);
    NPT_ASSERT(i64 == ((((NPT_UInt64)0x0B0C0D0E)<<32) | (((NPT_UInt64)0x0F000102))));
    NPT_ASSERT(mems.GetDataSize() == 18);
    NPT_ASSERT(mems.GetAvailable(available) == NPT_SUCCESS);
    NPT_ASSERT(available == 0);
    
    return 0;
}
