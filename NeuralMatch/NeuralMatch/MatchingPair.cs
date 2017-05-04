using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Serialization;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;
using Rubicon.NovaFind.MatchService.Messages.Attributes;

namespace NeuralMatch
{
  [Serializable]
  public class MatchingPair
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchingPair));

    public MatchingPair()
    {
      LossAttributes = new Dictionary<string, double[]>();
      FindingAttributes = new Dictionary<string, double[]>();
    }

    public double[] ToVectorArray(IDictionary<string, IndexableAttributeMetadata> metadataAttributes, IList<string> propertiesToSkip = null)
    {
      var vectors = new List<double> { PercentMatch };
      if (propertiesToSkip == null)
        propertiesToSkip = new List<string>();

      AddProperty(vectors, propertiesToSkip, nameof(LossDateOfIncident), LossDateOfIncident);
      AddProperty(vectors, propertiesToSkip, nameof(FindingDateOfIncident), FindingDateOfIncident);
      AddProperty(vectors, propertiesToSkip, nameof(LossCategory), LossCategory);
      AddProperty(vectors, propertiesToSkip, nameof(FindingCategory), FindingCategory);
      AddProperty(vectors, propertiesToSkip, nameof(LossMoney), LossMoney);
      AddProperty(vectors, propertiesToSkip, nameof(FindingMoney), FindingMoney);

      if (!propertiesToSkip.Contains("LossColors"))
        vectors.AddRange(LossColors);
      if (!propertiesToSkip.Contains("FindingColors"))
        vectors.AddRange(FindingColors);

      foreach (var metadataAttribute in metadataAttributes.Keys)
      {
        AddAttributes(vectors, metadataAttribute, LossAttributes, metadataAttributes);
        AddAttributes(vectors, metadataAttribute, FindingAttributes, metadataAttributes);
      }

      return vectors.ToArray();
    }

    private static void AddProperty(IList<double> vectors, IEnumerable<string> propertiesToSkip, string propertyName, double propertyValue)
    {
      if (!propertiesToSkip.Contains(propertyName))
      {
        vectors.Add(propertyValue);
      }
    }

    private void AddAttributes(List<double> vectors, string metadataAttribute, IDictionary<string, double[]> attributes, IDictionary<string, IndexableAttributeMetadata> metadataAttributes)
    {
      var values = attributes.ContainsKey(metadataAttribute) ? attributes[metadataAttribute] : new double[0];

      if (values.Length < 1)
        values = metadataAttributes[metadataAttribute].Attribute.GetType() == typeof(ColorValueAttribute) ? new double[] {0, 0, 0} : new double[] {0};

      if (values.Length > 1 && metadataAttributes[metadataAttribute].Attribute.GetType() !=
          typeof(ColorValueAttribute) || values.Length > 3)
      {
        Logger.ErrorFormat("Attribute has too many values. Vector would be malformed. {0} with ID {1} has {2} values.",
          metadataAttributes[metadataAttribute].Attribute.GetType().FullName, metadataAttributes[metadataAttribute].Attribute.ID, values.Length);
        throw new ApplicationException($"Attribute {metadataAttributes[metadataAttribute].Attribute.GetType().FullName} has too many values ({values.Length} values)");
      }

      vectors.AddRange(values);
    }

    public double PercentMatch { get; set; } // Range -1 - 1

    public double LossCategory { get; set; }

    public double FindingCategory { get; set; }

    public double LossDateOfIncident { get; set; }

    public double FindingDateOfIncident { get; set; }

    public double LossEasyFindNo { get; set; }

    public double FindingEasyFindNo { get; set; }

    public double LossBagTag { get; set; }

    public double FindingBagTag { get; set; }

    public double LossMoney { get; set; }

    public double FindingMoney { get; set; }

    public double[] LossColors { get; set; }

    public double[] FindingColors { get; set; }

    [XmlIgnore]
    public Dictionary<string, double[]> LossAttributes { get; set; }

    [XmlIgnore]
    public Dictionary<string, double[]> FindingAttributes { get; set; }

    #region wrapper for serialization

    public List<KeyValuePair<string, double[]>> LossAttributesForSerialization
    {
      get { return LossAttributes.Select(pair => pair).ToList(); }
      set { LossAttributes = value?.ToDictionary(pair => pair.Key, pair => pair.Value); }
    }

    public List<KeyValuePair<string, double[]>> FindingAttributesForSerialization
    {
      get { return FindingAttributes.Select(pair => pair).ToList(); }
      set { FindingAttributes = value?.ToDictionary(pair => pair.Key, pair => pair.Value); }
    }

    #endregion

    /*TOOD: string
        text*/
  }
}