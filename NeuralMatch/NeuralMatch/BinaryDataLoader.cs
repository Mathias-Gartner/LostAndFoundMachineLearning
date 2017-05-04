using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeuralMatch
{
  public class BinaryDataLoader : IDataLoader
  {
    public string JsonFilename { get; }
    public string TrainingFileName { get; }
    public string TestFileName { get; }

    public bool UseLegacyData => true;

    public BinaryDataLoader(string jsonFilename, string trainingFileName, string testFileName)
    {
      JsonFilename = jsonFilename;
      TrainingFileName = trainingFileName;
      TestFileName = testFileName;
    }

    public LearningData Load()
    {
      MatchingPair[] trainingData, testData;
      var serializer = new BinaryFormatter();
      using (var filestream = File.OpenRead(TrainingFileName))
        trainingData = (MatchingPair[]) serializer.Deserialize(filestream);
      using (var filestream = File.OpenRead(TestFileName))
        testData = (MatchingPair[]) serializer.Deserialize(filestream);

      var metadataDeserializer = new MetadataDeserializer(JsonFilename);
      var metadata = metadataDeserializer.Deserialize();

      var usedAttributes = trainingData.Concat(testData).SelectMany(pair => pair.FindingAttributes.Concat(pair.LossAttributes).Select(a => a.Key));
      var usedMetadata = metadata.Where(attr => usedAttributes.Contains(attr.Attribute.ID));
      var actualMetadata = usedMetadata.Where(data => data.Attribute.ID != null).ToDictionary(data => data.Attribute.ID);

      return new LearningData(actualMetadata, trainingData, testData);
    }
  }
}