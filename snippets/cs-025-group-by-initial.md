---
id: cs-025
title: Group words by first letter
tags: [collections]
modes: [trace, recall, blank]
spec: Group words by their first letter into a SortedDictionary<char, List<string>>, keeping input order within each group.
tests:
  - call: 'string.Join(";", GroupByInitial(new[] {"apple", "avocado", "banana"}).Select(kv => kv.Key + "=" + string.Join(",", kv.Value)))'
    expect: 'a=apple,avocado;b=banana'
  - call: 'GroupByInitial(new string[0]).Count'
    expect: '0'
---
public static SortedDictionary<char, List<string>> GroupByInitial(IEnumerable<string> words)
{
    var groups = new SortedDictionary<char, List<string>>();
    foreach (var word in words)
    {
        if (!groups.TryGetValue(word[0], out var list)) groups[word[0]] = list = new List<string>();
        list.Add(word);
    }
    return groups;
}
