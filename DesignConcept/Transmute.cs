namespace TransmuteExtensions
{
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
