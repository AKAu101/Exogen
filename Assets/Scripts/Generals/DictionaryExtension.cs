using System.Collections.Generic;

public static class IDictionaryExtension
{
    public static bool SwapEntries<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey first, TKey second)
    {
        if (!dict.ContainsKey(first) || !dict.ContainsKey(second)) return false;

        // If trying to swap the same key, do nothing
        if (EqualityComparer<TKey>.Default.Equals(first, second)) return true;

        var entryAtSecond = dict[second];
        var entryAtFirst = dict[first];

        dict.Remove(first);
        dict.Remove(second);
        dict.Add(first, entryAtSecond);
        dict.Add(second, entryAtFirst);

        return true;
    }

    public static bool SwapEntries<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey first, TKey second,IDictionary<TKey,TValue> other)
    {
        if (!dict.ContainsKey(first) || !dict.ContainsKey(second)) return false;

        // If trying to swap the same key, do nothing
        if (EqualityComparer<TKey>.Default.Equals(first, second)) return true;

        //var entryAtSecond = dict[second];
        //var entryAtFirst = dict[first];

        //swapping across two different inventories
        if (!dict.ContainsKey(first) || !other.ContainsKey(second)) return false;


        var entryAtSecond = other[second];
        var entryAtFirst = dict[first];

        dict.Remove(first);
        other.Remove(second);
        dict.Add(first, entryAtSecond);
        other.Add(second, entryAtFirst);

        return true;
    }

}