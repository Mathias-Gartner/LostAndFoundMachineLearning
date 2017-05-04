using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using log4net;
using Rubicon.NovaFind.MatchService.Messages;
using Rubicon.NovaFind.MatchService.Messages.Attributes;

namespace NeuralMatch
{
  public class CsvExporter
  {
    private static ILog Logger = LogManager.GetLogger(typeof(CsvExporter));

    private readonly IDictionary<string, IndexableAttributeMetadata> _metadataAttributes;

    public CsvExporter(IDictionary<string, IndexableAttributeMetadata> metadata)
    {
      _metadataAttributes = metadata;
    }

    public void WriteCsv(string filename, IEnumerable<MatchingPair> pairs)
    {
      var written = 0;

      using (var file = File.Open(filename, FileMode.Create))
      using (var writer = new StreamWriter(file))
      {
        WriteHeader(writer);

        foreach (var pair in pairs)
        {
          WritePair(writer, pair);
          written++;
        }
      }
      Logger.InfoFormat("Written {0} data lines to CSV", written);
    }

    public void WriteHeader(TextWriter writer)
    {
      writer.Write("PercentMatch,LossDateOfIncident,FindingDateOfIncident,LossCategory,FindingCategory,LossMoney,FindingMoney,LossColorHue,LossColorSaturation,LossColorBrightness,FindingColorHue,FindingColorSaturation,FindingColorBrightness");
      foreach (var attribute in _metadataAttributes.Keys)
      {
        writer.Write(",loss{0},finding{0}", attribute);
      }
      writer.WriteLine();
    }

    public void WritePair(TextWriter writer, MatchingPair pair)
    {
      var vector = pair.ToVectorArray(_metadataAttributes);
      writer.WriteLine(string.Join(",", vector.Select(number => number.ToString(CultureInfo.InvariantCulture))));
    }
  }
}