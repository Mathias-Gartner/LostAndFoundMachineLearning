using System.Collections.Generic;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public interface ILearner
  {
    void Learn();

    string Name { get; set; }
    LearningData LearningData { get; set; }
    IDictionary<string, IndexableAttributeMetadata> Metadata { get; set; }
    IList<string> PropertiesToSkip { get; set; }
  }
}