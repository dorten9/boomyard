using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>
    /// The first level teaches by doing: look, walk to the bin, place a firecracker, light it, run, then light
    /// one in the hand and throw it, and finally clear the yard against the clock. The clock is held until then.
    /// </summary>
    public sealed class Tutorial : MonoBehaviour
    {
        public enum Step { None, Look, Move, Place, Light, Run, Throw, Finish }

        public Step Current { get; private set; }
        public bool Active { get { return Current != Step.None; } }
        public int Index { get { return (int)Current - 1; } }
        public const int Count = 7;

        GameController game;
        float looked, stepTime;
        float lastYaw;

        public void Begin(GameController g)
        {
            game = g;
            Go(Step.Look);
            lastYaw = g.Player.Yaw;
        }

        public void Stop()
        {
            Current = Step.None;
            if (game != null) game.Round.Frozen = false;
        }

        /// <summary>What to tell the player now.</summary>
        public string Text
        {
            get
            {
                switch (Current)
                {
                    case Step.Look: return "Drag on the right side of the screen to look around";
                    case Step.Move: return "Use the left stick to walk up to the green bin";
                    case Step.Place: return "Tap PLACE to set a firecracker down next to it";
                    case Step.Light: return "Hold LIGHT until the fuse catches";
                    case Step.Run: return "It's lit. RUN! Get some distance";
                    case Step.Throw: return "Now hold LIGHT with nothing nearby to light one in your hand, then THROW it at the mailbox";
                    case Step.Finish: return "Destroy everything before the clock runs out!";
                    default: return "";
                }
            }
        }

        /// <summary>Which control to highlight: "look", "move", "place", "light", "throw" or "".</summary>
        public string Highlight
        {
            get
            {
                switch (Current)
                {
                    case Step.Look: return "look";
                    case Step.Move: return "move";
                    case Step.Place: return "place";
                    case Step.Light: return "light";
                    case Step.Throw: return LiveExplosive.Active.Count > 0 || game == null || !game.Player.HeldLit ? "light" : "throw";
                    default: return "";
                }
            }
        }

        void Go(Step s)
        {
            Current = s;
            stepTime = 0f;
            if (game != null) game.Round.Frozen = s != Step.Finish && s != Step.None;
            if (s != Step.Look && s != Step.None) Sfx.I.Confirm();
        }

        public void OnEvent(string e)
        {
            if (!Active) return;
            if (Current == Step.Place && e == "placed") Go(Step.Light);
            else if (Current == Step.Light && e == "lit") Go(Step.Run);
            else if (Current == Step.Run && e == "boom") Go(Step.Throw);
            else if (Current == Step.Throw && e == "thrown" && game.Player.HeldLit == false) Go(Step.Finish);
        }

        void Update()
        {
            if (!Active || game == null) return;
            stepTime += Time.deltaTime;
            var p = game.Player;
            switch (Current)
            {
                case Step.Look:
                    looked += Mathf.Abs(Mathf.DeltaAngle(lastYaw, p.Yaw));
                    lastYaw = p.Yaw;
                    if (looked > 70f && stepTime > 1f) Go(Step.Move);
                    break;
                case Step.Move:
                    foreach (var t in game.Arena.Targets)
                        if (t != null && t.Def.Id == "bin" && Vector3.Distance(p.transform.position, t.transform.position) < 3.2f) { Go(Step.Place); break; }
                    break;
                case Step.Throw:
                    // A thrown unlit one doesn't count; a placed-and-lit one does as well.
                    break;
                case Step.Finish:
                    if (stepTime > 5f) Stop();
                    break;
            }
        }

        /// <summary>The prop the tutorial points at right now (for the HUD marker), or null.</summary>
        public Destructible Focus
        {
            get
            {
                if (game == null || !Active) return null;
                string id = Current == Step.Throw ? "mailbox" : Current == Step.Move || Current == Step.Place ? "bin" : null;
                if (id == null) return null;
                foreach (var t in game.Arena.Targets) if (t != null && !t.Destroyed && t.Def.Id == id) return t;
                return null;
            }
        }
    }
}
