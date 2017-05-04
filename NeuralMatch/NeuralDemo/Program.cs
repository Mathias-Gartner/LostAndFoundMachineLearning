using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.IO;
using Accord.MachineLearning.DecisionTrees;
using Accord.Neuro;
using log4net.Config;
using NeuralMatch;
using Rubicon.NovaFind.MatchService.Messages;
using Rubicon.NovaFind.MatchService.Messages.Attributes;

namespace NeuralDemo
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      if (args.Length != 4)
      {
        Console.WriteLine("Usage: NeuralDemo.exe network.dat tree.dat training.json test.json reindex.json");
      }
      var networkFileame = args[0];
      var treeFilename = args[1];
      var trainingFilename = args[2];
      var testFilename = args[3];
      var reindexFilename = args[4];

      XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
      var stopWatch = new Stopwatch();

      stopWatch.Start();
      var network = Network.Load(networkFileame);
      stopWatch.Stop();
      Console.WriteLine("Network loaded in {0}.", stopWatch.Elapsed);

      stopWatch.Restart();
      DecisionTree tree;
      using (var fileStream = File.OpenRead(treeFilename))
        tree = Serializer.Load<DecisionTree>(fileStream);
      stopWatch.Stop();
      Console.WriteLine("DecisionTree loaded in {0}.", stopWatch.Elapsed);

      stopWatch.Restart();
      var deserializer = new ReindexDeserializer(reindexFilename);
      var data = deserializer.Deserialize();
      stopWatch.Stop();
      Console.WriteLine("Data loaded in {0}.", stopWatch.Elapsed);

      stopWatch.Restart();
      var mapperSettings = ItemMapperSettings.FromDeserialized(data);
      var mapper = new MatchedItemsMapper(mapperSettings);
      stopWatch.Stop();
      Console.WriteLine("Mapper loaded in {0}.", stopWatch.Elapsed);

      /*stopWatch.Restart();
      //var loader = new BinaryDataLoader(reindexFilename, trainingFilename, testFilename);
      //var loader = new JsonDataLoader(reindexFilename, false, false, false);
      var loader = new JsonPairDataLoader(reindexFilename, trainingFilename, testFilename);
      var learningData = loader.Load();
      stopWatch.Stop();
      Console.WriteLine("Pairs loaded in {0}", stopWatch.Elapsed);*/

      var items = data.Items.Where(item => item.ItemType == LostAndFoundIndexedItem.Type.Loss).ToArray();
      Console.WriteLine("Ready.");
      Console.WriteLine();

      while (true)
      {
        Console.WriteLine("Enter some data on the item you found:");
        var date = EnterData("Date of finding");
        var money = EnterData("Money");
        var attributeList = new List<AttributeBase>();
        if (money != null)
          attributeList.Add(new MoneyValueAttribute {Value = new MoneyValue {Currency = "EUR", Value = Convert.ToDecimal(money)}});
        var color = EnterData("Color (#HTML-Code)");
        if (color != null)
          attributeList.Add(new ColorValueAttribute {Value = color});

        var finding = new LostAndFoundIndexedItem
        {
          CategoryID = "ff61ce82-db05-b000-07ff-0000000000" + EnterData("CategoryID (ff61ce82-db05-b000-07ff-0000000000XX)"),
          SubCategoryID = "b988f940-db05-b000-07ff-0000000000" + EnterData("SubCategoryID (b988f940-db05-b000-07ff-0000000000XX)"),
          DateOfIncident = date != null ? Convert.ToDateTime(date) : new DateTime(1999, 12, 31),
          Attributes = attributeList,
        };

        var results = new List<Tuple<LostAndFoundIndexedItem, MatchingPair, int>>();
        Parallel.ForEach(items, item =>
        {
          var pair = mapper.Map(item, finding);
          pair.PercentMatch = network.Compute(pair.ToVectorArray(new Dictionary<string, IndexableAttributeMetadata>())).Single();
          var decisionTreeResult = tree.Decide(pair.ToVectorArray(new Dictionary<string, IndexableAttributeMetadata>()));

          lock (results)
          {
            results.Add(new Tuple<LostAndFoundIndexedItem, MatchingPair, int>(item, pair, decisionTreeResult));
          }
        });

        Console.WriteLine("Top 5 Neural Network Results:");
        Console.WriteLine(string.Join(Environment.NewLine,
          results.OrderByDescending(result => result.Item2.PercentMatch)
            .Take(5)
            .Select(result => string.Format("{0}%\t{1}", result.Item2.PercentMatch * 100, result.Item1.RecordNumber))));

        Console.WriteLine("Decision Tree Results:");
        Console.WriteLine(string.Join(Environment.NewLine,
          results.Where(result => result.Item3 == 1)
            .Select(result => string.Format("{0}%\t{1}", result.Item3 * 100, result.Item1.RecordNumber))));

        Console.Write("Enter next item? [Y/n]");
        var next = Console.ReadLine();
        if (!string.IsNullOrEmpty(next) && next.ToLower() != "y")
          return;
      }

    }

    private static string EnterData(string caption)
    {
      Console.Write("{0}:", caption);
      var data = Console.ReadLine();
      if (data == string.Empty)
        return null;
      return data;
    }
  }
}