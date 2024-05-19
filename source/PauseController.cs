using EFT;
using EFT.UI.BattleTimer;
using UnityEngine;
using Comfort.Common;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Linq;
using HarmonyLib;

namespace Pause
{
    public class PauseController : MonoBehaviour
    {
        internal static bool IsPaused { get; private set; } = false;

        private DateTime? _pausedDate;
        private DateTime? _unpausedDate;

        private static GameWorld _gameWorld;
        private static Player _mainPlayer;

        private GameTimerClass _gameTimerClass;
        private MainTimerPanel _mainTimerPanel;
        private AbstractGame _abstractGame;

        private static FieldInfo _mouseLookControlField;

        private static FieldInfo _startTimeField;
        private static FieldInfo _escapeTimeField;
        private static FieldInfo _timerPanelField;
        private static FieldInfo _gameDateTimeField;

        internal static ManualLogSource Logger;

        private static readonly string[] TargetBones =
        {
            "calf",
            "foot",
            "toe",
            "spine2",
            "spine3",
            "forearm",
            "neck"
        };

        private List<AudioSource> _pausedAudioSources;

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(PauseController));

            IsPaused = false;
            _abstractGame = GameObject.FindObjectOfType<AbstractGame>();
            _mainTimerPanel = GameObject.FindObjectOfType<MainTimerPanel>();
            _gameTimerClass = _abstractGame?.GameTimer;

            _mouseLookControlField = AccessTools.Field(typeof(Player), "_mouseLookControl");

            _startTimeField = typeof(GameTimerClass).GetField("nullable_0", BindingFlags.Instance | BindingFlags.NonPublic);
            _escapeTimeField = typeof(GameTimerClass).GetField("nullable_1", BindingFlags.Instance | BindingFlags.NonPublic);
            _timerPanelField = typeof(TimerPanel).GetField("dateTime_0", BindingFlags.Instance | BindingFlags.NonPublic);
            _gameDateTimeField = typeof(GameDateTime).GetField("_realtimeSinceStartup", BindingFlags.Instance | BindingFlags.NonPublic);

            _pausedAudioSources = new List<AudioSource>();
        }

        private void Update()
        {
            if (IsKeyPressed(Plugin.TogglePause.Value))
            {
                IsPaused = !IsPaused;

                if (IsPaused)
                {
                    Pause();
                }
                else
                {
                    Unpause();
                }
            }
        }

        private void Pause()
        {
            Time.timeScale = 0f;
            _pausedDate = DateTime.UtcNow;

            _mainPlayer.enabled = false;
            SetMouseLookControl(false);
            _mainPlayer.PauseAllEffectsOnPlayer();

            foreach (var player in GetPlayers())
            {
                if (player.IsYourPlayer) continue;

                Logger.LogInfo($"Deactivating player: {player?.name}");
                SetPlayerState(player, false);
            }

            PauseAllAudio();
            ShowTimer();
        }

        private void Unpause()
        {
            Time.timeScale = 1f;
            _unpausedDate = DateTime.UtcNow;

            _mainPlayer.enabled = true;
            SetMouseLookControl(true);
            _mainPlayer.UnpauseAllEffectsOnPlayer();

            foreach (var player in GetPlayers())
            {
                if (player.IsYourPlayer) continue;

                Logger.LogInfo($"Reactivating player: {player?.name}");
                SetPlayerState(player, true);
            }

            ResumeAllAudio();
            StartCoroutine(CoHideTimer());

            UpdateTimers(GetTimePaused());
        }

        private void SetMouseLookControl(bool enable)
        {
            if (_mainPlayer != null)
            {
                _mouseLookControlField.SetValue(_mainPlayer, enable);
            }
        }

        private void PauseAllAudio()
        {
            _pausedAudioSources.Clear();
            foreach (var audioSource in FindObjectsOfType<AudioSource>())
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Pause();
                    _pausedAudioSources.Add(audioSource);
                }
            }
        }

        private void ResumeAllAudio()
        {
            foreach (var audioSource in _pausedAudioSources)
            {
                audioSource.UnPause();
            }
            _pausedAudioSources.Clear();
        }

        private IEnumerable<Player> GetPlayers() => _gameWorld?.AllAlivePlayersList ?? new List<Player>();

        private void SetPlayerState(Player player, bool active)
        {
            foreach (var r in player.PlayerBones.GetComponentsInChildren<Rigidbody>())
            {
                r.velocity = Vector3.zero;
                r.angularVelocity = Vector3.zero;

                if (active)
                {
                    r.WakeUp();
                }
                else
                {
                    r.Sleep();
                }
            }

            var weaponRigidBody = player.HandsController?.ControllerGameObject?.GetComponent<Rigidbody>();
            if (weaponRigidBody != null)
            {
                weaponRigidBody.angularVelocity = Vector3.zero;
                weaponRigidBody.velocity = Vector3.zero;
                weaponRigidBody.Sleep();
            }

            if (!active)
            {
                player.AIData.BotOwner.DecisionQueue.Clear();
                player.gameObject.SetActive(false);
            }
            else
            {
                player.gameObject.SetActive(true);
                player.AIData.BotOwner.CalcGoal();
            }
        }

        private void ShowTimer()
        {
            _mainTimerPanel?.DisplayTimer();
        }

        private IEnumerator CoHideTimer()
        {
            if (_mainTimerPanel != null)
            {
                yield return new WaitForSeconds(4f);
                _mainTimerPanel.HideTimer();
            }
        }

        private TimeSpan GetTimePaused() => (_pausedDate.HasValue && _unpausedDate.HasValue) ? _unpausedDate.Value - _pausedDate.Value : TimeSpan.Zero;

        private void UpdateTimers(TimeSpan timePaused)
        {
            var startDate = _startTimeField.GetValue(_gameTimerClass) as DateTime?;
            var escapeDate = _timerPanelField.GetValue(_mainTimerPanel) as DateTime?;
            var realTimeSinceStartup = (float)_gameDateTimeField.GetValue(_gameWorld.GameDateTime);

            if (startDate.HasValue && escapeDate.HasValue)
            {
                _startTimeField.SetValue(_gameTimerClass, startDate.Value.Add(timePaused));
                _escapeTimeField.SetValue(_gameTimerClass, escapeDate.Value.Add(timePaused));
                _timerPanelField.SetValue(_mainTimerPanel, escapeDate.Value.Add(timePaused));
                _gameDateTimeField.SetValue(_gameWorld.GameDateTime, realTimeSinceStartup + (float)timePaused.TotalSeconds);
            }
        }

        private void OnDestroy()
        {
            IsPaused = false;
            _gameWorld = null;
            _mainPlayer = null;
            Logger = null;
            _pausedAudioSources.Clear();
            _mouseLookControlField = null;
            _startTimeField = null;
            _escapeTimeField = null;
            _timerPanelField = null;
            _gameDateTimeField = null;
        }

        internal static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                _gameWorld = Singleton<GameWorld>.Instance;
                _gameWorld.GetOrAddComponent<PauseController>();
                _mainPlayer = _gameWorld.MainPlayer;
                Logger.LogDebug("PauseController enabled.");
            }
        }

        internal static bool IsKeyPressed(KeyboardShortcut key)
        {
            if (!UnityInput.Current.GetKeyDown(key.MainKey)) return false;
            return key.Modifiers.All(modifier => UnityInput.Current.GetKey(modifier));
        }

        internal static bool IsKeyPressed(KeyCode key) => UnityInput.Current.GetKeyDown(key);
    }
}
