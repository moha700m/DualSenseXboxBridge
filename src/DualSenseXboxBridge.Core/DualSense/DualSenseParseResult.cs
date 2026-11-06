namespace DualSenseXboxBridge.Core.DualSense;

public enum DualSenseParseFailure
{
    None = 0,
    ReportTooShort,
    UnsupportedReportId,
    BluetoothReport,
    ReadFailed
}

/// <summary>Result of parsing one HID input report.</summary>
public readonly struct DualSenseParseResult
{
    private DualSenseParseResult(bool success, DualSenseInputState state, DualSenseParseFailure failure, string message)
    {
        Success = success;
        State = state;
        Failure = failure;
        Message = message;
    }

    public bool Success { get; }

    public DualSenseInputState State { get; }

    public DualSenseParseFailure Failure { get; }

    public string Message { get; }

    public static DualSenseParseResult Ok(DualSenseInputState state) =>
        new(true, state, DualSenseParseFailure.None, string.Empty);

    public static DualSenseParseResult Fail(DualSenseParseFailure failure, string message) =>
        new(false, default, failure, message);
}
