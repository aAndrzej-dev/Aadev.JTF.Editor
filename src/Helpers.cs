using System;
using System.Collections.Generic;

namespace Aadev.JTF.Editor
{
    internal static class Helpers
    {
        public static T? FirstOrNull<T>(this IEnumerable<T> source, Func<T, bool> predicate) where T : struct
        {
            foreach (T item in source)
            {
                if (predicate(item))
                    return item;
            }
            return null;
        }

        public delegate void ControlDelegate();
    }
}
