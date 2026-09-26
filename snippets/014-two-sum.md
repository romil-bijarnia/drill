---
id: 014
title: Two sum
tags: [algorithms, collections]
modes: [trace, recall, blank]
spec: Return the indices of the two numbers that add up to target, lower index first, or an empty array.
tests:
  - call: 'string.Join(",", TwoSum(new[] {2, 7, 11, 15}, 9))'
    expect: '0,1'
  - call: 'string.Join(",", TwoSum(new[] {3, 2, 4}, 6))'
    expect: '1,2'
  - call: 'string.Join(",", TwoSum(new[] {1, 2}, 5))'
    expect: ''
---
public static int[] TwoSum(int[] nums, int target)
{
    var seen = new Dictionary<int, int>();
    for (int i = 0; i < nums.Length; i++)
    {
        if (seen.TryGetValue(target - nums[i], out var j)) return new[] { j, i };
        seen[nums[i]] = i;
    }
    return Array.Empty<int>();
}
