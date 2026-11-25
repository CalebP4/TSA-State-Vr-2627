using System.IO;
using UnityEngine;

namespace FirstAidVR
{
    /// <summary>
    /// Saves every finished run to a small JSON file on the device
    /// (Application.persistentDataPath/firstaid_results.json). No accounts, no cloud.
    /// </summary>
    public static class ResultsStore
    {
        public const int MaxStored = 100;
        public static string FileName = "firstaid_results.json";
        static ResultHistory cache;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static ResultHistory Load()
        {
            if (cache != null) return cache;
            cache = new ResultHistory();
            try
            {
                if (File.Exists(FilePath))
                    cache = JsonUtility.FromJson<ResultHistory>(File.ReadAllText(FilePath)) ?? new ResultHistory();
            }
            catch (System.Exception e) { Debug.LogWarning("[FA] Could not read results: " + e.Message); }
            return cache;
        }

        public static void Save(ScenarioResult result)
        {
            var h = Load();
            h.results.Add(result);
            while (h.results.Count > MaxStored) h.results.RemoveAt(0);
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(h, true)); }
            catch (System.Exception e) { Debug.LogWarning("[FA] Could not save results: " + e.Message); }
        }

        /// <summary>Best overall score for a scenario and mode, or -1 if never played.</summary>
        public static int Best(string scenarioId, SimMode mode)
        {
            int best = -1;
            foreach (var r in Load().results)
                if (r.scenarioId == scenarioId && r.mode == mode && r.overallScore > best) best = r.overallScore;
            return best;
        }

        public static bool EverPassed(string scenarioId)
        {
            foreach (var r in Load().results)
                if (r.scenarioId == scenarioId && r.mode == SimMode.Certification && r.passed) return true;
            return false;
        }

        public static void ClearAll()
        {
            cache = new ResultHistory();
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
