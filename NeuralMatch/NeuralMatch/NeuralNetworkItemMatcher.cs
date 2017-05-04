using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Neuro;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class NeuralNetworkItemMatcher
  {
    public IItemMapper Mapper { get; }
    public IDictionary<string, IndexableAttributeMetadata> ActualMetadata { get; }
    public LostAndFoundIndexedItem[] Findings { get; }
    public LostAndFoundIndexedItem[] Losses { get; }

    public NeuralNetworkItemMatcher(IItemMapper mapper, IDictionary<string, IndexableAttributeMetadata> actualMetadata, ICollection<LostAndFoundIndexedItem> items)
    {
      Mapper = mapper;
      ActualMetadata = actualMetadata;
      Findings = items.Where(item => item.ItemType == LostAndFoundIndexedItem.Type.Finding).ToArray();
      Losses = items.Where(item => item.ItemType == LostAndFoundIndexedItem.Type.Loss).ToArray();
    }

    public virtual IList<MatchingPair> GetMatchingPairs(ActivationNetwork net)
    {
      var pairs = new List<MatchingPair>();
      Parallel.ForEach(Findings, finding =>
      {
        MatchingPair bestPair = null;
        foreach (var loss in Losses)
        {
          var pair = Mapper.MapMatch(loss, finding);
          pair.PercentMatch = net.Compute(pair.ToVectorArray(ActualMetadata)).Single();
          if (bestPair == null || bestPair.PercentMatch < pair.PercentMatch)
            bestPair = pair;
        }
        lock (pairs)
        {
          pairs.Add(bestPair);
        }
      });
      return pairs;
    }
  }
}