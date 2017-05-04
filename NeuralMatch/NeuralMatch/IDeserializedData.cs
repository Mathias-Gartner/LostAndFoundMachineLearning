using System.Collections.Generic;
using Rubicon.NovaFind.MatchService.Messages;

namespace NeuralMatch
{
  public interface IDeserializedData
  {
    ICollection<LostAndFoundIndexedItem> Items { get; }

    ICollection<IndexableAttributeMetadata> AttributeMetadata { get; }

    IDictionary<string, string> CategoryIdMapping { get; }
    Dictionary<string, string> SubCategoryIdMapping { get; }

    IDictionary<string, IEnumerable<string>> CategoryHierarchy { get; }
  }
}