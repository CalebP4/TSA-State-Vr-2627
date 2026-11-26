using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    public enum Sound
    {
        Click, Hover, Correct, Wrong, Grab, Release, Place, Tick, Thump, Press,
        Complete, Fail, Dial, Ring, Siren, Cough, Cry, Breath, Whoosh
    }

    /// <summary>
    /// Every sound effect is generated in code when first needed, so the project
    /// has no audio files to license. Put a clip at Resources/FirstAidVR/Audio/&lt;Sound&gt;
    /// to replace any generated sound with a recording.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 22050;
        static readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
        static readonly List<AudioSource> pool = new List<AudioSource>();
        static GameObject root;

        public static AudioClip Clip(Sound s)
        {
            if (clips.TryGetValue(s, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>("FirstAidVR/Audio/" + s) ?? Generate(s);
            clips[s] = c;
            return c;
        }

        /// <summary>Plays a sound. With a position it is 3D, otherwise it plays flat (UI).</summary>
        public static void Play(Sound s, Vector3? at = null, float volume = 1f, float pitch = 1f)
        {
            var src = Get();
            if (src == null) return;
            src.transform.position = at ?? Vector3.zero;
            src.spatialBlend = at.HasValue ? 0.85f : 0f;
            src.pitch = pitch;
            src.volume = volume;
            src.clip = Clip(s);
            src.loop = false;
            src.Play();
        }

        /// <summary>A looping source the caller owns (sirens, breathing...).</summary>
        public static AudioSource Loop(Sound s, Transform parent, float volume = 0.5f)
        {
            var go = new GameObject("Loop " + s);
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = Clip(s);
            src.loop = true;
            src.volume = volume;
            src.spatialBlend = 0.9f;
            src.minDistance = 2f;
            src.maxDistance = 40f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
            return src;
        }

        static AudioSource Get()
        {
            if (root == null)
            {
                root = new GameObject("Sfx Pool");
                Object.DontDestroyOnLoad(root);
                pool.Clear();
            }
            foreach (var p in pool) if (p != null && !p.isPlaying) return p;
            if (pool.Count >= 16) return pool[0];
            var go = new GameObject("Sfx");
            go.transform.SetParent(root.transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.minDistance = 1f;
            src.maxDistance = 20f;
            pool.Add(src);
            return src;
        }

        // ---- Synthesis --------------------------------------------------------------------

        static AudioClip Generate(Sound s)
        {
            float[] d;
            switch (s)
            {
                case Sound.Click: d = Tone(0.05f, t => 1500f, Env(0.002f, 0.04f), Wave.Sine, 0.5f); break;
                case Sound.Hover: d = Tone(0.03f, t => 2200f, Env(0.002f, 0.025f), Wave.Sine, 0.15f); break;
                case Sound.Correct: d = Mix(Tone(0.32f, t => 660f, Env(0.005f, 0.25f), Wave.Sine, 0.4f), Delay(Tone(0.4f, t => 990f, Env(0.005f, 0.35f), Wave.Sine, 0.4f), 0.09f)); break;
                case Sound.Wrong: d = Tone(0.35f, t => 220f - 60f * t, Env(0.005f, 0.3f), Wave.Square, 0.18f); break;
                case Sound.Grab: d = Tone(0.08f, t => 500f + 900f * t, Env(0.003f, 0.07f), Wave.Sine, 0.35f); break;
                case Sound.Release: d = Tone(0.08f, t => 900f - 600f * t, Env(0.003f, 0.07f), Wave.Sine, 0.3f); break;
                case Sound.Place: d = Mix(Noise(0.12f, Env(0.002f, 0.1f), 0.25f, 0.3f), Tone(0.15f, t => 180f, Env(0.003f, 0.12f), Wave.Sine, 0.5f)); break;
                case Sound.Tick: d = Tone(0.03f, t => 1000f, Env(0.001f, 0.025f), Wave.Sine, 0.3f); break;
                case Sound.Thump: d = Mix(Tone(0.14f, t => 120f - 60f * t, Env(0.002f, 0.12f), Wave.Sine, 0.8f), Noise(0.06f, Env(0.001f, 0.05f), 0.3f, 0.2f)); break;
                case Sound.Press: d = Tone(0.12f, t => 140f, Env(0.01f, 0.1f), Wave.Sine, 0.5f); break;
                case Sound.Complete: d = Arp(new[] { 523f, 659f, 784f, 1046f }, 0.11f, 0.5f); break;
                case Sound.Fail: d = Arp(new[] { 392f, 330f, 262f }, 0.16f, 0.45f); break;
                case Sound.Dial: d = Mix(Tone(1.0f, t => 350f, Gate(0.0f, 1.0f), Wave.Sine, 0.2f), Tone(1.0f, t => 440f, Gate(0f, 1f), Wave.Sine, 0.2f)); break;
                case Sound.Ring: d = RingTone(); break;
                case Sound.Siren: d = Tone(2.0f, t => 700f + 300f * Mathf.Sin(t * Mathf.PI), Gate(0f, 2f), Wave.Triangle, 0.35f); break;
                case Sound.Cough: d = Mix(Noise(0.18f, Env(0.01f, 0.15f), 0.6f, 0.45f), Delay(Noise(0.15f, Env(0.01f, 0.12f), 0.6f, 0.35f), 0.22f)); break;
                case Sound.Cry: d = CryTone(); break;
                case Sound.Breath: d = Noise(1.6f, t => Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.6f)), 2f), 0.15f, 0.12f); break;
                case Sound.Whoosh: d = Noise(0.3f, Env(0.08f, 0.2f), 0.2f, 0.25f); break;
                default: d = new float[64]; break;
            }
            var clip = AudioClip.Create(s.ToString(), d.Length, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }

        enum Wave { Sine, Square, Triangle }
        delegate float Fn(float t);

        static Fn Env(float attack, float release) => t =>
            t < attack ? t / attack : Mathf.Exp(-(t - attack) / Mathf.Max(0.001f, release) * 3f);

        static Fn Gate(float start, float end) => t => t < start || t > end ? 0f : Mathf.Clamp01(Mathf.Min(t - start, end - t) / 0.02f);

        static float[] Tone(float dur, Fn freq, Fn env, Wave w, float gain)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                phase += freq(t) / Rate;
                float p = (float)(phase - System.Math.Floor(phase));
                float v = w == Wave.Sine ? Mathf.Sin(p * Mathf.PI * 2f) : w == Wave.Square ? (p < 0.5f ? 1f : -1f) : 4f * Mathf.Abs(p - 0.5f) - 1f;
                d[i] = v * env(t) * gain;
            }
            return d;
        }

        static float[] Noise(float dur, Fn env, float lowpass, float gain)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(n);
            float y = 0;
            for (int i = 0; i < n; i++)
            {
                float x = (float)(rng.NextDouble() * 2 - 1);
                y += (x - y) * lowpass;
                d[i] = y * env((float)i / Rate) * gain * 2f;
            }
            return d;
        }

        static float[] Arp(float[] notes, float step, float gain)
        {
            float[] d = new float[0];
            for (int i = 0; i < notes.Length; i++)
            {
                float f = notes[i];
                d = Mix(d, Delay(Tone(0.45f, t => f, Env(0.005f, 0.35f), Wave.Sine, gain * 0.6f), step * i));
            }
            return d;
        }

        static float[] RingTone()
        {
            var a = Tone(0.4f, t => 1300f + (Mathf.Repeat(t * 25f, 1f) < 0.5f ? 0f : 200f), Gate(0f, 0.4f), Wave.Sine, 0.25f);
            return Mix(Mix(a, Delay(a, 0.5f)), Delay(new float[1], 1.6f));
        }

        static float[] CryTone()
        {
            // A soft, wobbling "waah" - clearly a baby breathing and crying again.
            float[] d = new float[0];
            for (int k = 0; k < 3; k++)
            {
                var w = Tone(0.55f, t => 420f + 120f * Mathf.Sin(t * 5.5f) - 80f * t, t => Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.55f)), Wave.Triangle, 0.18f);
                d = Mix(d, Delay(w, k * 0.75f));
            }
            return d;
        }

        static float[] Delay(float[] a, float seconds)
        {
            int off = (int)(seconds * Rate);
            var d = new float[a.Length + off];
            System.Array.Copy(a, 0, d, off, a.Length);
            return d;
        }

        static float[] Mix(float[] a, float[] b)
        {
            var d = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < d.Length; i++)
                d[i] = Mathf.Clamp((i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f), -1f, 1f);
            return d;
        }
    }
}
