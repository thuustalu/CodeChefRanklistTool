using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeChefRanklistTool
{
    public static class RandomSampler
    {
        /// <summary>
        ///     Picks <paramref name="requiredLength" /> items from <paramref name="items" />,
        ///     preserving the original order.
        /// </summary>
        public static IEnumerable<T> Sample<T>(IEnumerable<T> items, int requiredLength)
        {
            var itemsList = items.ToList();

            var rnd = new Random();
            var indicesToTake = Enumerable.Range(0, itemsList.Count)
                .OrderBy(x => rnd.Next())
                .Take(requiredLength).ToList();
            indicesToTake.Sort();

            foreach (var index in indicesToTake) yield return itemsList[index];
        }
    }
}