using System.Collections.Generic;

namespace IELTS_Learning_Tool.Utils
{
    public static class EnumerableHelper
    {
        // Helper to get index in a foreach loop
        public static IEnumerable<(T item, int index)> WithIndex<T>(IEnumerable<T> source)
        {
            int i = 0;
            foreach (var item in source)
            {
                yield return (item, i++);
            }
        }
    }
}

