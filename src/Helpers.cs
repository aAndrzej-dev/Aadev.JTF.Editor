using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aadev.JTF.Editor
{
    public static class Helpers
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

    }
}
