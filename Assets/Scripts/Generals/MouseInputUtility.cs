using UnityEngine;
using UnityEngine.InputSystem;

namespace Generals
{
    public static class MouseInputUtility
    {
        private static readonly Camera camera = Camera.main;

        public static Vector3 GetMousePositionInWorldSpace(float zValue = 0f)
        {
            if (camera == null)
            {
                Debug.LogWarning("MouseInputUtility: No camera available to calculate world position.");
                return Vector3.zero;
            }

            Plane dragPlane = new(camera.transform.forward, new Vector3(0, 0, zValue));
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            Debug.Log($"MousePos:{Mouse.current.position.ReadValue()}");
            if (dragPlane.Raycast(ray, out var distance)) return ray.GetPoint(distance);
            return Vector3.zero;
        }

        public static Vector3 GetRawMouse()
        {
            var mv = Mouse.current.position.ReadValue();
            var vec = new Vector3(mv.x, mv.y, -1f);
            //Debug.Log($"MousePos:{Mouse.current.position.ReadValue()}");
            return vec;
        }
    }
}