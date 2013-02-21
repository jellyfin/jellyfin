/*****************************************************************
|
|      Neptune - Threads :: Null Implementation
|
|      (c) 2001-2002 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|       includes
+---------------------------------------------------------------------*/
#include "NptConfig.h"
#include "NptTypes.h"
#include "NptThreads.h"

/*----------------------------------------------------------------------
|       NPT_NullAtomicVariable
+---------------------------------------------------------------------*/
class NPT_NullAtomicVariable : public NPT_AtomicVariableInterface
{
 public:
    // methods
    NPT_NullAtomicVariable(NPT_Integer value) : m_Value(value) {}
   ~NPT_NullAtomicVariable() {}
    NPT_Integer Increment() { return ++m_Value; }
    NPT_Integer Decrement() { return --m_Value; }
    NPT_Integer GetValue() { return m_Value; }
    void        SetValue(NPT_Integer value) { m_Value = value; }

 private:
    // members
    volatile NPT_Integer m_Value;
};

/*----------------------------------------------------------------------
|       NPT_AtomicVariable::NPT_AtomicVariable
+---------------------------------------------------------------------*/
NPT_AtomicVariable::NPT_AtomicVariable(NPT_Integer value)
{
    m_Delegate = new NPT_NullAtomicVariable(value);
}






