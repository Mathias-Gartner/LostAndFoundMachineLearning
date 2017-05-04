using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NeuralMatch
{
  public class JsonPairDataLoader : IDataLoader
  {
    public string JsonFilename { get; }
    public string TrainingFileName { get; }
    public string TestFileName { get; }

    public bool UseLegacyData => true;

    public JsonPairDataLoader(string jsonFilename, string trainingFileName, string testFileName)
    {
      JsonFilename = jsonFilename;
      TrainingFileName = trainingFileName;
      TestFileName = testFileName;
    }

    public LearningData Load()
    {
      MatchingPair[] trainingData, testData;
      var serializer = new JsonSerializer();
      using (var filestream = File.OpenRead(TrainingFileName))
      using (var reader = new StreamReader(filestream))
      using (var jsonReader = new JsonTextReader(reader))
        trainingData = serializer.Deserialize<MatchingPair[]>(jsonReader);
      using (var filestream = File.OpenRead(TestFileName))
      using (var reader = new StreamReader(filestream))
      using (var jsonReader = new JsonTextReader(reader))
        testData = serializer.Deserialize<MatchingPair[]>(jsonReader);

      var metadataDeserializer = new MetadataDeserializer(JsonFilename);
      var metadata = metadataDeserializer.Deserialize();

      var usedAttributes = trainingData.Concat(testData).SelectMany(pair => pair.FindingAttributes.Concat(pair.LossAttributes).Select(a => a.Key));
      var usedMetadata = metadata.Where(attr => usedAttributes.Contains(attr.Attribute.ID));
      var actualMetadata = usedMetadata.Where(data => data.Attribute.ID != null).ToDictionary(data => data.Attribute.ID);

      return new LearningData(actualMetadata, trainingData, testData);
    }
  }
}