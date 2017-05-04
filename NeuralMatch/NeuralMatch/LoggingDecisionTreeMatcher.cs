using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.MachineLearning.DecisionTrees;
using Accord.Neuro;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class LoggingDecisionTreeMatcher
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(LoggingNeuralNetworkMatcher));

    public const double Threshold = 0;

    public IEnumerable<MatchingPair> TestData { get; }

    public LoggingDecisionTreeMatcher(IEnumerable<MatchingPair> testData)
    {
      TestData = testData;
    }

    public virtual IEnumerable<Tuple<MatchingPair, int>> GetMatchingPairs(DecisionTree tree, IDictionary<string, IndexableAttributeMetadata> actualMetadata, IList<string> propertiesToSkip)
    {
      return TestData.AsParallel().Select(pair => new Tuple<MatchingPair, int>(pair, tree.Decide(pair.ToVectorArray(actualMetadata, propertiesToSkip))));
    }

    public void LogMatchCount(string prefix, DecisionTree tree, IDictionary<string, IndexableAttributeMetadata> actualMetadata, IList<string> propertiesToSkip = null)
    {
      var truePositive = 0;
      var trueNegative = 0;
      var falsePositive = 0;
      var falseNegative = 0;
      var pairCount = 0;

      var pairs = GetMatchingPairs(tree, actualMetadata, propertiesToSkip);

      foreach (var tuple in pairs)
      {
        pairCount++;
        if (tuple.Item1.PercentMatch > Threshold && tuple.Item2 == 1)
          truePositive++;
        if (tuple.Item1.PercentMatch <= Threshold && tuple.Item2 == 0)
          trueNegative++;
        if (tuple.Item1.PercentMatch <= Threshold && tuple.Item2 == 1)
          falsePositive++;
        if (tuple.Item1.PercentMatch > Threshold && tuple.Item2 == 0)
          falseNegative++;
      }

      if (truePositive == 0)
      {
        Logger.WarnFormat("{0} has no true positives", prefix);
        return;
      }

      var accuracy = (double)(truePositive + trueNegative) / pairCount;
      var recall = (double)truePositive / (truePositive + falseNegative);
      var precision = (double)truePositive / (truePositive + falsePositive);

      Logger.DebugFormat("PercentMatch for {0}: Sum: {13} True Positive: {6} True Negative: {7} False Positive: {8} False Negative: {9}\nAccuracy: {10} Recall: {11} Precision: {12}",
        prefix, truePositive, trueNegative, falsePositive, falseNegative, accuracy, recall, precision, pairCount);
    }
  }
}