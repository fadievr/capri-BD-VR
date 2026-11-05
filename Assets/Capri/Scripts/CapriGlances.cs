// Watch me: her glances through your Quest's camera, ported from the desktop app's Watch my screen (Capri 0.8.3,
// Brain.cs Glances). She's shown one picture, answers "SAW: what's there SAY: her line", and keeps quiet when she isn't
// sure or has nothing new to say.
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CapriRoom
{
    public static class CapriGlances
    {
        public const string Line = "(You glance through the cameras on their Quest headset at what's in front of them while they do their thing. Only this picture counts: your instructions, old chats and memories aren't evidence of what's there. It's their real room; you, Capri, aren't in the picture. Reply in two parts, with the labels SAW: and SAY: written exactly like that. SAW: only what you can clearly see, in under 20 plain words: what they're doing, holding or looking at, or what's on a screen in front of them. SAY: your reaction in one short line, in character, to one specific thing you can see: tease them, cheer them on, wince, or check in on them, and don't ask what they're doing. A game or show on a screen is something they're playing or watching: react as someone sitting next to them. Don't invent action from a still image, never guess who a person is, and never comment on anyone's body or looks. If you aren't sure what you're seeing, reply with just SKIP. If nothing's worth a line, still give the SAW part and make it SAY: SKIP. For a dark, blurry or blank picture, reply with just SKIP.)";
        private const string Tongue = "(Write the SAY line in {0}. Keep SAW in English, and the labels SAW: and SAY: and the word SKIP exactly as written.)";
        // Her glance lines from the last hour count as already said.
        public const float Memory = 3600;

        private static string SayIn()
        {
            string language = CapriConfig.LanguageEnglish[Mathf.Clamp(CapriConfig.Language, 0, CapriConfig.LanguageEnglish.Length - 1)];
            return language == "English" ? "" : "\n" + string.Format(Tongue, language);
        }

        // Each glance is a fresh look with no chat history, so she's shown her recent lines to stay off.
        public static string Ask(List<string> said)
        {
            if (said.Count == 0) return Line + SayIn();
            string ask = Line + "\n(You already said these about what's in front of them:";
            for (int i = Mathf.Max(0, said.Count - 6); i < said.Count; i++) ask += "\n- " + said[i];
            return ask + "\nDon't bring any of it up again. Pick something new and specific, or make it SAY: SKIP.)" + SayIn();
        }

        // Her line from a glance, or "" to stay quiet. A look she isn't sure of stays unsaid.
        public static string Watched(string raw, out string saw)
        {
            string line = Reply(raw, out saw);
            if (saw != null && Regex.IsMatch(saw, @"\b(maybe|perhaps|probably|possibly|unclear|uncertain|not sure|can't (?:see|tell|read)|cannot (?:see|tell|read))\b", RegexOptions.IgnoreCase)) { saw = null; return ""; }
            return line;
        }

        private static readonly Regex Label = new Regex(@"(?<![\p{L}\p{N}'])(?:(?<label>SAW|SAY)[*_]*\s*(?::|\p{Pd}(?=\s))|(?<label>[Ss]aw|[Ss]ay)[*_]*\s*:)[*_]*");

        private static string Reply(string raw, out string saw)
        {
            saw = null;
            string text = raw ?? "";
            Match sawLabel = null, sayLabel = null;
            foreach (Match label in Label.Matches(text))
            {
                bool saying = label.Groups["label"].Value.ToUpperInvariant() == "SAY";
                if (saying && sayLabel == null) sayLabel = label;
                else if (!saying && sawLabel == null) sawLabel = label;
            }
            if (sawLabel != null || sayLabel != null)
            {
                string seen = sawLabel == null ? "" : Clean(Labelled(text, sawLabel, sayLabel));
                if (seen.Length > 0 && !Regex.IsMatch(seen, @"^[\[(]?SKIP[\])]?[.!?]*$", RegexOptions.IgnoreCase)) saw = seen.Length > 200 ? seen.Substring(0, 200).TrimEnd() + "..." : seen;
                text = sayLabel == null ? "" : Labelled(text, sayLabel, sawLabel);
            }
            string said = Clean(text);
            bool skipped = Regex.IsMatch(said, @"^[\[(]?(SKIP|nothing|none)[\])]?[.!?]*$", RegexOptions.IgnoreCase) || Regex.IsMatch(said, @"^[\[(]?SKIP\b");
            return skipped ? "" : said;
        }

        private static string Labelled(string text, Match label, Match other)
        {
            int start = label.Index + label.Length;
            int end = other != null && other.Index > label.Index ? other.Index : text.Length;
            return text.Substring(start, end - start).Trim(' ', '*', '_', '#', '>', '-', '|', '/', ';', ',', '\n', '\r');
        }

        private static string Clean(string text)
        {
            text = Regex.Replace(text ?? "", @"<think>[\s\S]*?</think>", "");
            text = Regex.Replace(text.Replace("*", ""), @"\s+", " ");
            return text.Trim().Trim('"');
        }

        // True when a line mostly repeats one she already said: the same words, or most of its longer words again.
        public static bool Repeats(string said, IEnumerable<string> before)
        {
            List<string> words = Words(said);
            foreach (string old in before)
            {
                List<string> then = Words(old);
                if (string.Join(" ", then) == string.Join(" ", words)) return true;
                int shared = 0;
                foreach (string word in words) if (then.Contains(word)) shared++;
                if (shared >= 2 && shared * 5 >= words.Count * 3) return true;
            }
            return false;
        }

        private static List<string> Words(string line)
        {
            List<string> words = new List<string>();
            foreach (Match m in Regex.Matches((line ?? "").ToLowerInvariant(), @"[\p{L}\p{Nd}']{4,}")) if (!words.Contains(m.Value)) words.Add(m.Value);
            return words;
        }
    }
}
