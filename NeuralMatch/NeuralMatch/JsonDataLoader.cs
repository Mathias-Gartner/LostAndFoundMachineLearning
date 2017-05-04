using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using log4net;
using Newtonsoft.Json;

namespace NeuralMatch
{
  public class JsonDataLoader : IDataLoader
  {
    private static ILog Logger = LogManager.GetLogger(typeof(JsonDataLoader));

    public string Filename { get; }
    public bool UseLegacyData { get; }
    public bool ExportSerializedAsJson { get; }
    public bool ExportSerializedAsBinary { get; }

    public JsonDataLoader(string filename, bool useLegacyData, bool exportSerializedAsJson, bool exportSerializedAsBinary)
    {
      Filename = filename;
      UseLegacyData = useLegacyData;
      ExportSerializedAsJson = exportSerializedAsJson;
      ExportSerializedAsBinary = exportSerializedAsBinary;
    }

    public LearningData Load()
    {
      var stopWatch = new Stopwatch();
      stopWatch.Start();
      var deserializer = new ReindexDeserializer(Filename);
      var deserializedData = deserializer.Deserialize();
      var items = deserializedData.Items;

      if (!UseLegacyData)
        items = items.Where(item => !item.IsLegacyObject).ToList();

      stopWatch.Stop();
      Logger.DebugFormat("JSON deserialization took {0}", stopWatch.Elapsed);

      stopWatch.Restart();

//      Logger.DebugFormat("Public attributes: " + string.Join(", ", items.SelectMany(item => item.PublicAttributes.Select(attr => attr.Name.DE)).Distinct()));
//      Logger.DebugFormat("Attributes: {0}", string.Join(", ", items.SelectMany(item => item.Attributes.Select(attr => attr.ID).Distinct())));
//      Logger.DebugFormat("Categories: {0}", string.Join(", ", items.Select(item => item.CategoryID)));
//      Logger.DebugFormat("Attributes: {0}", string.Join(", ", deserializedData.AttributeMetadata.Select(metadata => metadata.Attribute.GetType().FullName).Distinct()));
//      var enumValues = items.SelectMany(item => item.Attributes.OfType<EnumValueAttribute>());
//      Logger.DebugFormat("EnumValues: {0}", string.Join(", ", enumValues.Select(enumValue => $"ID: {enumValue.ID} Value: {enumValue.Value}")));
//      Logger.DebugFormat("Dates: {0}", string.Join(", ", items.GroupBy(item => item.DateOfIncident.ToShortDateString()).OrderByDescending(group => group.Count()).Select(group => $"{group.Key}: {group.Count()}")));
//      Logger.DebugFormat("Attributes with ID null: {0}",
//        string.Join(", ", deserializedData.AttributeMetadata.Where(attr => attr.Attribute.ID == null).Select(attr => attr.Attribute.GetType().FullName)));
//      Logger.DebugFormat("MoneyValue with ID: {0} Without: {1}",
//        deserializedData.AttributeMetadata.Count(attr => attr.Attribute is MoneyValueAttribute && attr.Attribute.ID != null),
//        deserializedData.AttributeMetadata.Count(attr => attr.Attribute is MoneyValueAttribute && attr.Attribute.ID == null));
//      Logger.DebugFormat("Items with more than one color: {0}", items.Count(item => item.Attributes.OfType<ColorValueAttribute>()
//                          .Count(color => !string.IsNullOrEmpty(color.Value?.Trim()) && color.Value != "#000000" && color.Value != "#ffffff") > 1));
//      Logger.DebugFormat("Items with more than one money: {0}", items.Count(item => item.Attributes.OfType<MoneyValueAttribute>().Count(color => color.Value != null && color.Value.Value != 0) > 1));
//      Logger.DebugFormat("Items with more than one color: {0}", string.Join(", ",
//          items.Where(item => item.Attributes.OfType<ColorValueAttribute>().Count(color => !string.IsNullOrEmpty(color.Value?.Trim()) && color.Value != "#000000" && color.Value != "#ffffff") > 1)
//            .Take(10).Select(i => i.Description)));
//      File.WriteAllLines("/tmp/text.txt", items.SelectMany(item => new[] {item.Description, item.PublicDescription}.Where(s => !string.IsNullOrEmpty(s))));
//      Logger.DebugFormat("Legacy: {0} Not legacy: {1}", items.Count(item => item.IsLegacyObject), items.Count(item => !item.IsLegacyObject));
//      return null;

      stopWatch.Restart();

      var mapperSettings = ItemMapperSettings.FromDeserialized(deserializedData);
      var mapper = new MatchedItemsMapper(mapperSettings);

      var matcher = new MatchingItemMatcher(mapper);
      var matches = matcher.GetMatchingPairs(items);

      var unmatcher = new RandomNotMatchingItemMatcher(mapper, 15);
      // TODO: Umnmatches auf Basis der matches erzeugen?
      var unmatched = unmatcher.GetMatchingPairs(items);

//      unmatched = unmatched.Where(pair => pair.LossAttributes.Any(attr => attr.Value.Length >= 1 && attr.Value[0] != 0.0) &&
//                                      pair.FindingAttributes.Any(attr => attr.Value.Length >= 1 && attr.Value[0] != 0.0)).ToList();

      Logger.InfoFormat("Matches: {0}\tUnmatched: {1}", matches.Count, unmatched.Count);

      var trainingSetSize = matches.Count / 2;
      var trainingData = matches.Take(trainingSetSize).Concat(unmatched.Take(trainingSetSize)).ToArray();
      var testData = matches.Skip(trainingSetSize).Concat(unmatched.Skip(trainingSetSize).Take(matches.Count - trainingSetSize)).ToArray();

      var usedAttributes = matches.Concat(unmatched).SelectMany(pair => pair.FindingAttributes.Concat(pair.LossAttributes).Select(a => a.Key));
      var usedMetadata = deserializedData.AttributeMetadata.Where(attr => usedAttributes.Contains(attr.Attribute.ID));
      var actualMetadata = usedMetadata.Where(data => data.Attribute.ID != null).ToDictionary(data => data.Attribute.ID);

      stopWatch.Stop();
      Logger.DebugFormat("Data manipulation took {0}", stopWatch.Elapsed);

      var learningData = new LearningData(actualMetadata, testData, trainingData);

      if (ExportSerializedAsJson)
        SerializeDataAsJson(learningData);
      if (ExportSerializedAsBinary)
        SerializeDataAsBinary(learningData);

      return learningData;
    }

    protected virtual void SerializeDataAsJson(LearningData data)
    {
      var serializer = new JsonSerializer();
      using (var filestream = File.Open("training.json", FileMode.Create, FileAccess.Write))
      using (var writer = new StreamWriter(filestream))
        serializer.Serialize(writer, data.TrainingData);
      using (var filestream = File.Open("test.json", FileMode.Create, FileAccess.Write))
      using (var writer = new StreamWriter(filestream))
        serializer.Serialize(writer, data.TestData);
    }

    protected virtual void SerializeDataAsBinary(LearningData data)
    {
      var serializer = new BinaryFormatter();
      using (var filestream = File.Open("training.dat", FileMode.Create, FileAccess.Write))
        serializer.Serialize(filestream, data.TrainingData);
      using (var filestream = File.Open("test.dat", FileMode.Create, FileAccess.Write))
        serializer.Serialize(filestream, data.TestData);
    }
  }
}