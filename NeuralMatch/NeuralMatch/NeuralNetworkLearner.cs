using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Accord;
using Accord.Neuro;
using Accord.Neuro.Learning;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class NeuralNetworkLearner : ILearner
  {
    private static ILog Logger = LogManager.GetLogger(typeof(NeuralNetworkLearner));

    public NeuralNetworkLearner(LearningData learningData, int hiddenLayerNeuronCountMin, int hiddenLayerNeuronCountMax)
    {
      LearningData = learningData;
      Metadata = learningData.ActualMetadata;
      Range = new IntRange(hiddenLayerNeuronCountMin, hiddenLayerNeuronCountMax);
      Name = "NeuralNetwork";
      BestError = double.MaxValue;
    }

    public string Name { get; set; }
    public LearningData LearningData { get; set; }
    public IDictionary<string, IndexableAttributeMetadata> Metadata { get; set; }
    public IList<string> PropertiesToSkip { get; set; }

    public IntRange Range { get; set; }
    public ActivationNetwork BestNetwork { get; set; }
    public double BestError { get; set; }
    public int BestParameter { get; set; }

    public void Learn()
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var trainingInputs = LearningData.TrainingData.Select(data => data.ToVectorArray(Metadata, PropertiesToSkip)).ToArray();
      var trainingOutputs = LearningData.TrainingData.Select(data => new[] { data.PercentMatch }).ToArray();
      var testInputs = LearningData.TestData.Select(data => data.ToVectorArray(Metadata, PropertiesToSkip)).ToArray();
      var testOutputs = LearningData.TestData.Select(data => new[] {data.PercentMatch}).ToArray();

      if (testInputs.Length != testOutputs.Length || trainingInputs.Length != trainingOutputs.Length)
        throw new ArgumentException("Inputs and outputs data are not the same size");
      var vectorSize = trainingInputs.First().Length;
      if (trainingInputs.Any(input => input.Length != vectorSize))
        throw new ArgumentException("Not all trainingInputs have the same vector size");
      if (testInputs.Any(input => input.Length != vectorSize))
        throw new ArgumentException("Not test inputs have the correct vector size");

      var testMatcher = new LoggingNeuralNetworkMatcher(LearningData.TestData);
      var trainingMatcher = new LoggingNeuralNetworkMatcher(LearningData.TrainingData);
      var results = new List<Tuple<int[], double, double>>();

      Parallel.For(Range.Min, Range.Max + 1, i =>
      {
        var parameters = i > 0 ? new[] {i, 1} : new [] { 1 };

        var network =
          new ActivationNetwork(new BipolarSigmoidFunction(), trainingInputs[0].Length,
            parameters); //new DeepBeliefNetwork();
        var teacher = new ParallelResilientBackpropagationLearning(network);
        var random = new Random();

        var error = double.MaxValue;
        var iteration = 0;
        while (error > 0.0005 && iteration < 1000)
        {
          iteration++;
          {
            var pair = random.Next(0, trainingInputs.Length - 1);
            teacher.Run(trainingInputs[pair], trainingOutputs[pair]);

            var accuracyRecallPrecision = trainingMatcher.MatchCount(network, Metadata, PropertiesToSkip);
            error = 3 - accuracyRecallPrecision.Item1 - accuracyRecallPrecision.Item2 - accuracyRecallPrecision.Item3;
          }

          if (iteration % 100 == 0)
            Logger.DebugFormat("NeuralNetwork: Iteration {0} Error {1}", iteration, error);
        }

        var inSampleError = teacher.ComputeError(trainingInputs, trainingOutputs);
        var outOfSampleError = teacher.ComputeError(testInputs, testOutputs);
        lock (results)
        {
          results.Add(new Tuple<int[], double, double>(parameters, inSampleError, outOfSampleError));
          if (error < BestError)
          {
            BestNetwork = network;
            BestParameter = i;
            BestError = error;
          }
        }
        testMatcher.LogMatchCount(string.Format("{0}: {1}", Name, string.Join("-", parameters)), network, Metadata, PropertiesToSkip);
      });

      Logger.DebugFormat("Results ({0}):\n{1}", Name,
        string.Join(", ", results.Select(result => $"{string.Join("-", result.Item1)}: In: {result.Item2} Out: {result.Item3}")));
      Logger.InfoFormat("Best {0}: {1}-1 Error {2}", Name, BestParameter, BestError);

      stopWatch.Stop();
      Logger.InfoFormat("Neural Network learning ({0}) took {1}", Name, stopWatch.Elapsed);
    }

  }
}