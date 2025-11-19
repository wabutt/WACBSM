using System;

namespace Presentation.Models
{
    /// <summary>
    /// Tracks progress of message sending operation
    /// </summary>
    public class ProgressModel
    {
        /// <summary>
        /// Total number of messages to send
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Number of messages successfully sent
        /// </summary>
        public int SentMessages { get; set; }

        /// <summary>
        /// Number of messages that failed
        /// </summary>
        public int FailedMessages { get; set; }

        /// <summary>
        /// Current contact being processed (0-based index)
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary>
        /// Percentage complete (0-100)
        /// </summary>
        public int PercentComplete
        {
            get
            {
                if (TotalMessages == 0) return 0;
                return (int)((double)(SentMessages + FailedMessages) / TotalMessages * 100);
            }
        }

        /// <summary>
        /// Number of messages remaining
        /// </summary>
        public int RemainingMessages => TotalMessages - SentMessages - FailedMessages;

        /// <summary>
        /// Whether all messages have been processed
        /// </summary>
        public bool IsComplete => (SentMessages + FailedMessages) >= TotalMessages;

        public ProgressModel()
        {
            Reset();
        }

        public ProgressModel(int totalMessages)
        {
            TotalMessages = totalMessages;
            SentMessages = 0;
            FailedMessages = 0;
            CurrentIndex = 0;
        }

        /// <summary>
        /// Reset progress to initial state
        /// </summary>
        public void Reset()
        {
            TotalMessages = 0;
            SentMessages = 0;
            FailedMessages = 0;
            CurrentIndex = 0;
        }

        /// <summary>
        /// Increment sent count
        /// </summary>
        public void IncrementSent()
        {
            SentMessages++;
        }

        /// <summary>
        /// Increment failed count
        /// </summary>
        public void IncrementFailed()
        {
            FailedMessages++;
        }

        public override string ToString()
        {
            return $"Progress: {SentMessages}/{TotalMessages} sent, {FailedMessages} failed ({PercentComplete}%)";
        }
    }
}
