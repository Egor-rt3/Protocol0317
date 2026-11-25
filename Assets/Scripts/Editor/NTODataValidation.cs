#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Protocol0317.Editor
{
    public static class NTODataValidation
    {
        [MenuItem("Protocol 03:17/NTO/Validate Demo Data")]
        public static void Validate()
        {
            string root = "NTO_Demonstration/";
            string[] files = {"incidents_before.json", "incidents_after.json", "incidents_invalid.json"};
            bool[] expected = {true, true, false};
            bool allPassed = true;
            for (int i = 0; i < files.Length; i++)
            {
                Protocol0317.IncidentCollection data;
                string error;
                bool valid = Protocol0317.IncidentLoader.TryParse(File.ReadAllText(root + files[i]), out data, out error);
                bool passed = valid == expected[i] && (!valid || data.incidents.Length == (i == 1 ? 13 : 12));
                allPassed &= passed;
                Debug.Log((passed ? "PASS: " : "FAIL: ") + files[i] + (valid ? " events=" + data.incidents.Length : " rejected=" + error));
            }
            Debug.Log(allPassed ? "NTO DATA TESTS: ALL PASS" : "NTO DATA TESTS: FAILED");
        }
    }
}
#endif
