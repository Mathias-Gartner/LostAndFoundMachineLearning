using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using ColorMine.ColorSpaces;
using Rubicon.NovaFind.MatchService.Messages;
using Rubicon.NovaFind.MatchService.Messages.Attributes;

namespace NeuralMatch
{
  public class ItemMapperSettings : IItemMapperSettings
  {
    public static ItemMapperSettings FromDeserialized(IDeserializedData deserializedData)
    {
      var settings = new ItemMapperSettings();

      GetSettingsFromCatgoryHierarchy(settings, deserializedData.CategoryHierarchy);
      GetSettingsFromItems(settings, deserializedData.Items);

      return settings;
    }

    private static void GetSettingsFromCatgoryHierarchy(ItemMapperSettings settings, IDictionary<string, IEnumerable<string>> categoryHierarchy)
    {
      settings.CategoryCount = categoryHierarchy.Keys.Count * 10 + categoryHierarchy.Values.SelectMany(subCategories => subCategories).Count();
      settings.SubCategoryIndex = categoryHierarchy.ToDictionary(pair => pair.Key,
        pair => pair.Value.ToDictionary(subCategory => subCategory, subCategory => pair.Value.ToList().IndexOf(subCategory)));

      var categoryIndex = new Dictionary<string, int>();
      var index = 0;
      foreach (var category in categoryHierarchy.Keys)
      {
        categoryIndex.Add(category, index);
        index += categoryHierarchy[category].Count();
        index += 10;
      }
      settings.CategoryIndex = categoryIndex;
    }

    // TODO: FOr colors, money usw.: only one range for all attributes
    private static void GetSettingsFromItems(ItemMapperSettings settings, IEnumerable<LostAndFoundIndexedItem> items)
    {
      var colorConverter = new ColorConverter();

      foreach (var item in items)
      {
        if (settings.OldestDate > item.DateOfIncident)
          settings.OldestDate = item.DateOfIncident;
        if (settings.NewestDate < item.DateOfIncident)
          settings.OldestDate = item.DateOfIncident;

        foreach (var attribute in item.Attributes)
        {
          if (attribute is ColorValueAttribute)
          {
            var convertFromInvariantString = colorConverter.ConvertFromInvariantString(attribute.GetValue().ToString()) as Color?;
            if (convertFromInvariantString == null)
              continue;

            var colorObject = convertFromInvariantString.Value;
            var rgb = new Rgb {R = colorObject.R, G = colorObject.G, B = colorObject.B};
            var lab = rgb.To<Lab>();

            IColorAttributeMapperSettings colorAttributeSettings;
            if (settings.ColorAttributes.ContainsKey(attribute.ID))
              colorAttributeSettings = settings.ColorAttributes[attribute.ID];
            else
            {
              colorAttributeSettings = new ColorAttributeMapperSettings();
              settings.ColorAttributes.Add(attribute.ID, colorAttributeSettings);
            }

            if (lab.L < colorAttributeSettings.LuminescenceSettings.MinValue)
              colorAttributeSettings.LuminescenceSettings.MinValue = lab.L;
            if (lab.L > colorAttributeSettings.LuminescenceSettings.MaxValue)
              colorAttributeSettings.LuminescenceSettings.MaxValue = lab.L;
            if (lab.A < colorAttributeSettings.ASettings.MinValue)
              colorAttributeSettings.ASettings.MinValue = lab.A;
            if (lab.A > colorAttributeSettings.ASettings.MaxValue)
              colorAttributeSettings.ASettings.MaxValue = lab.A;
            if (lab.B < colorAttributeSettings.BSettings.MinValue)
              colorAttributeSettings.BSettings.MinValue = lab.B;
            if (lab.B > colorAttributeSettings.BSettings.MaxValue)
              colorAttributeSettings.BSettings.MaxValue = lab.B;

            continue;
          }

          IAttributeMapperSettings attributeSettings;
          if (settings.Attributes.ContainsKey(attribute.ID))
            attributeSettings = settings.Attributes[attribute.ID];
          else
          {
            attributeSettings = new AttributeMapperSettings();
            settings.Attributes.Add(attribute.ID, attributeSettings);
          }
          attributeSettings.DataCount++;

          var value = attribute.GetValue();

          if (value is string)
            continue;

          var moneyValue = value as MoneyValue;
          if (moneyValue != null)
          {
            if (Convert.ToDouble(moneyValue.Value) < attributeSettings.MinValue)
              attributeSettings.MinValue = Convert.ToDouble(moneyValue.Value);
            if (Convert.ToDouble(moneyValue.Value) > attributeSettings.MaxValue)
              attributeSettings.MaxValue = Convert.ToDouble(moneyValue.Value);
            continue;
          }

          var convertible = value as IConvertible;
          if (convertible == null)
            continue;

          var doubleValue = convertible.ToDouble(CultureInfo.InvariantCulture);
          if (doubleValue < attributeSettings.MinValue)
            attributeSettings.MinValue = doubleValue;
          if (doubleValue > attributeSettings.MaxValue)
            attributeSettings.MaxValue = doubleValue;
        }
      }

      settings.DateRange = settings.NewestDate - settings.OldestDate;
    }

    public ItemMapperSettings()
    {
      OldestDate = DateTime.MaxValue;
      NewestDate = DateTime.MinValue;
      DateRange = TimeSpan.Zero;

      Attributes = new Dictionary<string, IAttributeMapperSettings>();
      ColorAttributes = new Dictionary<string, IColorAttributeMapperSettings>();
    }

    public TimeSpan DateRange { get; set; }
    public DateTime OldestDate { get; set; }
    public DateTime NewestDate { get; set; }

    public int CategoryCount { get; set; }
    public Dictionary<string, int> CategoryIndex { get; set; }
    public Dictionary<string, Dictionary<string, int>> SubCategoryIndex { get; set; }

    public IDictionary<string, IAttributeMapperSettings> Attributes { get; }
    public IDictionary<string, IColorAttributeMapperSettings> ColorAttributes { get; }

    // TODO Apply Attribute Dictionary refactoring
    public override string ToString()
    {
      var stringBuilder = new StringBuilder();
      foreach (var property in typeof(IItemMapperSettings).GetProperties())
      {
        if (stringBuilder.Length > 0)
          stringBuilder.Append(", ");

        stringBuilder.Append(property.Name);
        stringBuilder.Append(": ");
        stringBuilder.Append(property.GetValue(this));
      }
      return stringBuilder.ToString();
    }
  }

  public class ColorAttributeMapperSettings : IColorAttributeMapperSettings
  {
    public ColorAttributeMapperSettings()
    {
      LuminescenceSettings = new AttributeMapperSettings();
      ASettings = new AttributeMapperSettings();
      BSettings = new AttributeMapperSettings();
    }

    public IAttributeMapperSettings LuminescenceSettings { get; }
    public IAttributeMapperSettings ASettings { get; }
    public IAttributeMapperSettings BSettings { get; }
  }

  public class AttributeMapperSettings : IAttributeMapperSettings
  {
    public AttributeMapperSettings()
    {
      MinValue = double.MaxValue;
      MaxValue = double.MinValue;
    }

    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public long DataCount { get; set; }
  }
}
