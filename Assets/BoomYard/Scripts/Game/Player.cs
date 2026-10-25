using BoomYard.Core;
using UnityEngine;

namespace BoomYard.Game
{
    /// <summary>What the controls asked for this frame. Filled by the touch/keyboard layer (UI/TouchControls).</summary>
    public sealed class InputFrame
    {
        public Vector2 Move;          // -1..1 joystick
        public Vector2 Look;          // degrees this frame (yaw, pitch)
        public bool Run;
        public bool LighterHeld, LighterDown, LighterUp;
        public bool ThrowHeld, ThrowUp;
        public float ThrowPower = 0.35f;   // 0..1, a tap is a short throw, dragging up adds power
        public bool Place;
        public Vector2 PlaceScreen;
        public int Select = -1;
        public bool Pause;
        public bool Detonate;         // the remote's BOOM button
        public bool SprayToggle;      // the can in or out of the hand
        public bool PaintHeld;        // a finger painting with the can (at PaintScreen)
        public Vector2 PaintScreen;

        public void ClearEdges()
        {
            LighterDown = LighterUp = ThrowUp = Place = Pause = Detonate = SprayToggle = false;
            Select = -1;
            Look = Vector2.zero;
        }
    }

    /// <summary>
    /// The first-person player (spec §04, §05): eye height 1.65 m, FOV 75, head bob, camera shake by distance,
    /// knockdown in a blast wave. Owns the light → hold → throw / place flow of the item in the right hand.
    /// </summary>
    public sealed class Player : MonoBehaviour
    {
        public static Player I { get; private set; }

        public const float EyeHeight = 1.65f, Fov = 75f;
        public const float WalkSpeed = 2.4f, RunSpeed = 5f;
        public const float PlaceReach = 3.2f;
        public const float MinThrow = 4f, MaxThrow = 15f;

        public Camera Cam;
        public Hands Hands;
        public readonly InputFrame In = new InputFrame();
        public Ignition Ignition;
        public float Sensitivity = 1f;

        public ExplosiveDef HeldDef { get; private set; }
        public bool HeldLit { get; private set; }
        public float HeldRemaining { get; private set; }
        public float HeldTotal { get; private set; }
        public float PredictedFlight { get; private set; }
        public bool Knocked { get { return downLeft > 0f; } }
        public bool Running { get; private set; }
        public float Yaw { get { return yaw; } }
        public bool Controllable;

        CharacterController cc;
        Transform head;
        float yaw, pitch, bob, shake, downLeft, downTotal, fallSpeed, fovKick;
        LineRenderer arc, ring;
        readonly Vector3[] arcPoints = new Vector3[48];

        public static Player Create(Vector3 feet, float yaw)
        {
            var go = new GameObject("Player");
            go.layer = Layers.NoRaycast;
            go.transform.position = feet;
            var p = go.AddComponent<Player>();
            p.yaw = yaw;
            return p;
        }

        void Awake()
        {
            I = this;
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;

            head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, EyeHeight, 0f);
            var camGo = new GameObject("Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(head, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.fieldOfView = Fov;
            Cam.nearClipPlane = 0.03f;
            Cam.farClipPlane = 220f;
            Cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();

            Hands = Hands.Build(Cam.transform);
            arc = Line("ThrowArc", new Color(0.92f, 0.9f, 0.87f, 0.55f), 0.012f);
            ring = Line("SafeRing", new Color(1f, 0.48f, 0.1f, 0.8f), 0.03f);
            ring.loop = true;
            ring.positionCount = 40;
        }

        LineRenderer Line(string name, Color color, float width)
        {
            var go = new GameObject(name);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.ParticleAdd;
            lr.startColor = lr.endColor = color;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            return lr;
        }

