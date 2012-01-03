using System.Collections.Generic;

namespace Gurpenator
{
    public static class Util
    {
        public static IEnumerable<T> chain<T>(this IEnumerable<IEnumerable<T>> itemses)
        {
            foreach (var items in itemses)
                foreach (var item in items)
                    yield return item;
        }
    }
}
