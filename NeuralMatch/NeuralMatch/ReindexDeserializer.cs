using System;
using System.Collections.Generic;
using System.IO;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class ReindexDeserializer
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(ReindexDeserializer));

    private readonly string _filename;

    public ReindexDeserializer(string filename)
    {
      _filename = filename;
    }

    /// <summary>
    /// Deserializes the given file. Throws exception if not successful.
    /// </summary>
    public IDeserializedData Deserialize()
    {
      var deserialedData = new DeserializedData();
      var jsonSerializerSettings = new JsonSerializerSettings
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        TypeNameHandling = TypeNameHandling.Auto
      };
      jsonSerializerSettings.Converters.Add(new StringEnumConverter {CamelCaseText = true});
      var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);

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
          deserialedData.AttributeMetadata.Add(attributeMetadata);
        }

        int count = 0;
        while (jsonReader.Path.StartsWith("Items"))
        {
          if (jsonReader.TokenType != JsonToken.StartObject)
          {
            jsonReader.Read();
            continue;
          }
          var item = jsonSerializer.Deserialize<LostAndFoundIndexedItem>(jsonReader);
          count++;
          if (count % 10000 == 0)
            Logger.DebugFormat("{0} objects deserialized", count);
          deserialedData.Items.Add(item);
        }

        while (jsonReader.Path.StartsWith("CategoryIDMapping"))
        {
          if (jsonReader.TokenType != JsonToken.StartObject)
          {
            jsonReader.Read();
            continue;
          }
          deserialedData.CategoryIdMapping = jsonSerializer.Deserialize<Dictionary<string, string>>(jsonReader);
        }

        while (jsonReader.Path.StartsWith("SubCategoryIDMapping"))
        {
          if (jsonReader.TokenType != JsonToken.StartObject)
          {
            jsonReader.Read();
            continue;
          }
          deserialedData.SubCategoryIdMapping = jsonSerializer.Deserialize<Dictionary<string, string>>(jsonReader);
        }

        while (jsonReader.Path.StartsWith("CategoryHierarchy"))
        {
          if (jsonReader.TokenType != JsonToken.StartObject)
          {
            jsonReader.Read();
            continue;
          }
          deserialedData.CategoryHierarchy = jsonSerializer.Deserialize<Dictionary<string, IEnumerable<string>>>(jsonReader);
        }
      }

      Logger.Info("JSON Derserialization successful");
      return deserialedData;
    }
  }
}