        public void ResetForRound(Vector3 feet, float startYaw, LighterDef lighter, int seed)
        {
            cc.enabled = false;
            transform.position = feet;
            cc.enabled = true;
            yaw = startYaw;
            pitch = 4f;
            downLeft = 0f;
            shake = 0f;
            Ignition = new Ignition(lighter, seed);
            HeldDef = null;
            HeldLit = false;
            Hands.SetItem(null);
            Hands.SetLit(false);
            Hands.SetCan(null);
            Hands.Spraying = false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var game = GameController.I;
            bool act = Controllable && game != null && game.Round.CanAct && !game.Paused && !Knocked;

            if (In.Pause && game != null) game.TogglePause();

            Look(act ? In.Look : Vector2.zero);
            Move(act ? In.Move : Vector2.zero, act && In.Run, dt);
            Knockdown(dt);

            if (Controllable && game != null && game.Round.State != RoundState.Results)
            {
                var spray = Spray.I;
                if (act && In.SprayToggle && !HeldLit) SetCan(spray != null && !spray.Active);
                if (act && In.Select >= 0 && !HeldLit)
                {
                    int sel = Mathf.Clamp(In.Select, 0, game.Pocket.Slots.Count - 1);
                    // Tapping the timer charge again sets its delay: 5, 10 or 15 seconds.
                    if (sel == game.Pocket.Selected && HeldDef != null && HeldDef.Timer && !Spraying) game.CycleTimer();
                    game.Pocket.Selected = sel;
                    SetCan(false);   // picking an explosive puts the can away
                }
                if (act && In.Detonate) { if (game.Level.Show) game.LaunchShow(); else game.DetonateRemotes(); }
                if (Spraying)
                {
                    // The can: hold SPRAY to paint at the crosshair, tap a wall to stamp the stencil.
                    Ignition.Release();
                    LightTarget = null;
                    if (act && (In.LighterHeld || spray.HeldForShot)) spray.Paint(Cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));
                    else if (act && In.PaintHeld) spray.Paint(Cam.ScreenPointToRay(In.PaintScreen));
                    else if (spray.Painting) spray.Release();
                    // A tap stamps the stencil; with none on the can, a tap is just a puff of paint.
                    if (act && In.Place)
                    {
                        var ray = Cam.ScreenPointToRay(In.PlaceScreen);
                        if (spray.Stencil == null) { spray.Paint(ray); spray.Release(); }
                        else if (!spray.Stamp(ray)) game.Toast("toast.nowall");
                    }
                    Hands.Spraying = spray.Painting;
                }
                else
                {
                    Hold(game);
                    if (act) Lighter(game, dt);
                    else Ignition.Release();
                }
                Fuse(game, dt);
                // When the clock runs out with a lit item in the hand, it can still be thrown.
                bool late = game.Round.State == RoundState.Settling && HeldLit && !Knocked && !game.Paused;
                if ((act || late) && !Spraying) ThrowAndPlace(game);
            }
            else
            {
                Ignition.Release();
                if (Spraying && Spray.I.Painting) Spray.I.Release();
                Hands.Spraying = false;
            }

            Hands.FlameOn = Ignition != null && Ignition.Flame == FlameState.Lit;
            Hands.Lighting = Hands.FlameOn && (LightTarget != null || HeldDef != null && !HeldLit);
            Sfx.I.Flame(Hands.FlameOn);
            Sfx.I.Hiss(HeldLit, HeldTotal > 0f ? HeldRemaining / HeldTotal : 1f);
            Predict(HeldLit);
            In.ClearEdges();
        }

        /// <summary>The spray can is in the hand instead of an explosive.</summary>
        public bool Spraying { get { return Spray.I != null && Spray.I.Active; } }

        /// <summary>Takes the spray can out (the explosive goes back in the pocket) or puts it away.</summary>
        public void SetCan(bool on)
        {
            var spray = Spray.I;
            if (spray == null || spray.Active == on) return;
            spray.SetActive(on);
            Hands.Spraying = false;
            if (on) { HeldDef = null; Hands.SetCan(spray.Color); }
            else Hands.SetCan(null);   // Hold() puts the selected explosive back in the hand
        }

