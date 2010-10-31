using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NuGet {
    public static class CollectionExtensions {

        public static void AddRange<T>(this Collection<T> collection, IEnumerable<T> items) {
            foreach (var item in items) {
                collection.Add(item);
            }
        }

        public static int RemoveAll<T>(this Collection<T> collection, Func<T, bool> match) {
            IList<T> toRemove = collection.Where(match).ToList();
            foreach (var item in toRemove) {
                collection.Remove(item);
            }
            return toRemove.Count;
        }
    }
}
