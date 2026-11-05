// Her eyes: the Quest 3's passthrough camera, through Meta's PassthroughCameraAccess (MRUK). Only on when you turn on
// "Let her see" (Y → YOU), and only after you allow the headset camera. She sees your real room, not herself or anything
// virtual. Pictures go to her brain only when she glances (Watch me) or when you ask her to look at something.
using System;
using Meta.XR;
using UnityEngine;

namespace CapriRoom
{
    public class CapriEyes : MonoBehaviour
    {
        private const string CameraPermission = "horizonos.permission.HEADSET_CAMERA";
        // Pictures she sends are 640 pixels wide: plenty to tell what's going on, and small enough to send quickly.
        private const int Wide = 640;

        private PassthroughCameraAccess cam;
        private Texture2D read;
        private byte[] lastPrint;
        private bool asked;

        public string Problem { get; private set; }

        // Ready once the camera is on and has sent at least one picture.
        public bool Ready { get { return cam != null && cam.isActiveAndEnabled && cam.IsPlaying; } }

        public static bool Allowed
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return UnityEngine.Android.Permission.HasUserAuthorizedPermission(CameraPermission);
#else
                return false;
#endif
            }
        }

        // Turns her eyes on or off to match her settings, asking for the camera the first time.
        public void Want(bool on)
        {
            if (!on) { if (cam != null) cam.gameObject.SetActive(false); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Allowed)
            {
                if (asked) return;
                asked = true;
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                callbacks.PermissionGranted += delegate { Problem = null; Open(); };
                callbacks.PermissionDenied += delegate { Problem = "I can't see without the headset camera. Allow it in Settings > Privacy > App permissions."; };
                UnityEngine.Android.Permission.RequestUserPermission(CameraPermission, callbacks);
                return;
            }
#endif
            Open();
        }

        private void Open()
        {
            if (cam == null)
            {
                // Its settings are read when it's switched on, so it's made switched off and set up first.
                GameObject holder = new GameObject("Capri eyes");
                holder.transform.SetParent(transform, false);
                holder.SetActive(false);
                cam = holder.AddComponent<PassthroughCameraAccess>();
                cam.CameraPosition = PassthroughCameraAccess.CameraPositionType.Left;
                cam.RequestedResolution = new Vector2Int(1280, 960);
            }
            cam.gameObject.SetActive(true);
        }

        // A JPEG of what's in front of you right now, or null if her eyes aren't ready. With "onlyIfMoved", a picture that
        // looks much like the last one comes back null too, so she doesn't glance at the same thing twice.
        public byte[] Snap(bool onlyIfMoved)
        {
            if (!Ready) return null;
            Texture image = cam.GetTexture();
            if (image == null || image.width <= 0 || image.height <= 0) return null;
            int high = Mathf.Max(1, Mathf.RoundToInt(Wide * (float)image.height / image.width));
            RenderTexture small = RenderTexture.GetTemporary(Wide, high, 0, RenderTextureFormat.ARGB32);
            RenderTexture was = RenderTexture.active;
            try
            {
                Graphics.Blit(image, small);
                RenderTexture.active = small;
                if (read == null || read.width != Wide || read.height != high) read = new Texture2D(Wide, high, TextureFormat.RGB24, false);
                read.ReadPixels(new Rect(0, 0, Wide, high), 0, 0);
                read.Apply();
            }
            finally
            {
                RenderTexture.active = was;
                RenderTexture.ReleaseTemporary(small);
            }
            byte[] print = Fingerprint(read);
            bool moved = Moved(print, lastPrint);
            lastPrint = print;
            if (onlyIfMoved && !moved) return null;
            return read.EncodeToJPG(75);
        }

        // A 64 x 36 RGB thumbnail, like the desktop app's, to tell whether the view changed.
        private static byte[] Fingerprint(Texture2D image)
        {
            Color32[] pixels = image.GetPixels32();
            byte[] print = new byte[64 * 36 * 3];
            for (int y = 0; y < 36; y++)
                for (int x = 0; x < 64; x++)
                {
                    Color32 c = pixels[(y * image.height / 36) * image.width + x * image.width / 64];
                    int at = (y * 64 + x) * 3;
                    print[at] = c.r; print[at + 1] = c.g; print[at + 2] = c.b;
                }
            return print;
        }

        // From the desktop app's Glances.Moved: changed enough to be worth a look.
        private static bool Moved(byte[] print, byte[] before)
        {
            if (before == null || before.Length != print.Length) return true;
            long difference = 0;
            int moved = 0;
            for (int i = 0; i + 2 < print.Length; i += 3)
            {
                int change = Math.Max(Math.Abs(print[i] - before[i]), Math.Max(Math.Abs(print[i + 1] - before[i + 1]), Math.Abs(print[i + 2] - before[i + 2])));
                difference += change;
                if (change > 24) moved++;
            }
            return difference > 64 * 36 * 6 || moved >= 24;
        }
    }
}
