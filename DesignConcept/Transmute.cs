namespace TransmuteExtensions
{
    /// <summary>
    /// Code by Theodoros Chatzigiannakis 
    /// https://blog.tchatzigiannakis.com/changing-an-objects-type-at-runtime-in-c-sharp/
    ///
    /// Transmute one type to another.
    /// eg.
    ///     MyInterface is a copy of the code of Dynamic.Interface.
    ///     MyInterface.TransmuteTo(Dynamic.Interface) should cause all my MyInterfaces to point to Dynamic.Interface, and return the correct DI value.
    ///     (that's the theory, anyway!)
    /// </summary>
    public unsafe static class Transmute
    {
        public static unsafe void* GetObjectAddress(this object obj) => *(void**)Unsafe.AsPointer(ref obj);

        public static void TransmuteTo(this object target, object source)
        {
            if (target.GetType() == source.GetType()) return;

            var s = (void**)source.GetObjectAddress();
            var t = (void**)target.GetObjectAddress();
            *t = *s;

            if (target.GetType() != source.GetType())
                throw new AccessViolationException($"Illegal write to address {new IntPtr(t)}");
        }

        public static T TransmuteTo<T>(this object target, T source)
        {
            target.TransmuteTo((object)source);
            return (T)target;
        }

        public static T TransmuteTo<T>(this object target) where T : new() => target.TransmuteTo(new T());
    }
}
