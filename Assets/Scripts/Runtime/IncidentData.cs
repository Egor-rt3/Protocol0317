using System;
using UnityEngine;

namespace Protocol0317
{
    [Serializable] public class Incident
    {
        public string id;
        public string title;
        public string description;
        public int requiredPower;
        public int roomIndex; // 0 reactor, 1 pumps, 2 ventilation, 3 airlock
        public bool requiresVent;
        public bool requiresTerminal;
        public bool requiresLight;
        public int reward;
        public int penalty;
    }
    [Serializable] public class IncidentCollection { public Incident[] incidents; }

    // Configuration is validated before it can affect live game state.
    // Invalid external data is rejected; the original state is never modified.
    public static class IncidentLoader
    {
        public static bool TryParse(string json, out IncidentCollection data, out string error)
        {
            data = null;
            error = null;
            try
            {
                var parsed = JsonUtility.FromJson<IncidentCollection>(json);
                if (parsed == null || parsed.incidents == null || parsed.incidents.Length == 0)
                    throw new InvalidOperationException("Incident list is empty");
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var item in parsed.incidents)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.id) ||
                        string.IsNullOrWhiteSpace(item.title) || string.IsNullOrWhiteSpace(item.description) ||
                        !ids.Add(item.id) || item.roomIndex < 0 || item.roomIndex > 3 ||
                        item.requiredPower < 0 || item.requiredPower > 4 ||
                        item.reward < 0 || item.penalty < 0 || item.penalty > 100 ||
                        (item.requiresVent ? 2 : 0) + (item.requiresTerminal ? 1 : 0) +
                        (item.requiresLight ? 1 : 0) > 4)
                        throw new InvalidOperationException("Invalid incident entry: " + (item == null ? "null" : item.id));
                }
                data = parsed;
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        public static IncidentCollection Load()
        {
            var file = Resources.Load<TextAsset>("incidents");
            if (file == null) throw new InvalidOperationException("Resources/incidents.json not found");
            IncidentCollection result;
            string error;
            if (!TryParse(file.text, out result, out error))
                throw new InvalidOperationException("Invalid incidents.json: " + error);
            return result;
        }
    }
}
