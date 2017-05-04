using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public interface IItemMapper
  {
    MatchingPair MapMatch(LostAndFoundIndexedItem loss, LostAndFoundIndexedItem finding);

    MatchingPair MapNonMatch(LostAndFoundIndexedItem loss, LostAndFoundIndexedItem finding);
  }
}