using System;

namespace PeopleFlow.Core
{
    /// <summary>
    /// Central event hub using the Observer pattern.
    /// All game systems communicate through events here, keeping Logic and UI decoupled.
    /// 
    /// Usage:
    ///   EventManager.OnCorrectMinion += HandleCorrectMinion;   // Subscribe
    ///   EventManager.OnCorrectMinion -= HandleCorrectMinion;   // Unsubscribe
    ///   EventManager.RaiseCorrectMinion(gateId);               // Fire
    /// </summary>
    public static class EventManager
    {
        // ─── Minion Events ───────────────────────────────────────────────

        /// <summary>Fired when a Minion is spawned from a Station.</summary>
        public static event Action<MinionType> OnMinionSpawned;

        /// <summary>Fired when a correct-type Minion enters a Gate. Param: gate index.</summary>
        public static event Action<int> OnCorrectMinion;

        /// <summary>Fired when a wrong-type Minion enters a Gate. Param: gate index.</summary>
        public static event Action<int> OnWrongMinion;

        // ─── Capacity Events ─────────────────────────────────────────────

        /// <summary>Fired when capacity changes. Params: (currentUsed, maxCapacity).</summary>
        public static event Action<int, int> OnCapacityChanged;

        // ─── Gate Events ─────────────────────────────────────────────────

        /// <summary>Fired when a single Gate is completed (remainingCount == 0). Param: gate index.</summary>
        public static event Action<int> OnGateCompleted;

        // ─── Timer Events ────────────────────────────────────────────────

        /// <summary>Fired every frame during gameplay with remaining time in seconds.</summary>
        public static event Action<float> OnTimerChanged;

        // ─── Game State Events ───────────────────────────────────────────

        /// <summary>Fired when the level is won.</summary>
        public static event Action OnLevelWin;

        /// <summary>Fired when the level is lost. Param: reason string.</summary>
        public static event Action<string> OnLevelLose;

        /// <summary>Fired when the level is restarted.</summary>
        public static event Action OnLevelRestart;

        // ─── Raise Methods ───────────────────────────────────────────────

        public static void RaiseMinionSpawned(MinionType type)
        {
            OnMinionSpawned?.Invoke(type);
        }

        public static void RaiseCorrectMinion(int gateIndex)
        {
            OnCorrectMinion?.Invoke(gateIndex);
        }

        public static void RaiseWrongMinion(int gateIndex)
        {
            OnWrongMinion?.Invoke(gateIndex);
        }

        public static void RaiseCapacityChanged(int currentUsed, int maxCapacity)
        {
            OnCapacityChanged?.Invoke(currentUsed, maxCapacity);
        }

        public static void RaiseGateCompleted(int gateIndex)
        {
            OnGateCompleted?.Invoke(gateIndex);
        }

        public static void RaiseTimerChanged(float remainingTime)
        {
            OnTimerChanged?.Invoke(remainingTime);
        }

        public static void RaiseLevelWin()
        {
            OnLevelWin?.Invoke();
        }

        public static void RaiseLevelLose(string reason)
        {
            OnLevelLose?.Invoke(reason);
        }

        public static void RaiseLevelRestart()
        {
            OnLevelRestart?.Invoke();
        }

        /// <summary>
        /// Clears all event subscriptions. Call this on scene unload or level restart
        /// to prevent memory leaks and stale references.
        /// </summary>
        public static void ClearAll()
        {
            OnMinionSpawned = null;
            OnCorrectMinion = null;
            OnWrongMinion = null;
            OnCapacityChanged = null;
            OnGateCompleted = null;
            OnTimerChanged = null;
            OnLevelWin = null;
            OnLevelLose = null;
            OnLevelRestart = null;
        }
    }
}
