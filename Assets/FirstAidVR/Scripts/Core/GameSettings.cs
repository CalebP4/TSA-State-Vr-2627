using System;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>Player options saved with PlayerPrefs. <see cref="Changed"/> fires when any option changes.</summary>
    public static class GameSettings
    {
        public static event Action Changed;

        public static bool SmoothMove
        {
            get => PlayerPrefs.GetInt("fa_smoothMove", 0) == 1;
            set { PlayerPrefs.SetInt("fa_smoothMove", value ? 1 : 0); Save(); }
        }

        public static bool SmoothTurn
        {
            get => PlayerPrefs.GetInt("fa_smoothTurn", 0) == 1;
            set { PlayerPrefs.SetInt("fa_smoothTurn", value ? 1 : 0); Save(); }
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("fa_volume", 0.8f);
            set { PlayerPrefs.SetFloat("fa_volume", Mathf.Clamp01(value)); Save(); }
        }

        /// <summary>Raises the player's view (metres) for seated play.</summary>
        public static float HeightOffset
        {
            get => PlayerPrefs.GetFloat("fa_height", 0f);
            set { PlayerPrefs.SetFloat("fa_height", Mathf.Clamp(value, 0f, 0.6f)); Save(); }
        }

        public static bool Comfort
        {
            get => PlayerPrefs.GetInt("fa_vignette", 1) == 1;
            set { PlayerPrefs.SetInt("fa_vignette", value ? 1 : 0); Save(); }
        }

        static void Save()
        {
            PlayerPrefs.Save();
            AudioListener.volume = Volume;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply() => AudioListener.volume = Volume;
    }
}
