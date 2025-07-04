namespace BuildingBlocks.MessagingEvent.Base
{
    /// <summary>
    /// Base record for all integration events
    /// Designed for future microservices migration with versioning and correlation support
    /// </summary>
    public abstract record IntegrationEvent
    {
        /// <summary>
        /// Unique identifier for each event instance
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// When the event occurred (UTC)
        /// </summary>
        public DateTime OccurredOn { get; } = DateTime.UtcNow;

        /// <summary>
        /// Event type for serialization/deserialization
        /// </summary>
        public string EventType => GetType().Name;

        /// <summary>
        /// Source service/module that originated the event
        /// </summary>
        public string Source { get; init; } = string.Empty;

        /// <summary>
        /// Correlation ID for distributed tracing (will be essential for microservices)
        /// </summary>
        public string? CorrelationId { get; init; }

        /// <summary>
        /// Event version for future schema evolution
        /// </summary>
        public string Version { get; init; } = "1.0";
    }
}