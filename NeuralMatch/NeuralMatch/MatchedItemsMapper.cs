using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ColorMine.ColorSpaces;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;
using Rubicon.NovaFind.MatchService.Messages.Attributes;

namespace NeuralMatch
{
  public class MatchedItemsMapper : IItemMapper
  {
    private static readonly ILog Logger = LogManager.GetLogger(typeof(MatchedItemsMapper));

    private readonly IItemMapperSettings _settings;

    public MatchedItemsMapper(IItemMapperSettings settings)
    {
      _settings = settings;
    }


    public MatchingPair MapNonMatch(LostAndFoundIndexedItem loss, LostAndFoundIndexedItem finding)
    {
      var pair = Map(loss, finding);
      pair.PercentMatch = -1;
      return pair;
    }

    public MatchingPair MapMatch(LostAndFoundIndexedItem loss, LostAndFoundIndexedItem finding)
    {
      var pair = Map(loss, finding);
      pair.PercentMatch = 1;
      return pair;
    }

    public MatchingPair Map(LostAndFoundIndexedItem loss, LostAndFoundIndexedItem finding)
    {
      var pair = new MatchingPair
      {
        LossDateOfIncident = MapDate(loss.DateOfIncident),
        FindingDateOfIncident = MapDate(finding.DateOfIncident),

        /*LossEasyFindNo = MapIdentifier(loss.EasyFindNo),
        FindingEasyFindNo = MapIdentifier(finding.EasyFindNo),
        LossBagTag = MapIdentifier(loss.BagTag),
        FindingBagTag = MapIdentifier(loss.BagTag),*/



        // TODO:
//        Description
//        LossLocation
      };

      pair.LossCategory = MapCategory(loss.CategoryID, loss.SubCategoryID);
      pair.FindingCategory = MapCategory(finding.CategoryID, finding.SubCategoryID);

      pair.LossColors = MapColors(loss.Attributes.OfType<ColorValueAttribute>());
      pair.FindingColors = MapColors(finding.Attributes.OfType<ColorValueAttribute>());

      //*
      MapBool(pair.LossAttributes, loss.Attributes.OfType<BoolAttribute>());
      MapBool(pair.FindingAttributes, finding.Attributes.OfType<BoolAttribute>());
      //*/

      MapDouble(pair.LossAttributes, loss.Attributes.OfType<DoubleAttribute>());
      MapDouble(pair.FindingAttributes, finding.Attributes.OfType<DoubleAttribute>());

      pair.LossMoney = MapMoney(loss.Attributes.OfType<MoneyValueAttribute>());
      pair.FindingMoney = MapMoney(finding.Attributes.OfType<MoneyValueAttribute>());

      MapInteger(pair.LossAttributes, loss.Attributes.OfType<IntegerAttribute>());
      MapInteger(pair.FindingAttributes, finding.Attributes.OfType<IntegerAttribute>());

      return pair;
    }

    /// <summary>
    /// Map categories to vector space
    /// SubCategories of the same Category should be closer together
    /// </summary>
    public double MapCategory(string categoryId, string subCategoryId)
    {
      if (!_settings.CategoryIndex.ContainsKey(categoryId))
      {
        Logger.ErrorFormat("Category {0} is not in the CategoryIndex dictionary", categoryId);
        return 0;
      }
      if (!_settings.SubCategoryIndex[categoryId].ContainsKey(subCategoryId))
      {
        Logger.ErrorFormat("SubCategory {0} is not in the SubCategoryIndex dictionary", subCategoryId);
        return 0;
      }

      var value = (double)(_settings.CategoryIndex[categoryId] + _settings.SubCategoryIndex[categoryId][subCategoryId]) / _settings.CategoryCount;
      return value * 2 - 1;
    }

    /// <summary>
    /// Map to vector space
    /// </summary>
    public double MapDate(DateTime date)
    {
      if (date.Year < 2000) // date is not nullable. default in the data is 31.12.1999
        return 0;
      var toOld = date - _settings.OldestDate;
      var ratio = (double)toOld.Ticks / _settings.DateRange.Ticks;
      return ratio * 2 - 1;
    }

    /// <summary>
    /// Map alphanumeric identification number to vector
    /// </summary>
    public double MapIdentifier(string identifier)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Map string to vector
    /// </summary>
    public double MapText(string text)
    {
      throw new NotImplementedException();
    }

    private static readonly double[] NoColor = {0, 0, 0};
    public double[] MapColors(IEnumerable<ColorValueAttribute> colors)
    {
      var colorConverter = new ColorConverter();
      var color = colors.FirstOrDefault(c => !string.IsNullOrEmpty(c.Value) && c.Value != "#ffffff" && c.Value != "#000000");
      if (color == null)
        return NoColor;

      var convertFromInvariantString = colorConverter.ConvertFromInvariantString(color.Value) as Color?;
      if (convertFromInvariantString == null)
        return NoColor;

      var colorObject = convertFromInvariantString.Value;
      var rgb = new Rgb {R = colorObject.R, G = colorObject.G, B = colorObject.B};
      var lab = rgb.To<Lab>();

      var settings = _settings.ColorAttributes[color.ID];
      return new[]
      {
        ScaleNumber(lab.L / 100, settings.LuminescenceSettings.MinValue, settings.LuminescenceSettings.MaxValue),
        ScaleNumber(lab.A / 100, settings.ASettings.MinValue, settings.ASettings.MaxValue),
        ScaleNumber(lab.B / 100, settings.BSettings.MinValue, settings.BSettings.MaxValue)
      };
    }

    public void MapBool(IDictionary<string, double[]> attributes, IEnumerable<BoolAttribute> bools)
    {
      foreach (var b in bools)
      {
        double value = 0;
        if (b.Value == true)
          value = 1;
        else if (b.Value == false)
          value = -1;
        attributes[b.ID] = new[] { value };
      }
    }

    public void MapInteger(IDictionary<string, double[]> attributes, IEnumerable<IntegerAttribute> integers)
    {
      foreach (var integer in integers)
      {
        attributes[integer.ID] = new[] { ScaleNumber(integer.Value ?? 0, integer.ID) };
      }
    }

    public void MapDouble(IDictionary<string, double[]> attributes, IEnumerable<DoubleAttribute> doubles)
    {
      foreach (var d in doubles)
      {
        attributes[d.ID] = new[] { ScaleNumber(d.Value ?? 0, d.ID) };
      }
    }

    public double MapMoney(IEnumerable<MoneyValueAttribute> moneyValues)
    {
      var moneyValue = moneyValues.FirstOrDefault(money => money.Value != null && money.Value.Value != 0);
      if (moneyValue == null)
        return 0;

      return ScaleNumber(Convert.ToDouble(moneyValue.Value.Value), moneyValue.ID);
    }

    public IEnumerable<double> MapEnum(IEnumerable<EnumValueAttribute> enumValues)
    {
      //TODO: I would need all catalog values for a match. I only have ID and ClassificationTypeName (or similar)
      throw new NotImplementedException();
    }

    private double ScaleNumber(double value, string attributeId)
    {
      return ScaleNumber(value, _settings.Attributes[attributeId].MinValue, _settings.Attributes[attributeId].MaxValue);
    }

    private double ScaleNumber(double value, double minValue, double maxValue)
    {
      if (value == 0.0)
        return value;
      var range = maxValue - minValue;
      return (value - minValue) / range * 2 - 1;
    }
  }
}