using System;
using System.Collections.Generic;

namespace NeuralMatch
{
  public interface IItemMapperSettings
  {
    TimeSpan DateRange { get; set; }
    DateTime OldestDate { get; set; }

    int CategoryCount { get; }
    Dictionary<string, int> CategoryIndex { get; }
    Dictionary<string, Dictionary<string, int>> SubCategoryIndex { get; }

    IDictionary<string, IAttributeMapperSettings> Attributes { get; }
    IDictionary<string, IColorAttributeMapperSettings> ColorAttributes { get; }
  }

  public interface IColorAttributeMapperSettings
  {
    IAttributeMapperSettings LuminescenceSettings { get; }
    IAttributeMapperSettings ASettings { get; }
    IAttributeMapperSettings BSettings { get; }
  }

  public interface IAttributeMapperSettings
  {
    double MinValue { get; set; }
    double MaxValue { get; set; }
    long DataCount { get; set; }
  }
}