using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.Neuro;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class LoggingNeuralNetworkMatcher
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(LoggingNeuralNetworkMatcher));

    public const double Threshold = 0;

    public IEnumerable<MatchingPair> TestData { get; }

    public LoggingNeuralNetworkMatcher(IEnumerable<MatchingPair> testData)
    {
      TestData = testData;
    }

    public virtual IEnumerable<Tuple<MatchingPair, double>> GetMatchingPairs(ActivationNetwork net, IDictionary<string, IndexableAttributeMetadata> actualMetadata, IList<string> propertiesToSkip)
    {
      return TestData.AsParallel().Select(pair => new Tuple<MatchingPair, double>(pair, net.Compute(pair.ToVectorArray(actualMetadata, propertiesToSkip)).Single()));
    }

    public void LogMatchCount(string prefix, ActivationNetwork net, IDictionary<string, IndexableAttributeMetadata> actualMetadata, IList<string> propertiesToSkip)
    {
      var over90 = 0;
      var over75 = 0;
      var over50 = 0;
      var over35 = 0;
      var under35 = 0;
      var truePositive = 0;
      var trueNegative = 0;
      var falsePositive = 0;
      var falseNegative = 0;
      var pairCount = 0;

      var pairs = GetMatchingPairs(net, actualMetadata, propertiesToSkip);

      foreach (var tuple in pairs)
      {
        pairCount++;

        // actualMatch gets positive value if postive or negative match was classified correctly
        var actualMatch = tuple.Item1.PercentMatch * tuple.Item2;

        if (actualMatch > 0.8)
          over90++;
        else if (actualMatch> 0.5)
          over75++;
        else if (actualMatch > 0)
          over50++;
        else if (actualMatch > -0.3)
          over35++;
        else
          under35++;

        if (tuple.Item1.PercentMatch > Threshold && tuple.Item2 > Threshold)
          truePositive++;
        if (tuple.Item1.PercentMatch <= Threshold && tuple.Item2 <= Threshold)
          trueNegative++;
        if (tuple.Item1.PercentMatch <= Threshold && tuple.Item2 > Threshold)
          falsePositive++;
        if (tuple.Item1.PercentMatch > Threshold && tuple.Item2 <= Threshold)
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

      Logger.DebugFormat("PercentMatch for {0}: Over 90%: {1} Over 75%: {2} Over 50%: {3} Over 35%: {4} Under 35%: {5}\nSum: {13} True Positive: {6} True Negative: {7} " +
                        "False Positive: {8} False Negative: {9}\nAccuracy: {10} Recall: {11} Precision: {12}",
        prefix, over90, over75, over50, over35, under35, truePositive, trueNegative, falsePositive, falseNegative, accuracy, recall, precision, pairCount);
    }

    public Tuple<double, double, double> MatchCount(ActivationNetwork net, IDictionary<string, IndexableAttributeMetadata> actualMetadata,
      IList<string> propertiesToSkip)
    {
      var truePositive = 0;
      var trueNegative = 0;
      var falsePositive = 0;
      var falseNegative = 0;
      var pairCount = 0;

      var pairs = GetMatchingPairs(net, actualMetadata, propertiesToSkip);

      foreach (var tuple in pairs)
      {
        pairCount++;
        if (tuple.Item1.PercentMatch > Threshold && tuple.Item2 > Threshold)
          truePositive++;
        if (tuple.Item1.PercentMatch <= Threshold && tuple.Item2 <= Threshold)
          trueNegative++;
        if (tuple.Item1.PercentMatch <= Threshold && tuple.Item2 > Threshold)
          falsePositive++;
        if (tuple.Item1.PercentMatch > Threshold && tuple.Item2 <= Threshold)
          falseNegative++;
      }

      if (truePositive == 0)
      {
        return new Tuple<double, double, double>(0, 0, 0);
      }

      var accuracy = (double) (truePositive + trueNegative) / pairCount;
      var recall = (double) truePositive / (truePositive + falseNegative);
      var precision = (double) truePositive / (truePositive + falsePositive);

      return new Tuple<double, double, double>(accuracy, recall, precision);
    }
  }
}