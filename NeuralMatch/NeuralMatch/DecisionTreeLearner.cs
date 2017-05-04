using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Accord;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class DecisionTreeLearner : ILearner
  {
    private static ILog Logger = LogManager.GetLogger(typeof(DecisionTreeLearner));

    public DecisionTreeLearner(LearningData learningData)
    {
      LearningData = learningData;
      Metadata = LearningData.ActualMetadata;
      PropertiesToSkip = new List<string>();
      Name = "DecisionTree";
    }

    public LearningData LearningData { get; set; }

    public IDictionary<string, IndexableAttributeMetadata> Metadata { get; set; }

    public string Name { get; set; }

    public IList<string> PropertiesToSkip { get; set; }

    public DecisionTree Tree { get; set; }

    public void Learn()
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var variables = new List<DecisionVariable>();
      foreach (var vector in LearningData.TrainingData.First().ToVectorArray(Metadata, PropertiesToSkip))
        variables.Add(new DecisionVariable(variables.Count.ToString(), new DoubleRange(-1, 1)));
      Tree = new DecisionTree(variables, 2);
      var learner = new C45Learning(Tree);

      learner.Learn(LearningData.TrainingData.Select(data => data.ToVectorArray(Metadata, PropertiesToSkip)).ToArray(),
        LearningData.TrainingData.Select(data => data.PercentMatch > 0 ? 1 : 0).ToArray());

      var matcher = new LoggingDecisionTreeMatcher(LearningData.TrainingData);
      matcher.LogMatchCount($"{Name} TrainingData", Tree, Metadata, PropertiesToSkip);
      matcher = new LoggingDecisionTreeMatcher(LearningData.TestData);
      matcher.LogMatchCount($"{Name} TestData", Tree, Metadata, PropertiesToSkip);

      stopWatch.Stop();
      Logger.InfoFormat("DecisionTreeLearning took {0}", stopWatch.Elapsed);
    }
  }
}
