using System.Collections.Generic;
using NeuralMatch;
using NUnit.Framework;
using Rubicon.NovaFind.MatchService.Messages;

namespace UnitTests
{
  [TestFixture]
  public class MatchingPairTests
  {
    [Test]
    public void MatchingPairVectorSkipsProperties()
    {
      var pair = new MatchingPair
      {
        FindingDateOfIncident = 1,
        LossDateOfIncident = 2,
        FindingCategory = 12,
        LossCategory = 13,
        FindingColors = new[] {12.1, 16},
        LossColors = new[] {12.1, 16},
        FindingMoney = 14,
        LossMoney = 15,
        PercentMatch = 0.65323
      };

      var vector = pair.ToVectorArray(new Dictionary<string, IndexableAttributeMetadata>(),
        new[] {nameof(MatchingPair.FindingColors), nameof(MatchingPair.LossColors), nameof(MatchingPair.FindingMoney), nameof(MatchingPair.LossMoney)});

      Assert.That(vector, Is.EqualTo(new[] {0.65323, 2, 1, 13, 12.0}));
    }
  }
}