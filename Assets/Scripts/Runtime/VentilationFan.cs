using UnityEngine;

namespace Protocol0317
{
    // Rotates the fan independently of the terminal, cutscene and camera.
    public class VentilationFan : MonoBehaviour
    {
        public float rotationsPerMinute = 100f;
        private void Update()
        {
            transform.Rotate(0f, 0f, -rotationsPerMinute * 6f * Time.deltaTime, Space.Self);
        }
    }
}
