using System;
using System.Collections.Generic;

namespace BoomYard.Core
{
    public enum RoundState { Loadout, Playing, Stunned, Settling, Results }

    /// <summary>
    /// The round state machine (spec §16): Loadout → Playing ⇄ Stunned → Settling → Results.
    /// Settling lets fuses and burning objects already going finish before the bill.
    /// </summary>
    public sealed class RoundModel
    {
        public const float Duration = 90f;
        public const float StunSeconds = 2.5f;
        public const float HandBurnStun = 3f;
        public const float MaxSettle = 6f;

        public RoundState State { get; private set; }
        public float TimeLeft { get; private set; }
        public float Elapsed { get; private set; }
        public float StunLeft { get; private set; }
        float settle;

        public event Action<RoundState> Changed;

        /// <summary>This round's time limit (levels set their own).</summary>
        public float Limit { get; private set; }

        public RoundModel() { State = RoundState.Loadout; TimeLeft = Limit = Duration; }

        public void Start(float limit = Duration)
        {
            Limit = limit;
            TimeLeft = limit;
            Elapsed = 0f;
            StunLeft = 0f;
            Set(RoundState.Playing);
        }

        public void Stun(float seconds)
        {
            if (State != RoundState.Playing && State != RoundState.Stunned) return;
            StunLeft = Math.Max(StunLeft, seconds);
            Set(RoundState.Stunned);
        }

        /// <summary>Advances the round. <paramref name="busy"/>: something is still burning or about to go off.</summary>
        public void Tick(float dt, bool busy)
        {
            switch (State)
            {
                case RoundState.Playing:
                case RoundState.Stunned:
                    Elapsed += dt;
                    if (!Frozen) TimeLeft = Math.Max(0f, TimeLeft - dt);
                    if (State == RoundState.Stunned)
                    {
                        StunLeft -= dt;
                        if (StunLeft <= 0f) { StunLeft = 0f; Set(RoundState.Playing); }
                    }
                    if (TimeLeft <= 0f) { settle = 0f; Set(RoundState.Settling); }
                    break;
                case RoundState.Settling:
                    Elapsed += dt;
                    settle += dt;
                    if (!busy || settle >= MaxSettle) Set(RoundState.Results);
                    break;
            }
        }

        /// <summary>Holds the clock (the tutorial stops it while it explains).</summary>
        public bool Frozen;

        /// <summary>Ends the round early (every item used and nothing left going off).</summary>
        public void Finish()
        {
            if (State == RoundState.Playing || State == RoundState.Stunned) { settle = 0f; Set(RoundState.Settling); }
        }

        public bool CanAct { get { return State == RoundState.Playing; } }

        void Set(RoundState s)
        {
            if (State == s) return;
            State = s;
            if (Changed != null) Changed(s);
        }
    }

    /// <summary>One slot of the pocket: an item and how many are left this round.</summary>
    [Serializable]
    public sealed class PocketSlot
    {
        public string id;
        public int count;
    }

    /// <summary>
    /// What the player carries into a round: a slot per kind of explosive, 6 kinds, 8 with the vest. Every kind is
    /// unlimited (the owner's call: no counting charges); a slot's count is only kept for the tutorial's kit.
    /// </summary>
    public sealed class Pocket
    {
        public const int BaseSlots = 6, MaxSlots = 8;
        public bool Unlimited = true;
        public readonly List<PocketSlot> Slots = new List<PocketSlot>();
        public int Selected;

        public PocketSlot Current { get { return Selected >= 0 && Selected < Slots.Count ? Slots[Selected] : null; } }

        public int TotalLeft
        {
            get
            {
                if (Unlimited) return Slots.Count > 0 ? int.MaxValue : 0;
                int n = 0; foreach (var s in Slots) n += s.count; return n;
            }
        }

        /// <summary>Takes one of the selected item; returns its id or null when the slot is empty.</summary>
        public string Take()
        {
            var s = Current;
            if (s == null || s.count <= 0) return null;
            if (!Unlimited) s.count--;
            return s.id;
        }

        /// <summary>Moves the selection to the next slot that still has something.</summary>
        public void SelectNextAvailable()
        {
            if (Current != null && Current.count > 0) return;
            for (int i = 1; i <= Slots.Count; i++)
            {
                int j = (Selected + i) % Slots.Count;
                if (Slots[j].count > 0) { Selected = j; return; }
            }
        }
    }

    public static class Daily
    {
        /// <summary>The same seed everywhere on a given date: the daily yard, its pocket and its wind.</summary>
        public static int Seed(DateTime date) { return date.Year * 10000 + date.Month * 100 + date.Day; }
    }

    public static class Economy
    {
        /// <summary>Yard props get 20% dearer with every purchase of the same kind (spec §14).</summary>
        public static long PropPrice(int basePrice, int ownedOfKind)
        {
            return (long)Math.Round(basePrice * Math.Pow(1.2, ownedOfKind));
        }

        /// <summary>Shop pack price: 5 units for the price of ~4.4 (the M-80 pack is $7,900 for 5 × $1,800).</summary>
        public static long PackPrice(int unitPrice, int units) { return units >= 5 ? (long)Math.Round(unitPrice * units * 0.8778 / 100.0) * 100 : unitPrice * units; }
    }
}
