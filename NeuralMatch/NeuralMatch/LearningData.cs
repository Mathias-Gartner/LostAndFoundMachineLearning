using System.Collections.Generic;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class LearningData
  {
    public LearningData(Dictionary<string, IndexableAttributeMetadata> actualMetadata, MatchingPair[] trainingData, MatchingPair[] testData)
    {
      ActualMetadata = actualMetadata;
      TrainingData = trainingData;
      TestData = testData;
    }

    public Dictionary<string, IndexableAttributeMetadata> ActualMetadata { get; }
    public MatchingPair[] TrainingData { get; }
    public MatchingPair[] TestData { get; }
  }
}