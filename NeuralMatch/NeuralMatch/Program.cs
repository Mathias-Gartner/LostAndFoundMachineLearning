using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math.Optimization.Losses;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Statistics.Kernels;
using Accord.Statistics.Models.Regression.Linear;
using log4net;
using log4net.Config;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  internal class Program
  {
    private static ILog Logger = LogManager.GetLogger(typeof(Program));

    public static void Main(string[] args)
    {
      XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));

      IDataLoader dataLoader;

      var jsonFilename = args.First();
      if (args.Length == 1)
      {
        dataLoader = new JsonDataLoader(jsonFilename, useLegacyData: false, exportSerializedAsJson: false, exportSerializedAsBinary: false);
      }
      else if (args.Length == 3)
      {
        var trainingFileName = args[1];
        var testFileName = args[2];
        if (trainingFileName.EndsWith("json"))
          dataLoader = new JsonPairDataLoader(jsonFilename, trainingFileName, testFileName);
        else
          dataLoader = new BinaryDataLoader(jsonFilename, trainingFileName, testFileName);
      }
      else
      {
        Console.WriteLine("Usage: NeuralMatch.exe input.json [training.xml test.xml]");
        return;
      }

      var stopWatch = new Stopwatch();
      stopWatch.Start();
      LearningData data;
      try
      {
        data = dataLoader.Load();
      }
      catch (Exception e)
      {
        Logger.Error("Data load failed", e);
        throw;
      }
      if (data == null)
      {
        Logger.Fatal("No LearningData loaded");
        return;
      }
      stopWatch.Stop();
      Logger.InfoFormat("Data load took {0}", stopWatch.Elapsed);

      stopWatch.Restart();

      var csvWriter = new CsvExporter(data.ActualMetadata);
      csvWriter.WriteCsv("training.csv", data.TrainingData);
      csvWriter.WriteCsv("test.csv", data.TestData);

      stopWatch.Stop();
      Logger.InfoFormat("CSV Export took {0}", stopWatch.Elapsed);

      stopWatch.Restart();

      /*var neuralNetworkTask = new Task(() => NeuralNetworkLearningAllMetadata(trainingData, testData, actualMetadata));
      neuralNetworkTask.Start();
      var supportVectorMachineTraining = new Task(() => SupportVectorMachineTraining(trainingData, testData, actualMetadata));
      supportVectorMachineTraining.Start();
      var linearRegressionLearning = new Task(() => LinearRegressionLearning(trainingData, testData, actualMetadata));
      linearRegressionLearning.Start();
      linearRegressionLearning.Wait();
      supportVeectorMachineTraining.Wait();
      neuralNetworkTask.Wait();*/
      //SupportVectorMachineTraining(trainingData, testData, actualMetadata);


      Action decisionTreeLearning = () =>
      {
        var decisionTreeLearner = new DecisionTreeLearner(data);
        decisionTreeLearner.Learn();
        using (var fileStream = File.Open("decisiontree.dat", FileMode.Create, FileAccess.Write))
          Serializer.Save(decisionTreeLearner.Tree, fileStream);
      };

      Action neuralNetworkLearning = () =>
      {
        var learner = new NeuralNetworkLearner(data, 0, 512);
        learner.Learn();
        learner.BestNetwork.Save("network.dat");
      };

      Action neuralNetworkWithoutAttributes = () =>
      {
        var learner = new NeuralNetworkLearner(data, 0, 512)
        {
          Metadata = new Dictionary<string, IndexableAttributeMetadata>(),
          PropertiesToSkip = new []
          {
            nameof(MatchingPair.LossMoney),
            nameof(MatchingPair.FindingMoney),
            nameof(MatchingPair.FindingColors),
            nameof(MatchingPair.LossColors)
          },
          Name = "Date and category only"
        };
        learner.Learn();
      };

      Action neuralNetworkMoneyOnly = () =>
      {
        var learner = new NeuralNetworkLearner(data, 0, 512)
        {
          Metadata = new Dictionary<string, IndexableAttributeMetadata>(),
          PropertiesToSkip = new []
          {
            nameof(MatchingPair.FindingColors),
            nameof(MatchingPair.LossColors)
          },
          Name = "Money"
        };
        learner.Learn();
      };

      Action neuralNetworkColorsOnly = () =>
      {
        var learner = new NeuralNetworkLearner(data, 0, 512)
        {
          Metadata = new Dictionary<string, IndexableAttributeMetadata>(),
          PropertiesToSkip = new []
          {
            nameof(MatchingPair.LossMoney),
            nameof(MatchingPair.FindingMoney)
          },
          Name = "Color"
        };
        learner.Learn();
      };

      Action neuralNetworkSingleAttributes = () => NeuralNetworkLearningSingleAttributes(data);

      //TODO: 10-fold cross validation
      //TODO: random forest
      try
      {
        var threads = new[]
        {
          new Thread(() => decisionTreeLearning()),
          new Thread(() => neuralNetworkLearning()),
          new Thread(() => neuralNetworkWithoutAttributes()),
          new Thread(() => neuralNetworkMoneyOnly()),
          new Thread(() => neuralNetworkColorsOnly()),
          new Thread(() => neuralNetworkSingleAttributes())
        };
        //*
        foreach (var thread in threads)
          thread.Start();
        foreach (var thread in threads)
          thread.Join();
        //*/

        /*decisionTreeLearning();
        neuralNetworkLearning();
        neuralNetworkWithoutAttributes();
        neuralNetworkMoneyOnly();
        neuralNetworkColorsOnly();
        neuralNetworkSingleAttributes();*/
      }
      catch (Exception e)
      {
        Logger.Error("Learning failed", e);
        throw;
      }
      stopWatch.Stop();
      Logger.InfoFormat("Learning took {0}", stopWatch.Elapsed);

      // Neural: http://accord-framework.net/docs/html/T_Accord_Neuro_Learning_ParallelResilientBackpropagationLearning.htm
      // Linear Regression: https://github.com/accord-net/framework/wiki/Regression
    }

    private static void NeuralNetworkLearningSingleAttributes(LearningData learningData)
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var testMatcher = new LoggingNeuralNetworkMatcher(learningData.TestData);
      var trainingMatcher = new LoggingNeuralNetworkMatcher(learningData.TrainingData);

      Parallel.ForEach(learningData.ActualMetadata.Keys, metadataKey =>
      {
        var metadata = new Dictionary<string, IndexableAttributeMetadata>{{metadataKey, learningData.ActualMetadata[metadataKey]}};
        var trainingInputs = learningData.TrainingData.Select(data => data.ToVectorArray(metadata)).ToArray();
        var trainingOutputs = learningData.TrainingData.Select(data => new[] { data.PercentMatch }).ToArray();
        var testInputs = learningData.TestData.Select(data => data.ToVectorArray(metadata)).ToArray();
        var testOutputs = learningData.TestData.Select(data => new[] {data.PercentMatch}).ToArray();

        if (testInputs.Length != testOutputs.Length || trainingInputs.Length != trainingOutputs.Length)
          throw new ArgumentException("Inputs and outputs data are not the same size");
        var vectorSize = trainingInputs.First().Length;
        if (trainingInputs.Any(input => input.Length != vectorSize))
          throw new ArgumentException("Not all trainingInputs have the same vector size");
        if (testInputs.Any(input => input.Length != vectorSize))
          throw new ArgumentException("Not test inputs have the correct vector size");

        var results = new List<Tuple<int[], double, double>>();

        Parallel.For(0, 16, i =>
        {
        var parameters = new[] {i, 1};

        var network =
          new ActivationNetwork(new BipolarSigmoidFunction(), trainingInputs[0].Length,
            parameters); //new DeepBeliefNetwork();
        var teacher = new ParallelResilientBackpropagationLearning(network);
        var random = new Random();

        var error = double.MaxValue;
        var iteration = 0;
        while (error > 0.0005 && iteration < 200)
        {
          iteration++;
          //for (var i = 0; i < 10; i++)
          {
            //*
            var pair = random.Next(0, trainingInputs.Length - 1);
            error = teacher.Run(trainingInputs[pair], trainingOutputs[pair]);
            //*/
            /*
            error = teacher.RunEpoch(trainingInputs, trainingOutputs);
            //*/
            var accuracyRecallPrecision = trainingMatcher.MatchCount(network, metadata, new List<string>());
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
        }
          testMatcher.LogMatchCount(string.Format("{0} ({1})", metadataKey, learningData.ActualMetadata[metadataKey].Attribute.GetType().FullName), network,
            metadata, new List<string>());
        });

        Logger.InfoFormat("Results for {1} ({2}):\n{0}",
          string.Join(", ", results.Select(result => $"{string.Join("-", result.Item1)}: In: {result.Item2} Out: {result.Item3}")), metadataKey,
          learningData.ActualMetadata[metadataKey].Attribute.GetType().FullName);

      });

      stopWatch.Stop();
      Logger.InfoFormat("Neural Network learning (single attribute) took {0}", stopWatch.Elapsed);
    }

    private static void SupportVectorMachineTraining(IEnumerable<MatchingPair> trainingData, IEnumerable<MatchingPair> testData, IDictionary<string, IndexableAttributeMetadata> actualMetadata)
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var trainingInputs = trainingData.Select(data => data.ToVectorArray(actualMetadata)).ToArray();
      var trainingOutputs = trainingData.Select(data => data.PercentMatch > 0).ToArray();
      var testInputs = testData.Select(data => data.ToVectorArray(actualMetadata)).ToArray();
      var testOutputs = testData.Select(data => data.PercentMatch > 0).ToArray();

      var learn = new SequentialMinimalOptimization<Gaussian>()
      {
        UseComplexityHeuristic = true,
        UseKernelEstimation = true
      };

      SupportVectorMachine<Gaussian> svm = learn.Learn(trainingInputs, trainingOutputs);

      var inSampleScore = svm.Score(trainingInputs);
      var outOfSampleScore = svm.Score(testInputs);

      Logger.InfoFormat("Result:\nIn-sample: {0}\nOut-of-sample:{1}", string.Join(", ", inSampleScore), string.Join(", ", outOfSampleScore));

      var results = svm.Decide(trainingInputs);
      var inSampleErrors = trainingOutputs.Where((t, i) => results[i] != t).Count();
      results = svm.Decide(testInputs);
      var outOfSampleErrors = testOutputs.Where((t, i) => results[i] != t).Count();

      Logger.InfoFormat("Errors: In-sample: {0} Out-of-sample: {1}", inSampleErrors, outOfSampleErrors);

      stopWatch.Stop();
      Logger.InfoFormat("Regression Tree learning took {0}", stopWatch.Elapsed);
    }

    private static void LinearRegressionLearning(IEnumerable<MatchingPair> trainingData, IEnumerable<MatchingPair> testData, IDictionary<string, IndexableAttributeMetadata> actualMetadata)
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();

      var trainingInputs = trainingData.Select(data => data.ToVectorArray(actualMetadata)).ToArray();
      var trainingOutputs = trainingData.Select(data => new[] { data.PercentMatch }).ToArray();
      var testInputs = testData.Select(data => data.ToVectorArray(actualMetadata)).ToArray();
      var testOutputs = testData.Select(data => new[] {data.PercentMatch}).ToArray();

      var leastSquares = new OrdinaryLeastSquares();

      var regression = leastSquares.Learn(trainingInputs, trainingOutputs);

      var predictions = regression.Transform(trainingInputs);
      var error = new SquareLoss(trainingOutputs).Loss(predictions);
      Logger.InfoFormat("Linear Regression: In-sample error: {0}", error);

      predictions = regression.Transform(testInputs);
      error = new SquareLoss(testOutputs).Loss(predictions);
      Logger.InfoFormat("Linear Regression: Out-of-sample error: {0}", error);

      stopWatch.Stop();
      Logger.InfoFormat("Linear Regression learning took {0}", stopWatch.Elapsed);
    }
  }
}