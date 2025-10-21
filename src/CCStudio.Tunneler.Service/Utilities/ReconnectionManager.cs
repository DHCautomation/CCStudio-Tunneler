using Microsoft.Extensions.Logging;

namespace CCStudio.Tunneler.Service.Utilities;

/// <summary>
/// Manages automatic reconnection with exponential backoff
/// </summary>
public class ReconnectionManager
{
    private readonly ILogger _logger;
    private readonly string _connectionName;
    private readonly int _initialDelay;
    private readonly int _maxDelay;
    private readonly int _maxAttempts;

    private int _attemptCount = 0;
    private DateTime? _lastAttempt;
    private DateTime? _lastSuccessfulConnection;
    private int _currentDelay;

    public int AttemptCount => _attemptCount;
    public DateTime? LastAttempt => _lastAttempt;
    public DateTime? LastSuccessfulConnection => _lastSuccessfulConnection;
    public bool IsInBackoff => _lastAttempt.HasValue &&
        DateTime.UtcNow < _lastAttempt.Value.AddSeconds(_currentDelay);

    public ReconnectionManager(
        ILogger logger,
        string connectionName,
        int initialDelaySeconds = 5,
        int maxDelaySeconds = 300,
        int maxAttempts = 0)
    {
        _logger = logger;
        _connectionName = connectionName;
        _initialDelay = initialDelaySeconds;
        _maxDelay = maxDelaySeconds;
        _maxAttempts = maxAttempts;
        _currentDelay = initialDelaySeconds;
    }

    /// <summary>
    /// Checks if reconnection should be attempted
    /// </summary>
    public bool ShouldAttemptReconnection()
    {
        // Check if max attempts reached (0 = infinite)
        if (_maxAttempts > 0 && _attemptCount >= _maxAttempts)
        {
            _logger.LogWarning(
                "{ConnectionName}: Max reconnection attempts ({MaxAttempts}) reached",
                _connectionName, _maxAttempts);
            return false;
        }

        // Check if still in backoff period
        if (IsInBackoff)
        {
            var remainingSeconds = (_lastAttempt.Value.AddSeconds(_currentDelay) - DateTime.UtcNow).TotalSeconds;
            _logger.LogDebug(
                "{ConnectionName}: In backoff period, {Remaining:F0}s remaining",
                _connectionName, remainingSeconds);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Records a reconnection attempt
    /// </summary>
    public void RecordAttempt()
    {
        _attemptCount++;
        _lastAttempt = DateTime.UtcNow;

        // Calculate next delay with exponential backoff
        if (_attemptCount > 1)
        {
            _currentDelay = Math.Min(_currentDelay * 2, _maxDelay);
        }

        _logger.LogInformation(
            "{ConnectionName}: Reconnection attempt {Attempt}{MaxInfo}, next delay: {Delay}s",
            _connectionName,
            _attemptCount,
            _maxAttempts > 0 ? $"/{_maxAttempts}" : "",
            _currentDelay);
    }

    /// <summary>
    /// Records a successful connection
    /// </summary>
    public void RecordSuccess()
    {
        var wasReconnecting = _attemptCount > 0;

        _lastSuccessfulConnection = DateTime.UtcNow;
        _attemptCount = 0;
        _currentDelay = _initialDelay;
        _lastAttempt = null;

        if (wasReconnecting)
        {
            _logger.LogInformation(
                "{ConnectionName}: Reconnected successfully after {Attempts} attempts",
                _connectionName,
                _attemptCount);
        }
        else
        {
            _logger.LogInformation(
                "{ConnectionName}: Connected successfully",
                _connectionName);
        }
    }

    /// <summary>
    /// Records a failed connection attempt
    /// </summary>
    public void RecordFailure(string? reason = null)
    {
        _logger.LogWarning(
            "{ConnectionName}: Connection attempt {Attempt} failed{Reason}. Will retry in {Delay}s",
            _connectionName,
            _attemptCount,
            string.IsNullOrEmpty(reason) ? "" : $": {reason}",
            _currentDelay);
    }

    /// <summary>
    /// Resets the reconnection state
    /// </summary>
    public void Reset()
    {
        _attemptCount = 0;
        _currentDelay = _initialDelay;
        _lastAttempt = null;

        _logger.LogDebug("{ConnectionName}: Reconnection manager reset", _connectionName);
    }

    /// <summary>
    /// Gets the current delay in seconds
    /// </summary>
    public int GetCurrentDelay()
    {
        return _currentDelay;
    }

    /// <summary>
    /// Gets a delay schedule for display purposes
    /// </summary>
    public List<int> GetDelaySchedule(int count = 10)
    {
        var schedule = new List<int>();
        var delay = _initialDelay;

        for (int i = 0; i < count; i++)
        {
            schedule.Add(delay);
            delay = Math.Min(delay * 2, _maxDelay);
        }

        return schedule;
    }
}
