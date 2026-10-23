using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BoomYard
{
    /// <summary>
    /// Hebrew and English from day one (spec §15). Hebrew strings are stored in logical order; uGUI's legacy Text
    /// has no bidi, so <see cref="Rtl.Visual"/> reorders them for display.
    /// </summary>
    public static class Loc
    {
        public static bool He { get; private set; }

        public static void Init(string lang)
        {
            if (string.IsNullOrEmpty(lang)) He = Application.systemLanguage == SystemLanguage.Hebrew;
            else He = lang == "he";
        }

        /// <summary>The text for a key in the current language, ready to display.</summary>
        public static string T(string key)
        {
            string[] s;
            if (!Strings.TryGetValue(key, out s)) return key;
            return He ? Rtl.Visual(s[0]) : s[1];
        }

        /// <summary>Formats with arguments (in logical order), then shapes.</summary>
        public static string F(string key, params object[] args)
        {
            string[] s;
            if (!Strings.TryGetValue(key, out s)) return key;
            var text = string.Format(He ? s[0] : s[1], args);
            return He ? Rtl.Visual(text) : text;
        }

        /// <summary>Shapes any runtime text (object names) for the current language.</summary>
        public static string Show(string text) { return He ? Rtl.Visual(text) : text; }

        public static string Money(long v) { return "₪" + v.ToString("#,0"); }

        static readonly Dictionary<string, string[]> Strings = new Dictionary<string, string[]>
        {
            { "title.tag", new[] { "מדליקים. סופרים. בורחים.", "Light it. Count. Run." } },
            { "title.play", new[] { "שחק", "PLAY" } },
            { "title.tap", new[] { "הקש להמשך", "Tap to continue" } },
            { "disclaimer.title", new[] { "לפני שמתחילים", "Before you start" } },
            { "disclaimer.l1", new[] { "זה משחק. אף אדם או חיה לא נפגעים, רק רכוש.", "This is a game. No person or animal gets hurt, only property." } },
            { "disclaimer.l2", new[] { "חומרי נפץ אמיתיים מסוכנים ולא חוקיים.", "Real explosives are dangerous and illegal." } },
            { "disclaimer.l3", new[] { "אל תנסו את זה בבית. 16+", "Never try this at home. 16+" } },
            { "disclaimer.ok", new[] { "הבנתי", "GOT IT" } },
            { "hud.damage", new[] { "נזק", "DAMAGE" } },
            { "hud.chain", new[] { "שרשרת ×{0}", "CHAIN ×{0}" } },
            { "hud.wind", new[] { "רוח {0} m/s", "Wind {0} m/s" } },
            { "hud.lighter", new[] { "מצית (החזק)", "LIGHTER (HOLD)" } },
            { "hud.throw", new[] { "זרוק / הנח", "THROW / PLACE" } },
            { "hud.move", new[] { "תנועה", "MOVE" } },
            { "hud.caught", new[] { "נתפס!", "CAUGHT!" } },
            { "hud.hold", new[] { "החזק את הפתיל בלהבה", "Hold the fuse in the flame" } },
            { "hud.settle", new[] { "נגמר הזמן", "TIME" } },
            { "toast.wind", new[] { "הרוח כיבתה את המצית. הסתובב עם הגב לרוח", "The wind blew it out. Turn your back to the wind" } },
            { "toast.run", new[] { "ריצה מכבה את המצית", "Running blows the lighter out" } },
            { "toast.match", new[] { "הגפרור נשבר", "The match snapped" } },
            { "toast.light", new[] { "קודם מדליקים: החזק את המצית", "Light it first: hold the lighter" } },
            { "toast.reach", new[] { "רחוק מדי. התקרב כדי להניח", "Too far. Get closer to place it" } },
            { "toast.contained", new[] { "Containment ×1.8", "Containment ×1.8" } },
            { "bonus.CloseCall", new[] { "Close Call +₪5,000", "Close Call +₪5,000" } },
            { "bonus.Multi", new[] { "Multi +₪8,000", "Multi +₪8,000" } },
            { "bonus.LastSecond", new[] { "Last Second +₪3,000", "Last Second +₪3,000" } },
            { "bonus.HandBurn", new[] { "התפוצץ ביד −₪2,000", "Hand Burn −₪2,000" } },
            { "results.record", new[] { "שיא אישי (קודם {0})", "Personal best (was {0})" } },
            { "results.best", new[] { "השיא: {0}", "Best: {0}" } },
            { "results.base", new[] { "נזק בסיס", "Base damage" } },
            { "results.chain", new[] { "בונוס שרשרת (הכי ארוכה: {0})", "Chain bonus (longest: {0})" } },
            { "results.placement", new[] { "Containment ומיקום", "Containment & placement" } },
            { "results.bonuses", new[] { "בונוסים", "Bonuses" } },
            { "results.sweep", new[] { "Clean Sweep ×1.5", "Clean Sweep ×1.5" } },
            { "results.destroyed", new[] { "הרס הזירה {0}% · {1}/{2}", "Arena destroyed {0}% · {1}/{2}" } },
            { "results.coins", new[] { "לכיס: {0}", "Earned: {0}" } },
            { "results.again", new[] { "שוב", "AGAIN" } },
            { "results.menu", new[] { "תפריט", "MENU" } },
            { "tip.contain", new[] { "טיפ: הנח M-80 מתחת לטרנזיט במקום לזרוק. Containment ×1.8", "Tip: place an M-80 under the van instead of throwing it. Containment ×1.8" } },
            { "tip.chain", new[] { "טיפ: פוצץ את מה שמתפוצץ בעצמו. כל חוליה בשרשרת מוסיפה ×0.5", "Tip: hit what explodes by itself. Every chain link adds ×0.5" } },
            { "tip.again", new[] { "טיפ: שחרר בירוק לפיצוץ באוויר, Airburst ×1.5", "Tip: release in the green for an airburst, ×1.5" } },
            { "pause.title", new[] { "הפסקה", "PAUSED" } },
            { "pause.resume", new[] { "המשך", "RESUME" } },
            { "pause.restart", new[] { "התחל מחדש", "RESTART" } },
            { "pause.quit", new[] { "לתפריט", "MENU" } },
            { "lang", new[] { "English", "עברית" } },
        };
    }

    /// <summary>
    /// Minimal bidi for single-direction Hebrew UI lines: Hebrew and neutrals run right-to-left, while runs of Latin
    /// letters and numbers (with the ₪, %, ×, separators inside them) keep their left-to-right order.
    /// </summary>
    public static class Rtl
    {
        public static string Visual(string logical)
        {
            if (string.IsNullOrEmpty(logical)) return logical;
            if (logical.IndexOf('\n') >= 0)
            {
                var lines = logical.Split('\n');
                for (int i = 0; i < lines.Length; i++) lines[i] = Line(lines[i]);
                return string.Join("\n", lines);
            }
            return Line(logical);
        }

        static bool IsHebrew(char c) { return c >= '\u0590' && c <= '\u05FF'; }
        static bool IsLetter(char c) { return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'); }
        static bool IsLtr(char c) { return IsLetter(c) || char.IsDigit(c); }
        static bool IsWeak(char c) { return "₪%+-−×.,:/'\"#".IndexOf(c) >= 0; }

        static string Line(string s)
        {
            bool anyHebrew = false;
            foreach (var c in s) if (IsHebrew(c)) { anyHebrew = true; break; }
            if (!anyHebrew) return s;

            // Runs that keep their order: letters and digits, with weak characters glued to them ("M-80", "₪18,000",
            // "62%", "×1.8") and spaces only between two Latin words ("Black Cat"). Everything else flows right to left.
            var runs = new List<string>();
            int i = 0;
            while (i < s.Length)
            {
                if (!IsLtr(s[i]) && !(IsWeak(s[i]) && i + 1 < s.Length && IsLtr(s[i + 1])))
                {
                    runs.Add(Mirror(s[i]).ToString());
                    i++;
                    continue;
                }
                int j = i + 1;
                while (j < s.Length)
                {
                    char c = s[j];
                    if (IsLtr(c)) { j++; continue; }
                    if (IsWeak(c) && (j + 1 < s.Length && IsLtr(s[j + 1]) || char.IsDigit(s[j - 1]))) { j++; continue; }
                    if (c == ' ' && IsLetter(s[j - 1]))
                    {
                        int k = j;
                        while (k < s.Length && s[k] == ' ') k++;
                        if (k < s.Length && IsLetter(s[k])) { j = k; continue; }
                    }
                    break;
                }
                runs.Add(s.Substring(i, j - i));
                i = j;
            }
            var sb = new StringBuilder(s.Length);
            for (int k = runs.Count - 1; k >= 0; k--) sb.Append(runs[k]);
            return sb.ToString();
        }

        static char Mirror(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '<': return '>';
                case '>': return '<';
                default: return c;
            }
        }
    }
}
