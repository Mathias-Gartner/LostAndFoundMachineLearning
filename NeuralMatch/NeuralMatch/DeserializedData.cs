using System.Collections.Generic;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public class DeserializedData : IDeserializedData
  {
    public DeserializedData()
    {
      Items = new List<LostAndFoundIndexedItem>();
      AttributeMetadata = new List<IndexableAttributeMetadata>();
      CategoryIdMapping = new Dictionary<string, string>();
      CategoryHierarchy = new Dictionary<string, IEnumerable<string>>();
    }

    public ICollection<LostAndFoundIndexedItem> Items { get; set; }
    public ICollection<IndexableAttributeMetadata> AttributeMetadata { get; set; }
    public IDictionary<string, string> CategoryIdMapping { get; set; }
    public Dictionary<string, string> SubCategoryIdMapping { get; set; }
    public IDictionary<string, IEnumerable<string>> CategoryHierarchy { get; set; }
  }
}