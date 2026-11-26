using System.Collections.Generic;
using UnityEngine;

namespace FirstAidVR
{
    public enum LogKind { Info, Correct, Incorrect, Hint, Milestone }

    public struct LogEntry
    {
        public float time;
        public LogKind kind;
        public string message;

        public override string ToString() => $"[{time,6:0.0}s] {kind,-9} {message}";
    }

    /// <summary>
    /// Time-stamped record of everything that happened during a scenario.
    /// The scoring system and the dashboard timeline read from it.
    /// </summary>
    public class EventLogger
    {
        readonly List<LogEntry> entries = new List<LogEntry>();
        public IReadOnlyList<LogEntry> Entries => entries;

        /// <summary>Also print entries to the Unity console (handy while testing).</summary>
        public static bool EchoToConsole = true;

        public void Log(float time, LogKind kind, string message)
        {
            var e = new LogEntry { time = time, kind = kind, message = message };
            entries.Add(e);
            if (EchoToConsole) Debug.Log("[FA] " + e);
        }

        public void Clear() => entries.Clear();

        public int Count(LogKind kind)
        {
            int n = 0;
            foreach (var e in entries) if (e.kind == kind) n++;
            return n;
        }
    }
}
