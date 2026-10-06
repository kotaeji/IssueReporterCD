using System.Collections.Generic;
using System.Linq;
using IssueReporterCD.Reporting;

namespace IssueReporterCD.ViewModels
{
    /// <summary>A ComboBox entry: the stored value and the label shown to the engineer.</summary>
    public sealed class Choice<T>
    {
        public Choice(T value, string label)
        {
            Value = value;
            Label = label;
        }

        public T Value { get; }

        public string Label { get; }
    }

    public static class Choices
    {
        public static IReadOnlyList<Choice<Frequency>> Frequencies { get; } = Build(
            new[] { Frequency.Unknown, Frequency.Always, Frequency.Intermittent, Frequency.Once }, FieldLabels.Of);

        public static IReadOnlyList<Choice<Reproducible>> Reproducibility { get; } = Build(
            new[] { Reproducible.Unknown, Reproducible.Site, Reproducible.None }, FieldLabels.Of);

        public static IReadOnlyList<Choice<OperatingMode>> OperatingModes { get; } = Build(
            new[] { OperatingMode.Unknown, OperatingMode.Auto, OperatingMode.Manual, OperatingMode.Teaching, OperatingMode.Simulation }, FieldLabels.Of);

        public static IReadOnlyList<Choice<LifecyclePhase>> LifecyclePhases { get; } = Build(
            new[] { LifecyclePhase.Unknown, LifecyclePhase.Boot, LifecyclePhase.Load, LifecyclePhase.Run, LifecyclePhase.Stop, LifecyclePhase.Shutdown }, FieldLabels.Of);

        private static IReadOnlyList<Choice<T>> Build<T>(IEnumerable<T> values, System.Func<T, string> label)
        {
            return values.Select(v => new Choice<T>(v, label(v))).ToList();
        }
    }
}
