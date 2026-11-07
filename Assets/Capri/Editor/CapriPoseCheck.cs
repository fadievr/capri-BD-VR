// Capri > Debug > Check poses: loads every animation the way the headset does and lists what fails, in the Console.
using UnityEditor;
using UnityEngine;

namespace CapriRoom.EditorTools
{
    public static class CapriPoseCheck
    {
        [MenuItem("Capri/Debug/Check poses")]
        private static void Check()
        {
            try
            {
                foreach (string name in new[] { "idle", "perch", "lay", "perch_sit2", "mouth_lay", "mouth_perch_sit2" })
                {
                    Texture2D sheet = Resources.Load<Texture2D>("CapriSprites/" + name);
                    Debug.Log("[Capri] pose " + name + ": loaded=" + CapriClips.Has(name) + " sheet=" + (sheet == null ? "MISSING" : sheet.width + "x" + sheet.height));
                }
                Debug.Log("[Capri] pose problems: " + (CapriClips.Problems.Count == 0 ? "none" : string.Join(" | ", CapriClips.Problems)));
            }
            catch (System.Exception problem) { Debug.LogException(problem); }
        }
    }
}
