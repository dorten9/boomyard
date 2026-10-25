using System;
using UnityEngine;
#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace BoomYard.Game
{
    /// <summary>A pack of coins sold for real money through Google Play or the App Store.</summary>
    public sealed class CoinPack
    {
        /// <summary>Product id, the same in Google Play Console and App Store Connect. Permanent once created.</summary>
        public string Id;
        public int Coins;
        public string Name;
        /// <summary>Shown only until the store reports the real, local price.</summary>
        public string UsdPrice;
        public string Badge;
    }

    /// <summary>
    /// Real-money store for coins (Unity IAP: Google Play Billing on Android, StoreKit on iOS), as in Overlane.
    /// The store takes the payment; the game grants the coins only once the store confirms it, and saves before
    /// telling the store it was delivered, so a crash in between never loses a purchase. Every pack is used up
    /// (consumable), so there is nothing to restore. The web build sells nothing: the shop shows the packs and says
    /// they are bought in the app.
    /// </summary>
    public static class Purchases
    {
        public static readonly CoinPack[] Packs =
        {
            new CoinPack { Id = "coins_2500", Coins = 2500, Name = "POCKET OF COINS", UsdPrice = "$0.99" },
            new CoinPack { Id = "coins_14000", Coins = 14000, Name = "BAG OF COINS", UsdPrice = "$4.99", Badge = "+11% BONUS" },
            new CoinPack { Id = "coins_30000", Coins = 30000, Name = "SACK OF COINS", UsdPrice = "$9.99", Badge = "MOST POPULAR" },
            new CoinPack { Id = "coins_85000", Coins = 85000, Name = "SAFE OF COINS", UsdPrice = "$24.99", Badge = "BEST VALUE" },
        };

        public enum State { NotSupported, Connecting, Ready, Unavailable }

        public static State Status { get; private set; }
        public static bool Available { get { return Status == State.Ready; } }

        /// <summary>Raised when the store connects or coins are delivered, so open screens can refresh.</summary>
        public static event Action Changed;
        /// <summary>A pack was delivered, for the celebration on screen.</summary>
        public static event Action<CoinPack> Granted;

        public static CoinPack Find(string id)
        {
            foreach (var p in Packs) if (p.Id == id) return p;
            return null;
        }

        /// <summary>The store's own price string in the player's currency, or the US price until it is known.</summary>
        public static string PriceOf(CoinPack pack)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (listener.controller != null)
            {
                var product = listener.controller.products.WithID(pack.Id);
                if (product != null && product.metadata != null && !string.IsNullOrEmpty(product.metadata.localizedPriceString))
                    return product.metadata.localizedPriceString;
            }
#endif
            return pack.UsdPrice;
        }

        static void Grant(CoinPack pack)
        {
            var s = SaveService.Data;
            s.shekels += pack.Coins;
            s.purchases++;
            SaveService.Save();
        }

#if UNITY_ANDROID || UNITY_IOS
        static readonly Listener listener = new Listener();
        static Action<bool, string> pending;

        public static void Init()
        {
            if (Status == State.Connecting || Status == State.Ready) return;
            Status = State.Connecting;
            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (var p in Packs) builder.AddProduct(p.Id, ProductType.Consumable);
                UnityPurchasing.Initialize(listener, builder);
            }
            catch (Exception e)
            {
                Debug.LogWarning("BoomYard: store init failed: " + e.Message);
                Status = State.Unavailable;
            }
        }

        /// <summary>Opens the store's payment sheet. done(ok, message) comes back when the store answers; message is null when the player cancelled.</summary>
        public static void Buy(CoinPack pack, Action<bool, string> done)
        {
            if (!Available || listener.controller == null) { if (done != null) done(false, "The store is not available right now."); return; }
            var product = listener.controller.products.WithID(pack.Id);
            if (product == null || !product.availableToPurchase) { if (done != null) done(false, "This pack is not available right now."); return; }
            pending = done;
            listener.controller.InitiatePurchase(product);
        }

        class Listener : IDetailedStoreListener
        {
            public IStoreController controller;

            public void OnInitialized(IStoreController c, IExtensionProvider e)
            {
                controller = c;
                Status = State.Ready;
                if (Changed != null) Changed();
            }

            public void OnInitializeFailed(InitializationFailureReason error) { OnInitializeFailed(error, null); }

            public void OnInitializeFailed(InitializationFailureReason error, string message)
            {
                Debug.LogWarning("BoomYard: store unavailable: " + error + " " + message);
                Status = State.Unavailable;
                if (Changed != null) Changed();
            }

            public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
            {
                // Also called on launch for a purchase that was paid but not delivered (the app was closed in between).
                var pack = Find(args.purchasedProduct.definition.id);
                if (pack != null)
                {
                    Grant(pack);
                    if (Granted != null) Granted(pack);
                }
                if (Changed != null) Changed();
                Finish(true, null);
                return PurchaseProcessingResult.Complete;
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
            {
                Finish(false, reason == PurchaseFailureReason.UserCancelled ? null : "The purchase did not go through.");
            }

            public void OnPurchaseFailed(Product product, PurchaseFailureDescription description)
            {
                OnPurchaseFailed(product, description.reason);
            }

            static void Finish(bool ok, string message)
            {
                var cb = pending;
                pending = null;
                if (cb != null) cb(ok, message);
            }
        }
#else
        public static void Init() { Status = State.NotSupported; if (Changed != null) Changed(); }

        public static void Buy(CoinPack pack, Action<bool, string> done)
        {
            if (done != null) done(false, "Coins are bought in the BoomYard app.");
        }
#endif
    }
}
