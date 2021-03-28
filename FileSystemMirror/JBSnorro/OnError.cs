
using System;
using System.Collections.Generic;
/// <summary>
/// A callback when an exception occurs during hooking file system watchers.
/// </summary>
/// <param name="consecutiveExceptions"> The exceptions for each consecutive attempt. Resets when hooked successfully. </param>
/// <returns>whether a new attempt should be made.</returns>
public delegate bool OnError(IReadOnlyList<Exception> consecutiveExceptions);

