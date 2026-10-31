using System;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// Ads, as in Overlane: rewarded videos the player chooses to watch for a bonus (double coins, a retry with
    /// more time, free coins in the shop) and an occasional break between levels. On the web this uses Google's
    /// H5 Games Ads (AdSense) when a publisher id is set in the page; without one (in the editor, and in the
    /// apps until an ad network is added) a labelled test ad is shown so every placement can be tried.
    /// Players who have bought coins never get the breaks; videos for rewards stay available to everyone.
    /// </summary>
    public sealed class Ads : MonoBehaviour
    {
        public static Ads I { get; private set; }

        /// <summary>Shows the in-game test ad: (rewarded, onClosed(rewardEarned)). Set by the UI.</summary>
        public Action<bool, Action<bool>> TestAd;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int BoomAdsReady();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void BoomShowAd(string type, string name);
        static bool RealAds { get { return BoomAdsReady() == 1; } }
#else
        static bool RealAds { get { return false; } }
        static void BoomShowAd(string type, string name) { }
#endif

        Action<bool> pendingReward;
        Action pendingDone;
        public bool Busy { get; private set; }

        // Breaks: never before level 6, at most one every 3 minutes, every 3rd result screen.
        const float BreakGap = 180f;
        const int ResultsPerBreak = 3;
        float lastBreak = -1000f;
        int resultsSince;

        public static Ads Create()
        {
            var go = new GameObject("AdsBridge");   // name matters: the page sends results to this object
            DontDestroyOnLoad(go);
            I = go.AddComponent<Ads>();
            return I;
        }

        /// <summary>Rewarded video. done(true) only when the ad was watched to the end.</summary>
        public void ShowRewarded(string placement, Action<bool> done)
        {
            if (Busy) return;
            Busy = true;
            if (RealAds)
            {
                pendingReward = done;
                BoomShowAd("reward", placement);
            }
            else if (TestAd != null) TestAd(true, r => { Busy = false; done(r); });
            else { Busy = false; done(true); }
        }

        public void ShowBreak(string placement, Action done)
        {
            if (Busy) { done(); return; }
            Busy = true;
            if (RealAds)
            {
                pendingDone = done;
                BoomShowAd("next", placement);
            }
            else if (TestAd != null) TestAd(false, r => { Busy = false; done(); });
            else { Busy = false; done(); }
        }

        /// <summary>Called when leaving a result screen: maybe a break, then carries on.</summary>
        public void BetweenLevels(Action then)
        {
            resultsSince++;
            var save = SaveService.Data;
            if (save.purchases > 0 || save.unlocked < 6 || resultsSince < ResultsPerBreak || Time.realtimeSinceStartup - lastBreak < BreakGap)
            {
                then();
                return;
            }
            resultsSince = 0;
            lastBreak = Time.realtimeSinceStartup;
            ShowBreak("between_levels", then);
        }

        /// <summary>Messages from the page (Ads.jslib).</summary>
        public void OnAdResult(string result)
        {
            switch (result)
            {
                case "pause": AudioListener.pause = true; return;
                case "resume": AudioListener.pause = false; return;
            }
            AudioListener.pause = false;
            Busy = false;
            if (pendingReward != null)
            {
                var cb = pendingReward;
                pendingReward = null;
                cb(result == "reward");
            }
            if (pendingDone != null)
            {
                var cb = pendingDone;
                pendingDone = null;
                cb();
            }
        }
    }
}
