// Bridge to Google's H5 Games Ads (Ad Placement API: adBreak/adConfig), set up in the page template (as in Overlane).
mergeInto(LibraryManager.library, {
  BoomAdsReady: function () {
    var c = window.BOOMYARD_ADS && window.BOOMYARD_ADS.client;
    return (c && c.indexOf("__") !== 0 && typeof window.adBreak === "function") ? 1 : 0;
  },

  BoomShowAd: function (typePtr, namePtr) {
    var type = UTF8ToString(typePtr), name = UTF8ToString(namePtr);
    var send = function (r) { if (window.unityInstance) window.unityInstance.SendMessage("AdsBridge", "OnAdResult", r); };
    var rewarded = false, shown = false, finished = false;
    var finish = function (r) { if (!finished) { finished = true; send(r); } };
    var o = {
      type: type === "reward" ? "reward" : "next",
      name: name,
      beforeAd: function () { shown = true; send("pause"); },
      afterAd: function () { send("resume"); },
      adBreakDone: function () { finish(type === "reward" ? (rewarded ? "reward" : (shown ? "dismissed" : "none")) : "done"); }
    };
    if (type === "reward") {
      o.beforeReward = function (showAdFn) { showAdFn(); };
      o.adDismissed = function () {};
      o.adViewed = function () { rewarded = true; };
    }
    try { window.adBreak(o); } catch (e) { finish(type === "reward" ? "none" : "done"); }
  }
});
