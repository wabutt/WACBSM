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

        // Random thread-static para evitar problemas de concurrencia
        [ThreadStatic]
        private static Random _random;
        private static Random Random => _random ?? (_random = new Random(Guid.NewGuid().GetHashCode()));

        /// <summary>
        /// Get anti-blocking delay (adds randomness to appear human-like)
        /// Usa distribución triangular ±40% + micropauses ocasionales
        /// </summary>
        public int GetAntiBlockDelay(int baseDelay)
        {
            if (baseDelay <= 0) return 500;

            // Distribución triangular (60%-140% del tiempo base)
            // Más probabilidad cerca del centro (100%) = más humano
            var u = (GetRandomDouble() + GetRandomDouble()) / 2.0;

            int min = (int)(baseDelay * 0.6);  // 60% del base
            int max = (int)(baseDelay * 1.4);  // 140% del base
            int delayWithJitter = min + (int)Math.Round(u * (max - min));

            // 20% de probabilidad de agregar un micropause extra (1-3 segundos)
            if (GetRandomDouble() < 0.2)
            {
                int micropause = Random.Next(1000, 3000);
                delayWithJitter += micropause;
                Console.WriteLine($"   💤 Micropause humano: +{micropause}ms");
            }

            return Math.Max(500, delayWithJitter); // Mínimo 500ms
        }

        /// <summary>
        /// Obtener double aleatorio [0.0, 1.0)
        /// </summary>
        private static double GetRandomDouble()
        {
            return Random.NextDouble();
        }

        /// <summary>
        /// Obtener trigger de pausa variable (N±2 mensajes)
        /// Si el usuario configuró pausar cada 100, pausa entre 98-102
        /// </summary>
        public int GetVariablePauseTrigger(int baseTrigger)
        {
            if (baseTrigger <= 0) return 0;

            // Variación de ±2 mensajes
            int variance = Random.Next(-2, 3); // -2, -1, 0, 1, 2
            int result = baseTrigger + variance;

            Console.WriteLine($"🎲 Pausa configurada cada {baseTrigger}, pausará en mensaje {result} ({variance:+0;-0;0})");

            return Math.Max(1, result); // Mínimo mensaje 1
        }

        /// <summary>
        /// Obtener duración de pausa variable (12-18 minutos)
        /// En lugar de 15 minutos exactos
        /// </summary>
        public TimeSpan GetVariablePauseDuration()
        {
            // Distribución triangular entre 12 y 18 minutos (pico en 15)
            var u = (GetRandomDouble() + GetRandomDouble()) / 2.0;

            int minMinutes = 12;
            int maxMinutes = 18;
            int minutes = minMinutes + (int)Math.Round(u * (maxMinutes - minMinutes));

            Console.WriteLine($"⏸ Duración de pausa: {minutes} minutos (rango: 12-18 min)");

            return TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Tiempo de "pensar" antes de escribir (0.8-2.5 segundos)
        /// Simula: leer historial, pensar qué escribir
        /// </summary>
        public int GetThinkingDelay()
        {
            // Distribución triangular entre 800ms y 2500ms
            var u = (GetRandomDouble() + GetRandomDouble()) / 2.0;
            int delay = 800 + (int)Math.Round(u * (2500 - 800));

            return delay;
        }

        /// <summary>
        /// Tiempo de "revisar" antes de enviar (0.4-1.8 segundos)
        /// Simula: revisar el mensaje escrito antes de presionar Enter
        /// </summary>
        public int GetReviewDelay()
        {
            // Distribución triangular entre 400ms y 1800ms
            var u = (GetRandomDouble() + GetRandomDouble()) / 2.0;
            int delay = 400 + (int)Math.Round(u * (1800 - 400));

            return delay;
        }
    }
}
