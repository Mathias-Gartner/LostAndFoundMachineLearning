using System;
using System.Collections.Generic;
using System.Linq;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class RandomNotMatchingItemMatcher : IItemMatcher
  {
    private readonly IItemMapper _mapper;
    private readonly int _daysWithoutMatch;

    public RandomNotMatchingItemMatcher(IItemMapper mapper, int daysWithoutMatch)
    {
      _mapper = mapper;
      _daysWithoutMatch = daysWithoutMatch;
    }

    public IList<MatchingPair> GetMatchingPairs(ICollection<LostAndFoundIndexedItem> items)
    {
      var findings = items.Where(item => item.ItemType == LostAndFoundIndexedItem.Type.Finding).ToArray();
      var randomizer = new Random();

      Func<LostAndFoundIndexedItem, LostAndFoundIndexedItem> randomNonMatch =
        item => findings[randomizer.Next(0, findings.Length - 1)];

      var unmatched = items.Where(match => match.ItemType == LostAndFoundIndexedItem.Type.Loss &&
                                           match.MatchedRecordID == null &&
                                           (DateTime.Now - match.DateOfIncident).Days > _daysWithoutMatch);

      return unmatched.Select(item => _mapper.MapNonMatch(item, randomNonMatch(item))).ToList();
    }
  }
}