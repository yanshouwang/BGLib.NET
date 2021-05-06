namespace BGLib.API
{
    /// <summary>
    /// Timeout mode.
    /// </summary>
    public enum TimeoutMode : byte
    {
        /// <summary>
        /// Repeating timeout: the timer event is triggered at intervals defined with time.
        /// The stack only supports one repeating timer at a time for reliability purposes.
        /// Starting a repeating soft timer removes the current one if any.
        /// </summary>
        Repeating = 0,
        /// <summary>
        /// Single timeout: the timer event is triggered only once after a period defined
        /// with time. There can be up to 8 non-repeating software timers running at the
        /// same time (max number actually depends on the current activities of the stack,
        /// so it might be lower than 8 at times.)
        /// </summary>
        Single = 1,
    }
}