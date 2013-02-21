/*****************************************************************
|
|   Neptune - Dynamic Cast Support
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

#ifndef _NPT_DYNAMIC_CAST_H_
#define _NPT_DYNAMIC_CAST_H_

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptTypes.h"
#include "NptCommon.h"
#include "NptResults.h"
#include "NptConfig.h"

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
#if defined(NPT_CONFIG_NO_RTTI)
#define NPT_DYNAMIC_CAST(_class,_object) \
( ((_object)==0) ? 0 : reinterpret_cast<_class*>((_object)->DynamicCast(&_class::_class_##_class)) )
#define NPT_IMPLEMENT_DYNAMIC_CAST(_class)              \
static int _class_##_class;                             \
virtual void* DynamicCast(const void* class_anchor) {   \
    if (class_anchor ==  &_class::_class_##_class) {    \
        return static_cast<_class*>(this);              \
    }                                                   \
    return NULL;                                        \
}
#define NPT_IMPLEMENT_DYNAMIC_CAST_D(_class,_superclass)\
static int _class_##_class;                             \
virtual void* DynamicCast(const void* class_anchor) {   \
    if (class_anchor ==  &_class::_class_##_class) {    \
        return static_cast<_class*>(this);              \
    } else {                                            \
        return _superclass::DynamicCast(class_anchor);  \
    }                                                   \
}
#define NPT_IMPLEMENT_DYNAMIC_CAST_D2(_class,_superclass,_mixin)\
static int _class_##_class;                                     \
virtual void* DynamicCast(const void* class_anchor) {           \
    if (class_anchor ==  &_class::_class_##_class) {            \
        return static_cast<_class*>(this);                      \
    } else {                                                    \
        void* sup = _superclass::DynamicCast(class_anchor);     \
        if (sup) return sup;                                    \
        return _mixin::DynamicCast(class_anchor);               \
    }                                                           \
}
#define NPT_DEFINE_DYNAMIC_CAST_ANCHOR(_class) int _class::_class_##_class = 0;

#else

#define NPT_DYNAMIC_CAST(_class,_object) dynamic_cast<_class*>(_object)
#define NPT_IMPLEMENT_DYNAMIC_CAST(_class)
#define NPT_IMPLEMENT_DYNAMIC_CAST_D(_class,_superclass)
#define NPT_IMPLEMENT_DYNAMIC_CAST_D2(_class,_superclass,_mixin)
#define NPT_DEFINE_DYNAMIC_CAST_ANCHOR(_class)

#endif

#endif // _NPT_DYNAMIC_CAST_H_
