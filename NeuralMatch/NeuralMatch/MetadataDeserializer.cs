using System.Collections.Generic;
using System.IO;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class MetadataDeserializer
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(MetadataDeserializer));

    private readonly string _filename;

    public MetadataDeserializer(string filename)
    {
      _filename = filename;
    }

    public IList<IndexableAttributeMetadata> Deserialize()
    {
      var jsonSerializerSettings = new JsonSerializerSettings
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        TypeNameHandling = TypeNameHandling.Auto
      };
      jsonSerializerSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
      var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);

      var metadata = new List<IndexableAttributeMetadata>();

      using (var fileStream = File.OpenRead(_filename))
      using (var fileReader = new StreamReader(fileStream))
      using (var jsonReader = new JsonTextReader(fileReader))
      {
        jsonReader.Read(); // Move to root element
        jsonReader.Read(); // Read into root element

        while (jsonReader.Path.StartsWith("Metadata"))
        {
          if (jsonReader.TokenType != JsonToken.StartObject)
          {
            jsonReader.Read();
            continue;
          }
          var attributeMetadata = jsonSerializer.Deserialize<IndexableAttributeMetadata>(jsonReader);
          metadata.Add(attributeMetadata);
        }
        return metadata;
      }
    }
  }
}