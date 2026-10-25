using System;
using System.Collections.Generic;
using BoomYard.Core;
using BoomYard.Game;
using UnityEngine;
using UnityEngine.UI;

namespace BoomYard.UI
{
    /// <summary>The menu screens: home, the level map, shop, missions, settings, pause and results.</summary>
    public sealed partial class GameUI
    {
        public const string PrivacyUrl = "https://boomyard.vercel.app/privacy.html";
        public const string TermsUrl = "https://boomyard.vercel.app/terms.html";
        static readonly Color Dark = new Color(0.07f, 0.04f, 0.02f);
        static readonly Color Card = new Color(0.1f, 0.09f, 0.1f, 0.94f);

        // ---------- shared pieces ----------

        /// <summary>Top bar: back button and title on the left, coins on the right.</summary>
        void TopBar(RectTransform s, string title, Action back)
        {
            if (back != null)
            {
                var b = UIKit.IconButton(s, "Back", UIKit.Back, new Color(0.12f, 0.11f, 0.12f, 0.95f), UIKit.Fg, () => { Sfx.I.Tap(); back(); }, 76f);
                UIKit.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(36f, -22f), new Vector2(76f, 76f));
            }
            if (!string.IsNullOrEmpty(title))
            {
                var t = UIKit.Label(s, "Title", title, 50, UIKit.Fg, TextAnchor.MiddleLeft, true);
                UIKit.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(back != null ? 132f : 40f, -22f), new Vector2(800f, 76f));
            }
            Wallet(s);
        }

        void Wallet(RectTransform s)
        {
            var save = SaveService.Data;
            Pill(s, new Vector2(-40f, -26f), UIKit.Coin, Color.white, Loc.Coins(save.shekels));
        }

        void Pill(RectTransform s, Vector2 pos, Sprite icon, Color iconColor, string text)
        {
            var p = UIKit.Image(s, "Pill", new Color(0.05f, 0.045f, 0.05f, 0.85f), UIKit.Pill);
            UIKit.Place(p.rectTransform, new Vector2(1f, 1f), pos, new Vector2(230f, 64f));
            var ic = UIKit.Image(p.transform, "Icon", iconColor, icon);
            UIKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(40f, 40f));
            ic.rectTransform.pivot = new Vector2(0f, 0.5f);
            var t = UIKit.Label(p.transform, "Text", text, 30, UIKit.Fg, TextAnchor.MiddleRight, true);
            UIKit.Place(t.rectTransform, new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(160f, 50f));
            t.rectTransform.pivot = new Vector2(1f, 0.5f);
        }

        Button Btn(Transform parent, string label, bool primary, Action onClick, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize = 34)
        {
            var b = UIKit.Button(parent, label, label, primary ? UIKit.Ember : new Color(0.14f, 0.13f, 0.15f, 0.95f), primary ? Dark : UIKit.Fg, () => { Sfx.I.Tap(); onClick(); }, fontSize);
            var r = (RectTransform)b.transform;
            UIKit.Place(r, anchor, pos, size);
            r.pivot = new Vector2(0.5f, 0.5f);
            return b;
        }

        Text Text(Transform parent, string text, int size, Color c, TextAnchor a, Vector2 anchor, Vector2 pos, Vector2 box, bool display = false)
        {
            var t = UIKit.Label(parent, "Text", text, size, c, a, display);
            UIKit.Place(t.rectTransform, anchor, pos, box);
            t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        void Stars(Transform parent, int stars, Vector2 anchor, Vector2 pos, float size, float gap)
        {
            for (int i = 0; i < 3; i++)
            {
                var st = UIKit.Image(parent, "Star", i < stars ? UIKit.Flame : new Color(1f, 1f, 1f, 0.15f), UIKit.Star);
                UIKit.Place(st.rectTransform, anchor, pos + new Vector2((i - 1) * gap, 0f), new Vector2(size, size));
                st.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        // ---------- home ----------

        public void ShowHome()
        {
            ShowLayer(false);
            var s = NewScreen("Home", false);
            // Darken the left side so the menu reads over the live street.
            var shade = UIKit.Image(s, "Shade", new Color(0f, 0f, 0f, 0.62f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = new Vector2(0.45f, 1f);
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

            var logo = UIKit.Label(s, "Logo", "BOOMYARD", 128, UIKit.Fg, TextAnchor.MiddleLeft, true);
            UIKit.Place(logo.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -60f), new Vector2(1000f, 140f));
            logo.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.7f);
            var tag = UIKit.Label(s, "Tag", Loc.T("title.tag"), 32, UIKit.Flame, TextAnchor.MiddleLeft);
            UIKit.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -200f), new Vector2(800f, 44f));

            var save = SaveService.Data;
            int next = Mathf.Min(save.unlocked, Levels.All.Length);
            var play = Btn(s, "PLAY", true, () => game.GoMap(), new Vector2(0f, 0.5f), new Vector2(260f, -30f), new Vector2(380f, 110f), 52);
            var sub = UIKit.Label(play.transform, "Sub", "LEVEL " + next + " · " + Levels.Get(next).Name.ToUpperInvariant(), 20, Dark, TextAnchor.LowerCenter);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(380f, 28f));
            sub.rectTransform.pivot = new Vector2(0.5f, 0f);
            ((RectTransform)play.transform.Find("Label")).offsetMin = new Vector2(0f, 18f);

            Btn(s, "SHOP", false, () => ShowShop(), new Vector2(0f, 0.5f), new Vector2(170f, -165f), new Vector2(200f, 86f), 30);
            var missions = Btn(s, "MISSIONS", false, () => ShowMissions(), new Vector2(0f, 0.5f), new Vector2(380f, -165f), new Vector2(200f, 86f), 28);
            int ready = 0;
            foreach (var m in save.TodaysMissions()) if (m.Done && !m.claimed) ready++;
            if (ready > 0 || game.DailyAvailable() >= 0) Badge(missions.transform, ready > 0 ? ready.ToString() : "!");
            var gear = UIKit.IconButton(s, "Settings", UIKit.Gear, new Color(0.12f, 0.11f, 0.12f, 0.95f), UIKit.Fg, () => { Sfx.I.Tap(); ShowSettings(false); }, 76f);
            UIKit.Place((RectTransform)gear.transform, new Vector2(0f, 0f), new Vector2(70f, 40f), new Vector2(76f, 76f));

            Wallet(s);
            if (game.DailyAvailable() >= 0)
            {
                var gift = UIKit.IconButton(s, "Daily", UIKit.Gift, UIKit.Ember, Dark, () => { Sfx.I.Tap(); ShowDaily(); }, 76f);
                UIKit.Place((RectTransform)gift.transform, new Vector2(1f, 1f), new Vector2(-560f, -20f), new Vector2(76f, 76f));
            }
            var stars = UIKit.Label(s, "Stars", "★ " + save.TotalStars + " / " + Levels.All.Length * 3, 1, UIKit.Muted, TextAnchor.MiddleLeft);
            stars.gameObject.SetActive(false);

            if (!save.disclaimerSeen) ShowDisclaimer();
        }

        void Badge(Transform parent, string text)
        {
            var b = UIKit.Image(parent, "Badge", UIKit.Danger, UIKit.Circle);
            UIKit.Place(b.rectTransform, new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(34f, 34f));
            b.rectTransform.pivot = new Vector2(1f, 1f);
            var t = UIKit.Label(b.transform, "N", text, 20, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(t.rectTransform);
        }

        void ShowDisclaimer()
        {
            var p = Modal("Disclaimer", new Vector2(1060f, 470f));
            Text(p, Loc.T("disclaimer.title"), 44, UIKit.Flame, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1000f, 60f), true);
            for (int i = 0; i < 3; i++)
                Text(p, Loc.T("disclaimer.l" + (i + 1)), 30, i == 2 ? UIKit.Fg : UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -150f - i * 58f), new Vector2(1000f, 44f));
            Btn(p, Loc.T("disclaimer.ok"), true, () =>
            {
                SaveService.Data.disclaimerSeen = true;
                SaveService.Save();
                CloseModal();
            }, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(260f, 76f));
        }

        // ---------- level map ----------

        public void ShowMap()
        {
            ShowLayer(false);
            var s = NewScreen("Map");
            TopBar(s, "LEVELS", () => game.GoHome());
            var save = SaveService.Data;
            var gnomes = UIKit.Label(s, "Gnomes", "GOLDEN GNOMES  " + save.goldGnomes.Count + " / " + (Levels.All.Length - 1), 24, new Color(1f, 0.78f, 0.3f), TextAnchor.MiddleCenter, true);
            UIKit.Place(gnomes.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(600f, 34f));
            ScrollRect sr;
            var content = UIKit.Scroll(s, "Path", false, out sr);
            var view = (RectTransform)sr.transform;
            view.anchorMin = new Vector2(0f, 0f); view.anchorMax = new Vector2(1f, 1f);
            view.offsetMin = new Vector2(0f, 40f); view.offsetMax = new Vector2(0f, -120f);
            const float step = 230f, gap = 200f;
            // Each street starts with a gap and its name over the path.
            System.Func<int, float> X = i => 160f + i * step + (Streets.ForLevel(i + 1).FirstLevel > 1 ? gap * IndexOf(Streets.ForLevel(i + 1)) : 0f);
            content.sizeDelta = new Vector2(X(Levels.All.Length - 1) + 400f, 0f);

            for (int i = 0; i < Levels.All.Length; i++)
            {
                var l = Levels.All[i];
                float x = X(i), y = Mathf.Sin(i * 1.1f) * 120f;
                var street = l.Street;
                if (l.Number == street.FirstLevel)
                {
                    bool open = l.Number <= save.unlocked;
                    var title = UIKit.Label(content, "Street", street.Name.ToUpperInvariant(), 34, open ? UIKit.Flame : UIKit.Muted, TextAnchor.MiddleLeft, true);
                    UIKit.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(x - 70f, 250f), new Vector2(700f, 44f));
                    title.rectTransform.pivot = new Vector2(0f, 0.5f);
                    var blurb = UIKit.Label(content, "Blurb", street.Blurb, 20, UIKit.Muted, TextAnchor.MiddleLeft);
                    UIKit.Place(blurb.rectTransform, new Vector2(0f, 0.5f), new Vector2(x - 70f, 215f), new Vector2(700f, 28f));
                    blurb.rectTransform.pivot = new Vector2(0f, 0.5f);
                }
                if (i > 0)
                {
                    // Dotted path to the previous node.
                    float px = X(i - 1), py = Mathf.Sin((i - 1) * 1.1f) * 120f;
                    for (int d = 1; d < 6; d++)
                    {
                        float k = d / 6f;
                        var dot = UIKit.Image(content, "Dot", l.Number <= save.unlocked ? new Color(1f, 0.48f, 0.1f, 0.8f) : new Color(1f, 1f, 1f, 0.15f), UIKit.Circle);
                        UIKit.Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(Mathf.Lerp(px, x, k), Mathf.Lerp(py, y, k)), new Vector2(12f, 12f));
                        dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    }
                }
                bool locked = l.Number > save.unlocked;
                bool current = l.Number == save.unlocked;
                var rec = save.Level(l.Number);
                var node = UIKit.Image(content, "Level" + l.Number, locked ? new Color(0.14f, 0.13f, 0.15f, 0.95f) : current ? UIKit.Ember : new Color(0.2f, 0.18f, 0.19f, 0.98f), UIKit.Circle);
                float nodeSize = (current ? 140f : 120f) + (l.Boss ? 24f : 0f);
                UIKit.Place(node.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, y), new Vector2(nodeSize, nodeSize));
                node.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                node.raycastTarget = true;
                var ring = UIKit.Image(node.transform, "Ring", l.Boss ? UIKit.Danger : locked ? UIKit.Line : UIKit.Flame, UIKit.Ring);
                UIKit.Stretch(ring.rectTransform);
                if (l.Boss)
                {
                    var boss = UIKit.Label(content, "Boss", "BOSS", 20, UIKit.Danger, TextAnchor.MiddleCenter, true);
                    UIKit.Place(boss.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, y + 122f), new Vector2(120f, 26f));
                    boss.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
                if (locked)
                {
                    var lk = UIKit.Image(node.transform, "Lock", UIKit.Muted, UIKit.Lock);
                    UIKit.Place(lk.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50f, 50f));
                    lk.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                }
                else
                {
                    var n = UIKit.Label(node.transform, "N", l.Number.ToString(), 54, current ? Dark : UIKit.Fg, TextAnchor.MiddleCenter, true);
                    UIKit.Stretch(n.rectTransform);
                }
                Stars(content, rec.stars, new Vector2(0f, 0.5f), new Vector2(x, y - 92f), 30f, 32f);
                var name = UIKit.Label(content, "Name", l.Name.ToUpperInvariant(), 20, locked ? UIKit.Muted : UIKit.Fg, TextAnchor.MiddleCenter, true);
                UIKit.Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(x, y + 92f), new Vector2(220f, 28f));
                name.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (!locked)
                {
                    int num = l.Number;
                    var b = node.gameObject.AddComponent<Button>();
                    b.onClick.AddListener(() => { Sfx.I.Tap(); ShowLevelInfo(num); });
                }
            }
            // Scroll so the current level is in view.
            float target = X(Mathf.Clamp(save.unlocked - 1, 0, Levels.All.Length - 1));
            sr.horizontalNormalizedPosition = Mathf.Clamp01((target - 600f) / Mathf.Max(1f, content.sizeDelta.x - 1600f));
        }

        void ShowLevelInfo(int number)
        {
            var l = Levels.Get(number);
            var rec = SaveService.Data.Level(number);
            var p = Modal("LevelInfo", new Vector2(980f, 600f));
            Text(p, "LEVEL " + l.Number + "  ·  " + l.Street.Name.ToUpperInvariant() + (l.Boss ? "  ·  BOSS" : ""), 26, l.Boss ? UIKit.Danger : UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(900f, 34f), true);
            Text(p, l.Name.ToUpperInvariant(), 58, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(900f, 70f), true);
            Stars(p, rec.stars, new Vector2(0.5f, 1f), new Vector2(0f, -164f), 44f, 52f);
            int targets = 0;
            var list = new List<PropSpot>();
            for (int i = 0; i < l.Props.Length; i++) if (l.IsTarget(i)) { targets++; list.Add(l.Props[i]); }
            string rule = l.Rule == LevelRule.None ? "" : l.Rule == LevelRule.Spare ? "   ·   KEEP THE " + Catalog.Object(l.Props[l.Spare].Id).NameEn.ToUpperInvariant() : "   ·   " + RuleName(l.Rule);
            Text(p, "Destroy all " + targets + " targets in " + Loc.Time(l.Time) + rule, 32, UIKit.Flame, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -226f), new Vector2(940f, 44f));
            Text(p, "TARGETS:  " + Summary(list.ToArray()), 24, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(900f, 70f));
            if (l.Number >= 2) Text(p, SaveService.Data.goldGnomes.Contains(l.Number) ? "GOLDEN GNOME FOUND" : "A GOLDEN GNOME IS HIDDEN HERE", 22, new Color(1f, 0.78f, 0.3f), TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -420f), new Vector2(900f, 30f), true);
            Text(p, "YOU GET:  " + KitSummary(l.Kit) + Owned(), 24, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -368f), new Vector2(900f, 70f));
            Btn(p, "PLAY", true, () => game.StartLevel(number), new Vector2(0.5f, 0f), new Vector2(110f, 80f), new Vector2(320f, 96f), 44);
            Btn(p, "CLOSE", false, CloseModal, new Vector2(0.5f, 0f), new Vector2(-200f, 80f), new Vector2(220f, 86f), 28);
        }

        static string RuleName(LevelRule r)
        {
            switch (r)
            {
                case LevelRule.Rain: return "RAIN";
                case LevelRule.Windy: return "STRONG WIND";
                case LevelRule.Matches: return "MATCHES ONLY";
                default: return "";
            }
        }

        static int IndexOf(StreetDef s) { return System.Array.IndexOf(Streets.All, s); }

        static string Summary(PropSpot[] props)
        {
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var p in props) { if (!counts.ContainsKey(p.Id)) { counts[p.Id] = 0; order.Add(p.Id); } counts[p.Id]++; }
            var parts = new List<string>();
            foreach (var id in order) parts.Add((counts[id] > 1 ? counts[id] + "× " : "") + Catalog.Object(id).NameEn);
            return string.Join(", ", parts.ToArray());
        }

        static string KitSummary(PocketSlot[] kit)
        {
            var parts = new List<string>();
            foreach (var k in kit) parts.Add(k.count + "× " + Catalog.Explosive(k.id).NameEn);
            return string.Join(", ", parts.ToArray());
        }

        static string Owned()
        {
            int n = 0;
            foreach (var s in SaveService.Data.inventory) n += s.count;
            return n > 0 ? "  (+ " + n + " of your own)" : "";
        }

        // ---------- shop ----------

        string shopTab = "explosive";

        public void ShowShop()
        {
            ShowLayer(false);
            var s = NewScreen("Shop");
            TopBar(s, "SHOP", () => game.GoHome());
            string[] tabs = { "explosive", "lighter", "upgrade", "style" };
            string[] names = { "EXPLOSIVES", "LIGHTERS", "UPGRADES", "STYLE" };
            for (int i = 0; i < tabs.Length; i++)
            {
                string tab = tabs[i];
                var b = Btn(s, names[i], tab == shopTab, () => { shopTab = tab; ShowShop(); }, new Vector2(0f, 1f), new Vector2(180f + i * 250f, -150f), new Vector2(230f, 62f), 24);
            }
            ScrollRect sr;
            var content = UIKit.Scroll(s, "Items", true, out sr);
            var view = (RectTransform)sr.transform;
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(40f, 30f); view.offsetMax = new Vector2(-40f, -200f);
            var save = SaveService.Data;
            int idx = 0;
            const float cw = 500f, ch = 230f;
            foreach (var item in Shop.Items)
            {
                bool style = item.Kind == "glove" || item.Kind == "lighterskin";
                if (shopTab == "style" ? !style : item.Kind != shopTab) continue;
                int col = idx % 3, row = idx / 3;
                idx++;
                var card = UIKit.Image(content, item.Id, Card, UIKit.Rounded);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0f, 1f);
                card.rectTransform.pivot = new Vector2(0f, 1f);
                card.rectTransform.anchoredPosition = new Vector2(col * (cw + 24f), -row * (ch + 24f));
                card.rectTransform.sizeDelta = new Vector2(cw, ch);
                bool locked = item.UnlockLevel > save.unlocked;
                bool owned = style ? item.Coins == 0 || save.styles.Contains(item.Id) : item.Kind != "explosive" && (save.lighters.Contains(item.Grants) || save.Has(item.Grants));
                bool equipped = item.Kind == "lighter" && save.lighter == item.Grants
                    || item.Kind == "glove" && save.glove == item.Grants || item.Kind == "lighterskin" && save.lighterSkin == item.Grants;
                var chip = UIKit.Image(card.transform, "Chip", style ? StyleColor(item) : item.Kind == "explosive" ? ItemModel.Chip(item.Grants) : item.Kind == "lighter" ? UIKit.Flame : UIKit.SteelBlue, style ? UIKit.Circle : item.Kind == "explosive" ? UIKit.Rounded : item.Kind == "lighter" ? UIKit.FlameIcon : UIKit.Star);
                UIKit.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -24f), item.Kind == "explosive" ? new Vector2(38f, 70f) : new Vector2(70f, 70f));
                var title = UIKit.Label(card.transform, "Name", item.Name, 28, UIKit.Fg, TextAnchor.UpperLeft, true);
                UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(118f, -24f), new Vector2(360f, 40f));
                var desc = UIKit.Label(card.transform, "Desc", Describe(item), 19, UIKit.Muted, TextAnchor.UpperLeft);
                UIKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(118f, -66f), new Vector2(360f, 80f));
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                if (item.Kind == "explosive")
                {
                    var have = UIKit.Label(card.transform, "Have", "You have " + save.Count(item.Grants), 19, UIKit.Muted, TextAnchor.LowerLeft);
                    UIKit.Place(have.rectTransform, new Vector2(0f, 0f), new Vector2(24f, 26f), new Vector2(240f, 30f));
                }
                string label = locked ? "LEVEL " + item.UnlockLevel : equipped ? "EQUIPPED" : owned ? (item.Kind == "upgrade" ? "OWNED" : "EQUIP") : Loc.Coins(item.Coins);
                bool canBuy = !locked && !equipped && !(owned && item.Kind == "upgrade");
                var price = UIKit.Button(card.transform, "Buy", label, canBuy ? UIKit.Ember : new Color(0.2f, 0.19f, 0.2f), canBuy ? Dark : UIKit.Muted, () =>
                {
                    if (!canBuy) return;
                    if (game.Buy(item)) ShowShop();
                    else { Sfx.I.Fail(); Toast("Not enough coins"); }
                }, 26);
                var pr = (RectTransform)price.transform;
                UIKit.Place(pr, new Vector2(1f, 0f), new Vector2(-20f, 18f), new Vector2(190f, 64f));
                if (canBuy && !owned)
                {
                    var ic = UIKit.Image(price.transform, "Cur", Color.white, UIKit.Coin);
                    UIKit.Place(ic.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(32f, 32f));
                    ic.rectTransform.pivot = new Vector2(0f, 0.5f);
                    ((RectTransform)price.transform.Find("Label")).offsetMin = new Vector2(36f, 0f);
                }
            }
            content.sizeDelta = new Vector2(0f, ((idx + 2) / 3) * (ch + 24f));
        }

        static Color StyleColor(ShopItem item)
        {
            Look l;
            if (item.Kind == "glove") return Styles.Glove(item.Grants, out l) ? new Color(l.R, l.G, l.B) : Mats.Skin;
            l = Styles.Lighter(item.Grants);
            return new Color(l.R, l.G, l.B);
        }

        static string Describe(ShopItem item)
        {
            if (item.Kind == "glove") return item.Grants == "" ? "Just your hands." : "Gloves for both hands. Looks only.";
            if (item.Kind == "lighterskin") return "A new finish for your lighter. Looks only.";
            if (item.Kind == "explosive")
            {
                var e = Catalog.Explosive(item.Grants);
                return "Fuse " + Fuse.TotalBurn(e).ToString("0.#") + " s · Power " + (e.Damage * e.Units).ToString("#,0") + " · Radius " + e.Radius.ToString("0.#") + " m";
            }
            if (item.Kind == "lighter")
            {
                var l = Catalog.Lighter(item.Grants);
                return (l.WindBlowout > 0f ? "Can blow out in the wind. " : "Windproof. ") + (l.CatchTime <= 0f ? "Lights instantly." : "Lights a fuse in " + l.CatchTime.ToString("0.0") + " s.");
            }
            switch (item.Grants)
            {
                case "vest": return "Carry five kinds of explosives into every level.";
                case "legs": return "Sprint faster and keep the lighter lit while you jog.";
                case "watch": return "Every level's clock starts 10 seconds higher.";
                default: return "";
            }
        }

        // ---------- missions and the daily reward ----------

        public void ShowMissions()
        {
            ShowLayer(false);
            var s = NewScreen("Missions");
            TopBar(s, "MISSIONS", () => game.GoHome());
            var save = SaveService.Data;

            // The 7-day calendar.
            int next = game.DailyAvailable();
            for (int d = 0; d < 7; d++)
            {
                bool done = next < 0 ? d <= save.dailyStreak : d < next && save.dailyClaimDate != 0;
                bool today = d == next;
                var c = UIKit.Image(s, "Day" + d, today ? UIKit.Ember : done ? new Color(0.2f, 0.3f, 0.2f, 0.95f) : Card, UIKit.Rounded);
                UIKit.Place(c.rectTransform, new Vector2(0.5f, 1f), new Vector2((d - 3) * 168f, -140f), new Vector2(150f, 150f));
                c.rectTransform.pivot = new Vector2(0.5f, 1f);
                var t = UIKit.Label(c.transform, "Day", "DAY " + (d + 1), 20, today ? Dark : UIKit.Muted, TextAnchor.UpperCenter, true);
                UIKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(150f, 26f));
                t.rectTransform.pivot = new Vector2(0.5f, 1f);
                var ic = UIKit.Image(c.transform, "Icon", Color.white, UIKit.Coin);
                UIKit.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(54f, 54f));
                ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                var amt = UIKit.Label(c.transform, "Amount", DailyReward.Coins[d].ToString(), 26, today ? Dark : UIKit.Fg, TextAnchor.LowerCenter, true);
                UIKit.Place(amt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(150f, 32f));
                amt.rectTransform.pivot = new Vector2(0.5f, 0f);
                if (done)
                {
                    var ck = UIKit.Image(c.transform, "Done", UIKit.Ok, UIKit.Check);
                    UIKit.Place(ck.rectTransform, new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(34f, 34f));
                    ck.rectTransform.pivot = new Vector2(1f, 1f);
                }
            }
            if (next >= 0) Btn(s, "CLAIM TODAY", true, () => { game.ClaimDaily(); ShowMissions(); }, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(320f, 70f), 30);

            Text(s, "TODAY'S MISSIONS", 26, UIKit.Muted, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(-280f, next >= 0 ? -410f : -340f), new Vector2(700f, 34f), true);
            int i = 0;
            float top = next >= 0 ? -440f : -370f;
            foreach (var m in save.TodaysMissions())
            {
                var row = UIKit.Image(s, "Mission", Card, UIKit.Rounded);
                UIKit.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, top - i * 104f), new Vector2(1180f, 92f));
                row.rectTransform.pivot = new Vector2(0.5f, 1f);
                var t = UIKit.Label(row.transform, "Text", m.Text, 28, UIKit.Fg, TextAnchor.MiddleLeft, true);
                UIKit.Place(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 12f), new Vector2(620f, 40f));
                t.rectTransform.pivot = new Vector2(0f, 0.5f);
                var barBg = UIKit.Image(row.transform, "Bar", UIKit.Line, UIKit.Pill);
                UIKit.Place(barBg.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, -22f), new Vector2(520f, 14f));
                barBg.rectTransform.pivot = new Vector2(0f, 0.5f);
                var bar = UIKit.Image(barBg.transform, "Fill", m.Done ? UIKit.Ok : UIKit.Ember, UIKit.Pill);
                bar.rectTransform.anchorMin = Vector2.zero;
                bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(m.progress / (float)m.goal), 1f);
                bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
                var prog = UIKit.Label(row.transform, "Prog", m.progress + " / " + m.goal, 22, UIKit.Muted, TextAnchor.MiddleLeft);
                UIKit.Place(prog.rectTransform, new Vector2(0f, 0.5f), new Vector2(570f, -22f), new Vector2(140f, 30f));
                prog.rectTransform.pivot = new Vector2(0f, 0.5f);
                var reward = UIKit.Label(row.transform, "Reward", m.coins + " coins", 24, UIKit.Flame, TextAnchor.MiddleRight, true);
                UIKit.Place(reward.rectTransform, new Vector2(1f, 0.5f), new Vector2(-250f, 0f), new Vector2(260f, 40f));
                reward.rectTransform.pivot = new Vector2(1f, 0.5f);
                var mission = m;
                var b = UIKit.Button(row.transform, "Claim", m.claimed ? "DONE" : m.Done ? "CLAIM" : "…", m.Done && !m.claimed ? UIKit.Ember : new Color(0.2f, 0.19f, 0.2f), m.Done && !m.claimed ? Dark : UIKit.Muted, () =>
                {
                    if (!mission.Done || mission.claimed) return;
                    game.Claim(mission);
                    ShowMissions();
                }, 26);
                UIKit.Place((RectTransform)b.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(190f, 62f));
                ((RectTransform)b.transform).pivot = new Vector2(1f, 0.5f);
                i++;
            }
        }

        void ShowDaily()
        {
            int day = game.DailyAvailable();
            if (day < 0) return;
            var p = Modal("Daily", new Vector2(760f, 460f));
            Text(p, "DAILY REWARD", 48, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(700f, 60f), true);
            Text(p, "Day " + (day + 1) + " of 7. Come back tomorrow for more.", 26, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(700f, 40f));
            var ic = UIKit.Image(p, "Icon", Color.white, UIKit.Coin);
            UIKit.Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70f, 10f), new Vector2(90f, 90f));
            ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Text(p, "+" + DailyReward.Coins[day], 64, UIKit.Flame, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(80f, 10f), new Vector2(200f, 80f), true);
            Btn(p, "CLAIM", true, () => { game.ClaimDaily(); CloseModal(); ShowHome(); }, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(300f, 86f), 38);
        }

        // ---------- settings ----------

        bool settingsFromPause;

        public void ShowSettings(bool fromPause)
        {
            settingsFromPause = fromPause;
            var s = fromPause ? Modal("Settings", new Vector2(1500f, 720f)) : NewScreen("Settings");
            if (!fromPause) { ShowLayer(false); TopBar(s, "SETTINGS", () => game.GoHome()); }
            else
            {
                Text(s, "SETTINGS", 44, UIKit.Fg, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(240f, -46f), new Vector2(400f, 56f), true);
                Btn(s, "DONE", true, () => { CloseModal(); ShowPause(true); }, new Vector2(1f, 1f), new Vector2(-130f, -46f), new Vector2(200f, 64f), 28);
            }
            var st = SaveService.Data.settings;
            ScrollRect sr;
            var content = UIKit.Scroll(s, "List", true, out sr);
            var view = (RectTransform)sr.transform;
            view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(fromPause ? 60f : 220f, 20f); view.offsetMax = new Vector2(fromPause ? -60f : -220f, fromPause ? -100f : -130f);
            float y = 0f;
            Action<string> header = h =>
            {
                var t = UIKit.Label(content, h, h, 24, UIKit.Ember, TextAnchor.LowerLeft, true);
                Row(t.rectTransform, ref y, 56f);
            };
            Action<RectTransform> row = r => Row(r, ref y, 74f);

            header("AUDIO");
            row(UIKit.SliderRow(content, "Sound effects", 0f, 1f, st.sfxVolume, v => { st.sfxVolume = v; game.ApplySettings(); }, Pct).transform.parent as RectTransform);
            row(UIKit.SliderRow(content, "Music", 0f, 1f, st.musicVolume, v => { st.musicVolume = v; game.ApplySettings(); }, Pct).transform.parent as RectTransform);
            header("CONTROLS");
            row(UIKit.SliderRow(content, "Look sensitivity", 0.3f, 2.5f, st.sensitivity, v => { st.sensitivity = v; game.ApplySettings(); }, v => v.ToString("0.0") + "×").transform.parent as RectTransform);
            row(UIKit.ToggleRow(content, "Invert look up/down", "", st.invertY, v => { st.invertY = v; game.ApplySettings(); }).transform.parent as RectTransform);
            row(UIKit.ToggleRow(content, "Vibration", "Buzz on blasts that hit close", st.haptics, v => { st.haptics = v; game.ApplySettings(); }).transform.parent as RectTransform);
            header("GRAPHICS");
            var q = UIKit.Rect(content, "Quality");
            row(q);
            var ql = UIKit.Label(q, "Label", "Quality", 26, UIKit.Fg, TextAnchor.MiddleLeft);
            ql.rectTransform.anchorMin = Vector2.zero; ql.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            ql.rectTransform.offsetMin = ql.rectTransform.offsetMax = Vector2.zero;
            string[] qn = { "AUTO", "LOW", "MEDIUM", "HIGH" };
            for (int i = 0; i < qn.Length; i++)
            {
                int val = i - 1;
                var b = UIKit.Button(q, qn[i], qn[i], st.quality == val ? UIKit.Ember : new Color(0.16f, 0.15f, 0.17f), st.quality == val ? Dark : UIKit.Fg, () => { st.quality = val; game.ApplySettings(); ShowSettings(settingsFromPause); }, 22);
                var br = (RectTransform)b.transform;
                br.anchorMin = br.anchorMax = new Vector2(1f, 0.5f);
                br.pivot = new Vector2(1f, 0.5f);
                br.sizeDelta = new Vector2(150f, 56f);
                br.anchoredPosition = new Vector2(-(3 - i) * 160f, 0f);
            }
            header("ACCESSIBILITY");
            row(UIKit.ToggleRow(content, "Reduce motion", "No slow motion, camera kicks or head bob", st.reduceMotion, v => { st.reduceMotion = v; game.ApplySettings(); }).transform.parent as RectTransform);
            row(UIKit.ToggleRow(content, "Reduce flashes", "Softer explosion flashes (photosensitivity)", st.reduceFlashes, v => { st.reduceFlashes = v; game.ApplySettings(); }).transform.parent as RectTransform);
            row(UIKit.ToggleRow(content, "Colour-blind fuse bar", "Blue / orange / grey instead of green / orange / red", st.colorblind, v => { st.colorblind = v; game.ApplySettings(); }).transform.parent as RectTransform);
            row(UIKit.ToggleRow(content, "Left-handed controls", "Buttons on the left, stick on the right", st.leftHanded, v => { st.leftHanded = v; game.ApplySettings(); }).transform.parent as RectTransform);
            header("ABOUT");
            var links = UIKit.Rect(content, "Links");
            row(links);
            LinkButton(links, "PRIVACY POLICY", 0, () => Application.OpenURL(PrivacyUrl));
            LinkButton(links, "TERMS OF USE", 1, () => Application.OpenURL(TermsUrl));
            LinkButton(links, "CREDITS", 2, ShowCredits);
            if (!fromPause)
            {
                var reset = UIKit.Rect(content, "Reset");
                row(reset);
                var rb = UIKit.Button(reset, "Reset", "RESET PROGRESS", new Color(0.35f, 0.1f, 0.1f), UIKit.Fg, () => Confirm("Reset all progress?", "Levels, stars, coins and purchases are wiped. Settings stay.", "RESET", () => game.ResetProgress()), 22);
                var rr = (RectTransform)rb.transform;
                rr.anchorMin = rr.anchorMax = new Vector2(0f, 0.5f);
                rr.pivot = new Vector2(0f, 0.5f);
                rr.sizeDelta = new Vector2(300f, 58f);
            }
            var ver = UIKit.Label(content, "Version", "BoomYard " + Application.version + " · Made with procedural everything", 20, UIKit.Muted, TextAnchor.MiddleLeft);
            Row(ver.rectTransform, ref y, 60f);
            content.sizeDelta = new Vector2(0f, -y + 20f);
        }

        static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

        static void Row(RectTransform r, ref float y, float h)
        {
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(0f, h);
            y -= h + 6f;
        }

        void LinkButton(RectTransform parent, string label, int i, Action onClick)
        {
            var b = UIKit.Button(parent, label, label, new Color(0.16f, 0.15f, 0.17f), UIKit.Fg, () => { Sfx.I.Tap(); onClick(); }, 22);
            var r = (RectTransform)b.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(270f, 58f);
            r.anchoredPosition = new Vector2(i * 290f, 0f);
        }

        void ShowCredits()
        {
            var p = Modal("Credits", new Vector2(900f, 520f));
            Text(p, "BOOMYARD", 56, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(800f, 70f), true);
            Text(p, "Every model, texture, sound and note in this game is generated by code.\nFonts: Rubik and Heebo (SIL Open Font License).\nBuilt with Unity.", 26, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(800f, 200f));
            Btn(p, "CLOSE", false, () => { CloseModal(); if (settingsFromPause) ShowSettings(true); }, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(240f, 76f), 28);
        }

        void Confirm(string title, string body, string yes, Action onYes)
        {
            var p = Modal("Confirm", new Vector2(900f, 420f));
            Text(p, title, 44, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(820f, 60f), true);
            Text(p, body, 26, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(800f, 100f));
            Btn(p, yes, true, () => { CloseModal(); onYes(); }, new Vector2(0.5f, 0f), new Vector2(140f, 70f), new Vector2(240f, 76f), 30);
            Btn(p, "CANCEL", false, CloseModal, new Vector2(0.5f, 0f), new Vector2(-140f, 70f), new Vector2(240f, 76f), 28);
        }

        // ---------- pause ----------

        public void ShowPause(bool on)
        {
            if (!on) { CloseModal(); return; }
            var p = Modal("Pause", new Vector2(640f, 600f));
            Text(p, "PAUSED", 60, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(600f, 70f), true);
            Btn(p, "RESUME", true, () => game.TogglePause(), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(420f, 82f), 34);
            Btn(p, "RESTART", false, () => { game.TogglePause(); game.Restart(); }, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(420f, 76f), 30);
            Btn(p, "SETTINGS", false, () => ShowSettings(true), new Vector2(0.5f, 1f), new Vector2(0f, -372f), new Vector2(420f, 76f), 30);
            Btn(p, "QUIT TO MAP", false, () => game.GoMap(), new Vector2(0.5f, 1f), new Vector2(0f, -464f), new Vector2(420f, 76f), 30);
        }

        // ---------- results ----------

        public void ShowResults(RoundSummary r)
        {
            ShowLayer(false);
            var s = NewScreen("Results");
            var save = SaveService.Data;
            if (r.Cleared)
            {
                Text(s, "LEVEL " + r.Level.Number + " · " + r.Level.Name.ToUpperInvariant(), 26, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 36f), true);
                Text(s, "CLEARED!", 110, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1200f, 120f), true);
                StartCoroutine(StarsIn(s, r.Stars));
                Stat(s, 0, "TIME LEFT", Loc.Time(r.TimeLeft));
                Stat(s, 1, "WRECKAGE", Loc.Money(r.Total));
                Stat(s, 2, "LONGEST CHAIN", r.LongestChain > 0 ? r.LongestChain + " links" : "–");
                Stat(s, 3, "COINS EARNED", "+" + Loc.Coins(r.Coins));
                if (r.Record && r.PreviousBest > 0) Text(s, "NEW WRECKAGE RECORD", 26, UIKit.Ember, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(800f, 34f), true);
                Text(s, Loc.T(r.TipKey), 22, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(1300f, 34f));
                bool last = r.Level.Number >= Levels.All.Length;
                Btn(s, last ? "MAP" : "NEXT LEVEL", true, () => { if (last) game.GoMap(); else game.NextLevel(); }, new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(380f, 100f), 42);
                Btn(s, "RETRY", false, () => game.Restart(), new Vector2(0.5f, 0f), new Vector2(-330f, 100f), new Vector2(220f, 86f), 30);
                Btn(s, "MAP", false, () => game.GoMap(), new Vector2(0.5f, 0f), new Vector2(330f, 100f), new Vector2(220f, 86f), 30);
            }
            else
            {
                Text(s, "LEVEL " + r.Level.Number + " · " + r.Level.Name.ToUpperInvariant(), 26, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 36f), true);
                Text(s, r.FailReason == "spare" ? Loc.T("fail.spare") : r.FailReason == "time" ? "TIME'S UP" : "OUT OF EXPLOSIVES", 100, UIKit.Danger, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1400f, 120f), true);
                Text(s, r.FailReason == "spare" ? "The " + (game.Arena.Spare != null ? game.Arena.Spare.Name.ToLowerInvariant() : "one you had to keep") + " had to survive" : r.TargetsLeft + " of " + r.Targets + " targets still standing", 36, UIKit.Fg, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1200f, 50f));
                Text(s, "You keep the wreckage:  +" + Loc.Coins(r.Coins) + " coins", 28, UIKit.Muted, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 40f));
                Text(s, Loc.T(r.TipKey), 24, UIKit.Flame, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1300f, 40f));
                Btn(s, "TRY AGAIN", true, () => game.Restart(), new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(380f, 100f), 42);
                Btn(s, "SHOP", false, () => ShowShop(), new Vector2(0.5f, 0f), new Vector2(-330f, 110f), new Vector2(220f, 86f), 30);
                Btn(s, "MAP", false, () => game.GoMap(), new Vector2(0.5f, 0f), new Vector2(330f, 110f), new Vector2(220f, 86f), 30);
            }
            Wallet(s);
        }

        void Stat(RectTransform s, int i, string label, string value)
        {
            float x = (i - 1.5f) * 330f;
            var c = UIKit.Image(s, label, Card, UIKit.Rounded);
            UIKit.Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -40f), new Vector2(300f, 130f));
            c.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var l = UIKit.Label(c.transform, "L", label, 20, UIKit.Muted, TextAnchor.UpperCenter, true);
            UIKit.Place(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(300f, 28f));
            l.rectTransform.pivot = new Vector2(0.5f, 1f);
            var v = UIKit.Label(c.transform, "V", value, 44, UIKit.Fg, TextAnchor.MiddleCenter, true);
            UIKit.Place(v.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(300f, 60f));
            v.rectTransform.pivot = new Vector2(0.5f, 0f);
        }

        System.Collections.IEnumerator StarsIn(RectTransform s, int stars)
        {
            var imgs = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                var st = UIKit.Image(s, "Star", new Color(1f, 1f, 1f, 0.15f), UIKit.Star);
                UIKit.Place(st.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 130f, 130f + (i == 1 ? 20f : 0f)), new Vector2(110f, 110f));
                st.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                imgs.Add(st);
            }
            for (int i = 0; i < stars; i++)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                if (imgs[i] == null) yield break;
                imgs[i].color = UIKit.Flame;
                imgs[i].rectTransform.localScale = Vector3.one * 1.4f;
                Sfx.I.Star();
                for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
                {
                    if (imgs[i] == null) yield break;
                    imgs[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, t / 0.2f);
                    yield return null;
                }
                imgs[i].rectTransform.localScale = Vector3.one;
            }
        }
    }
}
