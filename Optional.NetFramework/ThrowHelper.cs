using System;

namespace Optional.NetFramework
{
    internal static class ThrowHelper
    {
        public static Exception InvalidAlternativeFactoryReference(string paramName)
            => new ArgumentNullException(paramName,
                "An invalid reference to an AlternativeFactory object was specified when calling this method!");
    }
}
