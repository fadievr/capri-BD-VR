// The little text box above her head. It turns to face you, types her line out as she says it (like the desktop app),
// and fades out after a few seconds.
using TMPro;
using UnityEngine;

namespace CapriRoom
{
    public class SpeechBubble : MonoBehaviour
    {
        private TextMeshPro text;
        private SpriteRenderer back;
        private float hideAt;
        private float alpha;
        // Typing: letters a second, when it started and how long the line is. Not typing when pace is 0.
        private float pace, typedFrom;
        private int length;
        private static readonly Color Ink = new Color(0.62f, 1f, 0.78f);
        private static readonly Color Panel = new Color(0.05f, 0.02f, 0.08f, 0.85f);

        public static SpeechBubble Make(Transform parent)
        {
            GameObject go = new GameObject("Speech bubble");
            go.transform.SetParent(parent, false);
            SpeechBubble bubble = go.AddComponent<SpeechBubble>();

            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            bubble.back = panel.AddComponent<SpriteRenderer>();
            Texture2D white = Texture2D.whiteTexture;
            bubble.back.sprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), white.width);
            bubble.back.sortingOrder = 10;

            GameObject words = new GameObject("Text");
            words.transform.SetParent(go.transform, false);
            words.transform.localPosition = new Vector3(0, 0, -0.002f);
            bubble.text = words.AddComponent<TextMeshPro>();
            bubble.text.fontSize = 0.35f;
            bubble.text.alignment = TextAlignmentOptions.Center;
            bubble.text.textWrappingMode = TextWrappingModes.Normal;
            bubble.text.rectTransform.sizeDelta = new Vector2(0.55f, 0.3f);
            bubble.text.sortingOrder = 11;
            bubble.SetAlpha(0);
            return bubble;
        }

        // Shows the whole line at once, like her "..." while she listens or thinks.
        public void Say(string line, float seconds) { Show(line, seconds, 0); }

        // Types the line out at "letters" a second, then keeps it up for "hold" seconds more. The panel is sized for the whole
        // line from the start, so it doesn't jump around as the words come in.
        public void Type(string line, float letters, float hold)
        {
            letters = Mathf.Max(1, letters);
            Show(line, line.Length / letters + hold, letters);
        }

        private void Show(string line, float seconds, float letters)
        {
            pace = letters;
            typedFrom = Time.time;
            length = line.Length;
            text.text = line;
            text.maxVisibleCharacters = letters > 0 ? 0 : int.MaxValue;
            text.ForceMeshUpdate();
            Vector2 size = text.GetRenderedValues(false);
            back.transform.localScale = new Vector3(size.x + 0.05f, size.y + 0.035f, 1);
            hideAt = Time.time + Mathf.Max(seconds, 2.5f);
            SetAlpha(1);
        }

        public void Face(Vector3 viewer)
        {
            Vector3 away = transform.position - viewer;
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }

        private void Update()
        {
            if (pace > 0)
            {
                int shown = (int)((Time.time - typedFrom) * pace);
                if (shown >= length) { pace = 0; shown = int.MaxValue; }
                text.maxVisibleCharacters = shown;
            }
            if (alpha > 0 && Time.time > hideAt) SetAlpha(Mathf.Max(0, alpha - Time.deltaTime * 2.5f));
        }

        private void SetAlpha(float a)
        {
            alpha = a;
            Color ink = Ink; ink.a = a;
            Color panel = Panel; panel.a = Panel.a * a;
            text.color = ink;
            back.color = panel;
            back.enabled = text.enabled = a > 0;
        }
    }
}
