using UnityEngine;

namespace PeopleFlow.Core
{
    /// <summary>
    /// Central game controller using a State Machine pattern.
    /// Manages the game loop: Init → Play → Win/Lose.
    /// 
    /// Responsibilities:
    /// - Countdown timer management
    /// - Win/Lose condition checking
    /// - Level restart flow
    /// - Coordinating with LevelManager, CapacityTracker
    /// 
    /// Execution order is set to -100 so this Awake() runs before all other scripts,
    /// ensuring GameManager.Instance is available when other scripts need it.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            Init,
            Play,
            Win,
            Lose
        }

        [Header("References")]
        [SerializeField] private ObjectPooler objectPooler;

        // Current state
        public GameState CurrentState { get; private set; } = GameState.Init;

        // Timer
        private float _remainingTime;
        private float _levelTime;

        // Gate tracking
        private int _totalGates;
        private int _completedGates;

        // Singleton (simple — acceptable for a prototype/test)
        public static GameManager Instance { get; private set; }
        public ObjectPooler Pool => objectPooler;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventManager.OnGateCompleted += HandleGateCompleted;
            EventManager.OnLevelLose += HandleLevelLose;
        }

        private void OnDisable()
        {
            EventManager.OnGateCompleted -= HandleGateCompleted;
            EventManager.OnLevelLose -= HandleLevelLose;
        }

        private void Update()
        {
            if (CurrentState != GameState.Play) return;

            // Countdown timer
            _remainingTime -= Time.deltaTime;
            EventManager.RaiseTimerChanged(_remainingTime);

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                TransitionTo(GameState.Lose);
                EventManager.RaiseLevelLose("Time's up!");
            }
        }

        // ─── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Called by LevelManager after level setup is complete.
        /// Transitions from Init → Play.
        /// </summary>
        public void StartLevel(float levelTime, int totalGateCount)
        {
            _levelTime = levelTime;
            _remainingTime = levelTime;
            _totalGates = totalGateCount;
            _completedGates = 0;

            TransitionTo(GameState.Play);

            Debug.Log($"[GameManager] Level started — Time: {levelTime}s, Gates: {totalGateCount}");
        }

        /// <summary>
        /// Restarts the current level. Resets all state and notifies systems.
        /// </summary>
        public void RestartLevel()
        {
            TransitionTo(GameState.Init);

            // Return all pooled objects
            if (objectPooler != null)
            {
                objectPooler.ReturnAll();
            }

            // Fire restart BEFORE clearing events so subscribers can handle it
            EventManager.RaiseLevelRestart();

            Debug.Log("[GameManager] Level restarted");
        }

        /// <summary>
        /// Returns whether the game is currently in Play state (accepting input).
        /// </summary>
        public bool IsPlaying => CurrentState == GameState.Play;

        // ─── Private Logic ───────────────────────────────────────────────

        private void HandleGateCompleted(int gateIndex)
        {
            _completedGates++;

            Debug.Log($"[GameManager] Gate {gateIndex} completed ({_completedGates}/{_totalGates})");

            if (_completedGates >= _totalGates)
            {
                TransitionTo(GameState.Win);
                EventManager.RaiseLevelWin();
                Debug.Log("[GameManager] All gates completed — YOU WIN!");
            }
        }

        private void HandleLevelLose(string reason)
        {
            if (CurrentState == GameState.Lose) return; // Prevent double-trigger
            TransitionTo(GameState.Lose);
            Debug.Log($"[GameManager] Level lost — Reason: {reason}");
        }

        private void TransitionTo(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
        }
    }
}