        /// <summary>Puts the player somewhere, looking a given way (screenshot runs).</summary>
        public void Teleport(Vector3 feet, float toYaw, float toPitch)
        {
            cc.enabled = false;
            transform.position = feet;
            cc.enabled = true;
            yaw = toYaw;
            pitch = toPitch;
        }

        /// <summary>A new colour on the can (from the colour strip or the shop).</summary>
        public void RefreshCan() { if (Spraying) Hands.SetCan(Spray.I.Color); }

        void Look(Vector2 delta)
        {
            // In the menus the camera drifts slowly over the street.
            if (!Controllable) delta.x += Mathf.Sin(Time.unscaledTime * 0.15f) * 0.03f;
            yaw += delta.x * Sensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * Sensitivity, -80f, 80f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        void Move(Vector2 move, bool run, float dt)
        {
            if (move.sqrMagnitude > 1f) move.Normalize();
            Running = run && move.sqrMagnitude > 0.2f;
            float speed = Running ? (SaveService.Data != null && SaveService.Data.Has("legs") ? RunSpeed * 1.3f : RunSpeed) : WalkSpeed;
            var v = (transform.forward * move.y + transform.right * move.x) * speed;
            fallSpeed = cc.isGrounded ? -1f : fallSpeed - 9.81f * dt;
            v.y = fallSpeed;
            if (cc.enabled) cc.Move(v * dt);

            float moving = new Vector2(v.x, v.z).magnitude;
            bob += dt * moving * 2.2f;
            float amount = Mathf.Clamp01(moving / WalkSpeed) * (GameController.ReduceMotion ? 0.3f : 1f);
            var bobOffset = new Vector3(Mathf.Cos(bob) * 0.012f, Mathf.Sin(bob * 2f) * 0.028f, 0f) * amount;

            shake = Mathf.MoveTowards(shake, 0f, dt * 1.6f);
            fovKick = Mathf.Lerp(fovKick, 0f, 1f - Mathf.Exp(-dt * 6f));
            Cam.fieldOfView = Fov + (GameController.ReduceMotion ? 0f : fovKick);
            float s = GameController.ReduceMotion ? shake * 0.25f : shake;
            var shakeOffset = new Vector3(Mathf.PerlinNoise(Time.time * 25f, 1f) - 0.5f, Mathf.PerlinNoise(2f, Time.time * 25f) - 0.5f, 0f) * s * 0.12f;
            float roll = (Mathf.PerlinNoise(Time.time * 18f, 5f) - 0.5f) * s * 6f;

            if (!Knocked)
            {
                head.localPosition = new Vector3(0f, EyeHeight, 0f) + bobOffset + shakeOffset;
                head.localRotation = Quaternion.Euler(pitch, 0f, roll);
            }
        }

        /// <summary>On the ground for a moment: the camera drops to the floor and rolls, the ears ring (spec §04).</summary>
        void Knockdown(float dt)
        {
            if (downLeft <= 0f) return;
            downLeft -= dt;
            float t = 1f - downLeft / downTotal;
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.12f));
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.8f) / 0.2f));
            float k = fall * (1f - rise);
            head.localPosition = new Vector3(0f, Mathf.Lerp(EyeHeight, 0.28f, k), 0f);
            head.localRotation = Quaternion.Euler(Mathf.Lerp(pitch, -10f, k) + Mathf.Sin(t * 9f) * 4f * k, Mathf.Sin(t * 5f) * 25f * k, Mathf.Lerp(0f, 72f, k));
            if (downLeft <= 0f) downLeft = 0f;
        }

        public void KnockDown(float seconds)
        {
            downTotal = Mathf.Max(seconds, 0.5f);
            downLeft = downTotal;
            Ignition.Release();
            Sfx.I.Thud();
            Sfx.I.Ring();
        }

        /// <summary>Puts the selected item in the right hand (unlit; nothing is used up until it catches).</summary>
        void Hold(GameController game)
        {
            if (HeldLit) return;
            game.Pocket.SelectNextAvailable();
            var slot = game.Pocket.Current;
            var def = slot != null && slot.count > 0 ? Catalog.Explosive(slot.id) : null;
            if (def != HeldDef)
            {
                HeldDef = def;
                Hands.SetItem(def);
                Ignition.NextFuse();
            }
        }

        /// <summary>The unlit charge the lighter would reach right now (placed or thrown earlier), or null.</summary>
        public LiveExplosive LightTarget { get; private set; }
        public const float LightReach = 2.1f;

        LiveExplosive FindLightTarget()
        {
            LiveExplosive best = null;
            float bestScore = float.MaxValue;
            var eye = Cam.transform.position;
            var flat = new Vector3(Cam.transform.forward.x, 0f, Cam.transform.forward.z).normalized;
            foreach (var l in LiveExplosive.Active)
            {
                if (l == null || l.Lit || l.Def.Remote || l.Def.Timer) continue;
                var to = l.transform.position - transform.position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > LightReach) continue;
                float facing = d < 0.4f ? 1f : Vector3.Dot(flat, to / d);
                if (facing < 0.25f) continue;
                float score = d - facing;
                if (score < bestScore) { bestScore = score; best = l; }
            }
            return best;
        }

        /// <summary>
        /// The lighter: strike it, then hold the flame to a fuse. A charge lying in reach comes first (place → light
        /// → run); otherwise it lights the item in the hand, to throw.
        /// </summary>
        void Lighter(GameController game, float dt)
        {
            LightTarget = HeldLit ? null : FindLightTarget();
            float exposure = game.ArenaDef.Outdoors ? game.Wind.Exposure(yaw) : 0f;
            if (In.LighterDown)
            {
                Sfx.I.Click();
                if (Ignition.Strike(exposure, Running) == FlameState.BlownOut)
                    game.Toast(Running ? "toast.run" : Ignition.Lighter.BreakChance > 0f ? "toast.match" : "toast.wind");
            }
            if (In.LighterHeld && Ignition.Flame == FlameState.Lit)
            {
                if (LightTarget != null)
                {
                    if (Ignition.Hold(dt, Running))
                    {
                        LightTarget.Light();
                        game.OnIgnite(LightTarget.Def);
                        game.Toast("toast.runaway");
                        Ignition.NextFuse();
                        Ignition.Release();
                    }
                }
                else if (HeldDef != null && !HeldLit && !HeldDef.Timer && !HeldDef.Remote && Ignition.Hold(dt, Running)) LightHeld(game);
            }
            if (!In.LighterHeld) Ignition.Release();
        }

        void LightHeld(GameController game)
        {
            if (game.Pocket.Take() == null) return;
            HeldLit = true;
            HeldTotal = BoomYard.Core.Fuse.TotalBurn(HeldDef);
            if (HeldTotal <= 0f) HeldTotal = 8f;   // a molotov rag burns until it is thrown
            HeldRemaining = HeldTotal;
            Hands.SetLit(true);
            game.OnIgnite(HeldDef);
        }

        void Fuse(GameController game, float dt)
        {
            if (!HeldLit) return;
            HeldRemaining -= dt;
            if (HeldRemaining > 0f) return;
            // Too late: it goes off in the hand (spec §04). White screen, 3 s of shaking hands, the item is gone.
            var live = LiveExplosive.Spawn(HeldDef, Hands.ItemWorld, Quaternion.identity, 0f, Placement.Thrown);
            DropHeld();
            game.OnHandBurn();
            live.Detonate();
        }

        void DropHeld()
        {
            HeldLit = false;
            HeldDef = null;
            Hands.SetItem(null);
            Hands.SetLit(false);
            Ignition.NextFuse();
        }

        /// <summary>Throw (lit or not: an unlit throw just puts it somewhere to light later) and place.</summary>
        void ThrowAndPlace(GameController game)
        {
            if (In.ThrowUp && HeldDef != null)
            {
                if (!HeldDef.Throwable) PlaceAt(GroundAhead(), null, game);
                else Throw(In.ThrowPower, game);
            }
            if (In.Place && HeldDef != null)
            {
                RaycastHit hit;
                var ray = Cam.ScreenPointToRay(In.PlaceScreen);
                if (Physics.Raycast(ray, out hit, PlaceReach, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    PlaceAt(hit.point + hit.normal * 0.03f, hit.collider.GetComponentInParent<Destructible>(), game);
                else PlaceAt(GroundAhead(), null, game);
            }
        }

        /// <summary>The ground just in front of the player's feet.</summary>
        Vector3 GroundAhead()
        {
            var p = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
            RaycastHit hit;
            if (Physics.Raycast(p, Vector3.down, out hit, 2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point + Vector3.up * 0.03f;
            return new Vector3(p.x, 0.05f, p.z);
        }

        Vector3 ThrowVelocity(float power)
        {
            var dir = Quaternion.AngleAxis(-8f, Cam.transform.right) * Cam.transform.forward;
            return dir * Mathf.Lerp(MinThrow, MaxThrow, Mathf.Clamp01(power));
        }

        void Throw(float power, GameController game)
        {
            LiveExplosive live;
            if (HeldLit) live = LiveExplosive.Spawn(HeldDef, Hands.ItemWorld, Cam.transform.rotation, HeldRemaining, Placement.Thrown);
            else
            {
                if (game.Pocket.Take() == null) return;
                live = LiveExplosive.SpawnUnlit(HeldDef, Hands.ItemWorld, Cam.transform.rotation, Placement.Placed);
            }
            var rb = live.GetComponent<Rigidbody>();
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = ThrowVelocity(power) + cc.velocity * 0.5f;
#else
            rb.velocity = ThrowVelocity(power) + cc.velocity * 0.5f;
#endif
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            live.Airborne = true;
            if (HeldLit && HeldDef.Id == "rocket")
            {
                // A lit rocket flies where you aim it, under power.
                rb.angularVelocity = Vector3.zero;
                live.Motor = live.gameObject.AddComponent<RocketMotor>();
                live.Motor.Launch(live, RocketMotor.Kind.Missile, ThrowVelocity(1f).normalized);
            }
            Hands.Throw();
            bool lit = HeldLit;
            DropHeld();
            if (lit) game.OnRelease(live);
        }

        /// <summary>
        /// Sets the item down. In or under something with room (a bin, a mailbox, under a car) it is contained:
        /// ×1.8 (spec §04, §07).
        /// </summary>
        void PlaceAt(Vector3 point, Destructible target, GameController game)
        {
            Destructible container = null;
            if (target != null && !target.Destroyed && (target.Def.Reaction & Reaction.Contain) != 0) container = target;
            else
            {
                RaycastHit up;
                if (Physics.Raycast(point + Vector3.up * 0.02f, Vector3.up, out up, 1.4f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    var over = up.collider.GetComponentInParent<Destructible>();
                    if (over != null && !over.Destroyed && (over.Def.Reaction & Reaction.Contain) != 0) container = over;
                }
            }
            var placement = container != null ? Placement.Contained : Placement.Placed;
            bool under = container != null && target == container && container.Def.Mass >= 300f;
            if (under)
            {
                // Tapping a car slides the item underneath it, toward the fuel tank.
                var tank = container.transform.TransformPoint(container.FirePoint);
                point = Vector3.Lerp(point, tank, 0.5f);
                point.y = 0.08f;
            }
            LiveExplosive live;
            bool timer = !HeldLit && HeldDef.Timer, remote = !HeldLit && HeldDef.Remote;
            if (HeldLit) live = LiveExplosive.Spawn(HeldDef, point, Quaternion.Euler(0f, yaw, 0f), HeldRemaining, placement);
            else
            {
                if (game.Pocket.Take() == null) return;
                if (timer) live = LiveExplosive.Spawn(HeldDef, point, Quaternion.Euler(0f, yaw, 0f), game.TimerSeconds, placement);
                else live = LiveExplosive.SpawnUnlit(HeldDef, point, Quaternion.Euler(0f, yaw, 0f), placement);
            }
            live.Container = container;
            if (container != null && target == container && !under)
            {
                // Pushed inside: it rides with the container.
                var rb = live.GetComponent<Rigidbody>();
                rb.isKinematic = true;
                foreach (var c in live.GetComponents<Collider>()) c.enabled = false;
                live.transform.SetParent(container.transform, true);
                live.transform.position = container.transform.TransformPoint(container.FirePoint);
            }
            Hands.Throw();
            bool wasLit = HeldLit;
            DropHeld();
            if (wasLit) game.OnRelease(live);
            else if (timer) game.OnIgnite(live.Def);
            else game.OnPlaced();
            if (timer) game.UI.Toast(Loc.F("toast.timer", Mathf.RoundToInt(game.TimerSeconds)));
            else if (remote) game.Toast("toast.remote");
            else game.Toast(container != null ? "toast.contained" : wasLit ? "toast.runaway" : "toast.placed");
        }

        /// <summary>The throw arc and the danger ring where it would land (spec §05).</summary>
        void Predict(bool show)
        {
            arc.enabled = ring.enabled = show && HeldDef != null && HeldDef.Throwable && !Knocked;
            if (HeldDef == null) { PredictedFlight = 0f; return; }
            Vector3 p = Hands.ItemWorld, v = ThrowVelocity(In.ThrowHeld ? In.ThrowPower : 0.35f);
            const float step = 0.04f;
            int count = 0;
            float t = 0f;
            Vector3 landing = p;
            bool landed = false;
            for (; count < arcPoints.Length; count++)
            {
                arcPoints[count] = p;
                var next = p + v * step;
                v += Physics.gravity * step;
                RaycastHit hit;
                if (Physics.Linecast(p, next, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    landing = hit.point;
                    t += step * hit.distance / Mathf.Max(0.001f, Vector3.Distance(p, next));
                    count++;
                    if (count < arcPoints.Length) arcPoints[count++] = landing;
                    landed = true;
                    break;
                }
                p = next;
                t += step;
            }
            if (!landed) landing = p;
            PredictedFlight = t;
            if (!arc.enabled) return;
            arc.positionCount = Mathf.Min(count, arcPoints.Length);
            arc.SetPositions(arcPoints);
            float r = Mathf.Max(0.4f, HeldDef.Radius);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float a = i / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(i, landing + new Vector3(Mathf.Cos(a) * r, 0.04f, Mathf.Sin(a) * r));
            }
        }

        /// <summary>A blast somewhere in the yard: shake by distance; knocked down close in, a close call just outside.</summary>
        public void OnBlast(Vector3 pos, float radius, float damage, bool playersOwn)
        {
            float d = Vector3.Distance(pos, transform.position + Vector3.up * 1.1f);
            shake = Mathf.Min(1.5f, shake + damage / (220f * (1f + 0.06f * d * d)));
            // How hard it hits the camera: by size and closeness.
            float k = Mathf.Clamp01(Mathf.Sqrt(damage / 500f)) * Mathf.Clamp01(1f - d / (radius * 10f + 8f));
            if (Rendering.I != null) Rendering.I.Kick(k);
            fovKick = Mathf.Max(fovKick, 9f * k);
            var game = GameController.I;
            if (game != null && k > 0.35f && radius >= 2f) game.HitStop(0.05f + 0.05f * k);
            if (k > 0.3f) Haptics.Buzz();
            if (game == null || !game.Round.CanAct && game.Round.State != RoundState.Stunned) return;
            if (d < radius * Blast.KnockdownShare)
            {
                KnockDown(RoundModel.StunSeconds);
                game.OnKnockdown();
            }
            else if (d < radius * 1.3f && radius >= 1.5f && !Knocked) game.OnCloseCall();
        }
    }
}
