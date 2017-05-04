namespace NeuralMatch
{
  public interface IDataLoader
  {
    LearningData Load();
    bool UseLegacyData { get; }
  }
}