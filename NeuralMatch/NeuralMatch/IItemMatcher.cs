using System.Collections.Generic;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public interface IItemMatcher
  {
    IList<MatchingPair> GetMatchingPairs(ICollection<LostAndFoundIndexedItem> items);
  }
}