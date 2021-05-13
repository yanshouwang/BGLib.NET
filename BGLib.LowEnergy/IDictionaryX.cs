using System.Collections.Generic;

namespace BGLib.LowEnergy
{
    internal static class IDictionaryX
    {
#if NET461
        public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            dictionary.TryGetValue(key, out value);
            return dictionary.Remove(key);
        }
#endif
    }
}
