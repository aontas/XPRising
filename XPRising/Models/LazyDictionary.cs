using System.Collections.Generic;

namespace XPRising.Models;

public class LazyDictionary<TKey,TValue> : Dictionary<TKey,TValue> where TValue : new()
{
    public new TValue this[TKey key]
    {
        get 
        {
            if (!base.ContainsKey(key)) base.Add(key, new TValue());
            return base[key];
        }
        set 
        {
            if (!base.ContainsKey(key)) base.Add(key, value);
            else base[key] = value;
        }
    }
}