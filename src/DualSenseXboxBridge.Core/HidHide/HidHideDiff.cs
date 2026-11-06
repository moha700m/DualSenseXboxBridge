namespace DualSenseXboxBridge.Core.HidHide;

/// <summary>Pure list comparison used to restore a HidHide configuration exactly.</summary>
public static class HidHideDiff
{
    public static HidHideDiffResult Compute(
        IEnumerable<string> current,
        IEnumerable<string> desired)
    {
        var currentSet = new HashSet<string>(current.Where(v => !string.IsNullOrWhiteSpace(v)), StringComparer.OrdinalIgnoreCase);
        var desiredSet = new HashSet<string>(desired.Where(v => !string.IsNullOrWhiteSpace(v)), StringComparer.OrdinalIgnoreCase);

        var add = desiredSet.Where(value => !currentSet.Contains(value)).ToList();
        var remove = currentSet.Where(value => !desiredSet.Contains(value)).ToList();
        return new HidHideDiffResult(add, remove);
    }
}

public sealed record HidHideDiffResult(IReadOnlyList<string> ToAdd, IReadOnlyList<string> ToRemove);
