using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class MatchingItemMatcher : IItemMatcher
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchingItemMatcher));

    private readonly IItemMapper _mapper;

    public MatchingItemMatcher(IItemMapper mapper)
    {
      _mapper = mapper;
    }

    public IList<MatchingPair> GetMatchingPairs(ICollection<LostAndFoundIndexedItem> items)
    {
      var findings = items.Where(item => item.ItemType == LostAndFoundIndexedItem.Type.Finding).ToArray();
      var lockObject = new object();
      var resultList = new List<MatchingPair>(500000);

      Parallel.ForEach<LostAndFoundIndexedItem, ICollection<MatchingPair>>(
        items.Where(item => item.MatchedRecordID != null && item.ItemType == LostAndFoundIndexedItem.Type.Loss), new ParallelOptions{MaxDegreeOfParallelism = 6},
        () => new LinkedList<MatchingPair>(),
        (loss, loopState, list) =>
        {
          var matches = findings.Where(i => i.RecordID == loss.MatchedRecordID).ToArray();

          if (loopState.ShouldExitCurrentIteration)
            return list;

          if (matches.Length > 1)
          {
            var description = loss.Description;
            if (description.Length > 100)
              description = description.Substring(0, 100);
            Logger.WarnFormat("Finding with ID {0} and Description {1} found multiple times", loss.MatchedRecordID, description);
          }

          var matchedFinding = matches.FirstOrDefault();
          if (matchedFinding != null)
          {
            var pair = _mapper.MapMatch(loss, matchedFinding);
            list.Add(pair);
          }
          return list;
        },
        list =>
        {
          lock (lockObject)
          {
            resultList.AddRange(list);
            if (resultList.Count % 1000 < (resultList.Count - list.Count) % 1000)
              Logger.DebugFormat("{0} items matched", resultList.Count);
          }
        }
      );
      return resultList;
    }
  }
}