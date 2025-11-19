using System;
using System.Threading;
using System.Threading.Tasks;

namespace Presentation.Services
{
    /// <summary>
    /// Service for handling timing, delays, and pauses
    /// </summary>
    public class TimingService
    {
        /// <summary>
        /// Apply delay with cancellation support
        /// </summary>
        public async Task ApplyDelayAsync(int milliseconds, CancellationToken token)
        {
            if (milliseconds <= 0)
                return;

            try
            {
                await Task.Delay(milliseconds, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Delay cancelled");
            }
        }

        /// <summary>
        /// Apply delay with multiple cancellation tokens (linked)
        /// </summary>
        public async Task ApplyDelayAsync(int milliseconds, params CancellationToken[] tokens)
        {
            if (milliseconds <= 0 || tokens == null || tokens.Length == 0)
                return;

            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokens))
                {
                    await Task.Delay(milliseconds, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Delay cancelled");
            }
        }

        /// <summary>
        /// Apply pause for specified duration with cancellation support
        /// </summary>
        public async Task ApplyPauseAsync(int seconds, CancellationToken token, Action<string> onPauseStart = null)
        {
            if (seconds <= 0)
                return;

            try
            {
                onPauseStart?.Invoke($"Pausando por {seconds / 60} minutos");
                await Task.Delay(TimeSpan.FromSeconds(seconds), token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Pause cancelled");
            }
        }

        /// <summary>
        /// Apply auto-pause (15 minutes) after certain number of messages
        /// </summary>
        public async Task ApplyAutoPauseAsync(int currentIndex, int pauseAfterCount, CancellationToken token, Action<string> onPauseStart = null)
        {
            if (pauseAfterCount <= 0 || currentIndex != pauseAfterCount)
                return;

            if (token.IsCancellationRequested)
                return;

            try
            {
                onPauseStart?.Invoke($"Pausa automática después de {pauseAfterCount} mensajes.\nEsperando 15 minutos...");
                await Task.Delay(TimeSpan.FromMinutes(15), token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Auto-pause cancelled");
            }
        }

        /// <summary>
        /// Get anti-blocking delay (adds randomness to appear human-like)
        /// </summary>
        public int GetAntiBlockDelay(int baseDelay)
        {
            // Add random variance (±20%) to make timing appear more human
            Random random = new Random();
            int variance = (int)(baseDelay * 0.2);
            int randomOffset = random.Next(-variance, variance);

            return Math.Max(500, baseDelay + randomOffset); // Minimum 500ms
        }
    }
}
