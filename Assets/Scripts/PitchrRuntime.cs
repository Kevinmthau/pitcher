using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Board.Core;
using Board.Input;

using UnityEngine;
using UnityEngine.UI;

namespace Pitchr
{
    public class PitchrRuntime : MonoBehaviour
    {
        private const int OrangeRobotGlyphId = 2;
        private const int PinkRobotGlyphId = 3;
        private const float MatchDurationSeconds = 60f;
        private static readonly float[] s_TrendSwapThresholds = { 40f, 20f };
        private static readonly Vector2 s_IntroHitPadding = new Vector2(36f, 36f);
        private static readonly Vector2 s_PitchHitPadding = new Vector2(34f, 22f);
        private static readonly Vector2 s_PadHitPadding = new Vector2(42f, 26f);
        private static readonly Vector2 s_ResultHitPadding = new Vector2(42f, 30f);

        private static readonly Color s_Background = Hex("#131723");
        private static readonly Color s_Banner = Hex("#1B2130");
        private static readonly Color s_Card = Hex("#F6EEDB");
        private static readonly Color s_CardInk = Hex("#1B1F2E");
        private static readonly Color s_Phone = Hex("#202A3F");
        private static readonly Color s_PhoneGlow = Hex("#2E3B59");
        private static readonly Color s_Hot = Hex("#FF9F5A");
        private static readonly Color s_Not = Hex("#7DB2FF");
        private static readonly Color s_Approve = Hex("#46C975");
        private static readonly Color s_Reject = Hex("#EB5E68");
        private static readonly Color s_OrangeRobot = Hex("#FF9B43");
        private static readonly Color s_PinkRobot = Hex("#FF67B0");
        private static readonly Color s_Cream = Hex("#FFF8E8");
        private static readonly Color s_Slate = Hex("#8593AA");
        private static readonly Color s_Winner = Hex("#FFD563");
        private static readonly Vector2 s_ReferenceResolution = new Vector2(1920f, 1080f);

        private readonly System.Random m_Random = new System.Random();
        private readonly Dictionary<int, ContactTrace> m_ContactTraces = new Dictionary<int, ContactTrace>();
        private readonly HashSet<int> m_SeenContacts = new HashSet<int>();
        private readonly List<int> m_ExpiredContacts = new List<int>();

        private Font m_Font;
        private Canvas m_Canvas;
        private RectTransform m_IntroGroup;
        private RectTransform m_GameplayGroup;
        private RectTransform m_ResultsGroup;
        private RectTransform m_PlayAgainZone;
        private RectTransform m_ExitZone;
        private Text m_HeaderTitle;
        private Text m_HeaderSubtitle;
        private Text m_WinnerBanner;
        private Text m_ResultsPrompt;

        private LaneState[] m_Lanes;
        private List<PitchTemplate> m_PitchLibrary;
        private List<TrendPack> m_TrendPacks;

        private GameState m_State;
        private float m_RemainingTime;
        private bool m_TimerExpired;
        private bool m_StartQueued;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BoardApplication.SetPauseScreenContext(applicationName: "Pitchr");

            m_Font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            BuildStaticData();
            BuildUi();
            ResetToIntro();
            ConfigureCamera();
        }

        private void Update()
        {
            if (m_State == GameState.Playing && !m_TimerExpired)
            {
                TickTimer(Time.deltaTime);
            }

            ProcessStamperContacts();
        }

        private void ConfigureCamera()
        {
            if (Camera.main == null)
            {
                return;
            }

            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = s_Background;
            Camera.main.orthographic = true;
        }

        private void BuildStaticData()
        {
            m_TrendPacks = new List<TrendPack>
            {
                new TrendPack(
                    new[] { "Time Travel", "Robots", "Heists" },
                    new[] { "Reboots", "Zombies", "Found Footage" }),
                new TrendPack(
                    new[] { "Kaiju", "Road Trips", "Cooking" },
                    new[] { "Biopics", "Multiverse", "Vampires" }),
                new TrendPack(
                    new[] { "Mermaids", "Wedding Chaos", "Revenge" },
                    new[] { "Dream Logic", "AI Romance", "Slow Cinema" }),
                new TrendPack(
                    new[] { "Ghosts", "Space Opera", "Reality TV" },
                    new[] { "Origin Stories", "Courtroom Drama", "Talking Animals" }),
            };

            m_PitchLibrary = new List<PitchTemplate>
            {
                new PitchTemplate("Timeshark", "A surfer must punch a shark through different decades before history collapses.", "Time Travel", "Sharks", "Action"),
                new PitchTemplate("Scrap Hearts", "Two junkyard robots fake a wedding to save their repair shop.", "Robots", "Wedding Chaos", "Romance"),
                new PitchTemplate("Jewel Run 9000", "A crew of washed-up magicians attempts one final casino heist in orbit.", "Heists", "Space Opera", "Comedy"),
                new PitchTemplate("Camp Blood Again", "A legacy slasher gets rebooted inside an abandoned summer camp.", "Reboots", "Zombies", "Horror"),
                new PitchTemplate("Stove Gods", "Celebrity siblings battle for culinary immortality on a floating food truck.", "Cooking", "Family", "Comedy"),
                new PitchTemplate("Kaiju on Route 9", "A busload of tourists road-trips through a state already claimed by giant monsters.", "Kaiju", "Road Trips", "Comedy"),
                new PitchTemplate("Bride Hard", "A stuntwoman maid of honor has to rescue a wedding from a crew of art thieves.", "Wedding Chaos", "Heists", "Action"),
                new PitchTemplate("Tide & Vengeance", "A betrayed mermaid returns to shore to ruin the prince who stole her voice.", "Mermaids", "Revenge", "Thriller"),
                new PitchTemplate("The Last Byte of Love", "A lonely coder falls for a sentient customer-service chatbot.", "AI Romance", "Slow Cinema", "Drama"),
                new PitchTemplate("Crown of Teeth", "A vampire prince launches a luxury nightclub in a strip mall.", "Vampires", "Comedy", "Action"),
                new PitchTemplate("Origin of the Originer", "A superhero discovers he was only invented to explain another hero's backstory.", "Origin Stories", "Multiverse", "Superheroes"),
                new PitchTemplate("Spectral Ratings", "Ghost hunters become reality-TV stars while accidentally haunting themselves.", "Ghosts", "Reality TV", "Comedy"),
                new PitchTemplate("Neptune's Kitchen", "A washed-up chef reopens his restaurant under the sea with help from a mermaid band.", "Mermaids", "Cooking", "Family"),
                new PitchTemplate("The Great Small-Town Heist", "A marching band robs a bank during a cross-country parade circuit.", "Heists", "Road Trips", "Heartland"),
                new PitchTemplate("Dream Court", "A defense lawyer must argue a murder case that only exists in recurring dreams.", "Dream Logic", "Courtroom Drama", "Thriller"),
                new PitchTemplate("Robo-Romcom Reunion", "A pair of ex-lovers discover their couples retreat is secretly run by robots.", "Robots", "Reboots", "Romance"),
                new PitchTemplate("Gigantico", "A grieving mascot becomes the reluctant handler for an emotional support kaiju.", "Kaiju", "Origin Stories", "Action"),
                new PitchTemplate("Haunted Honeymoon Hotline", "Newlyweds launch a ghost-busting advice show from a cursed beach hotel.", "Ghosts", "Wedding Chaos", "Comedy"),
                new PitchTemplate("Space Trial X", "A disgraced star pilot must defend Earth in an intergalactic courtroom.", "Space Opera", "Courtroom Drama", "Thriller"),
                new PitchTemplate("Farmhouse VHS", "Teen documentarians uncover a cult by filming every creak in a dying farmhouse.", "Found Footage", "Slow Cinema", "Horror"),
                new PitchTemplate("Bite Club", "A team of vampires moonlights as elite getaway drivers after sunset.", "Vampires", "Heists", "Action"),
                new PitchTemplate("Revenge Buffet", "A celebrity chef serves payback one tasting menu at a time.", "Revenge", "Cooking", "Comedy"),
                new PitchTemplate("Broadcast From Beyond", "A washed-up host revives his career by interviewing ghosts on live television.", "Ghosts", "Reality TV", "Drama"),
                new PitchTemplate("Talking Animal Court", "A courtroom ace must defend a foul-mouthed alpaca accused of insider trading.", "Talking Animals", "Courtroom Drama", "Comedy"),
            };
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("PitchrCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            m_Canvas = canvasObject.GetComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            DontDestroyOnLoad(canvasObject);

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = s_ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            CreatePanel(canvasRect, "Backdrop", Vector2.zero, s_ReferenceResolution, s_Background);
            CreateBand(canvasRect, "HeaderBand", new Vector2(0f, 460f), new Vector2(1920f, 180f), s_Banner);
            CreateBand(canvasRect, "FooterBand", new Vector2(0f, -470f), new Vector2(1920f, 140f), Hex("#0E121B"));

            m_HeaderTitle = CreateText(canvasRect, "HeaderTitle", "PITCHR", 72, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 490f), new Vector2(820f, 82f));
            m_HeaderSubtitle = CreateText(
                canvasRect,
                "HeaderSubtitle",
                "Pink Robot stamps movie pitches. Dip in ink, then make the call.",
                24,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                s_Slate,
                new Vector2(0f, 438f),
                new Vector2(1100f, 40f));

            m_IntroGroup = CreateGroup(canvasRect, "Intro");
            m_GameplayGroup = CreateGroup(canvasRect, "Gameplay");
            m_ResultsGroup = CreateGroup(canvasRect, "Results");

            BuildIntro(m_IntroGroup);
            BuildGameplay(m_GameplayGroup);
            BuildResults(m_ResultsGroup);
        }

        private void BuildIntro(RectTransform parent)
        {
            CreateText(parent, "IntroTitle", "STAMP BOTH START CARDS TO OPEN THE PITCH MEETING", 32, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 260f), new Vector2(1120f, 50f));
            CreateText(parent, "IntroBody", "Use the Pink Robot stamper. Gameplay is simultaneous: dip in green to approve, dip in red to pass, then stamp the pitch card.", 24, FontStyle.Normal, TextAnchor.MiddleCenter, s_Slate, new Vector2(0f, 204f), new Vector2(1240f, 80f));

            m_Lanes = new[]
            {
                new LaneState(LaneId.Left, "LEFT STUDIO", OrangeRobotGlyphId, "ORANGE ROBOT", s_OrangeRobot),
                new LaneState(LaneId.Right, "RIGHT STUDIO", PinkRobotGlyphId, "PINK ROBOT", s_PinkRobot),
            };

            BuildIntroLane(parent, m_Lanes[0], -470f);
            BuildIntroLane(parent, m_Lanes[1], 470f);
        }

        private void BuildIntroLane(RectTransform parent, LaneState lane, float x)
        {
            lane.IntroCard = CreatePanel(parent, $"{lane.Label}IntroCard", new Vector2(x, -18f), new Vector2(520f, 320f), Hex("#212B40"));
            AddOutline(lane.IntroCard.gameObject, lane.id == LaneId.Left ? s_Hot : s_Not);

            CreateText(lane.IntroCard, "LaneLabel", lane.Label, 20, FontStyle.Bold, TextAnchor.UpperCenter, s_Winner, new Vector2(0f, 118f), new Vector2(420f, 26f));
            CreateText(lane.IntroCard, "RobotLabel", $"STAMPER: {lane.StamperName}", 28, FontStyle.Bold, TextAnchor.MiddleCenter, lane.StamperColor, new Vector2(0f, 44f), new Vector2(420f, 40f));
            CreateText(lane.IntroCard, "IntroAction", "Stamp this card to lock in.\nBoth studios must be ready.", 24, FontStyle.Normal, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, -38f), new Vector2(420f, 100f));

            lane.IntroStamp = CreateText(lane.IntroCard, "ReadyStamp", string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleCenter, s_PinkRobot, Vector2.zero, new Vector2(300f, 80f));
            lane.IntroStamp.gameObject.SetActive(false);
        }

        private void BuildGameplay(RectTransform parent)
        {
            CreateText(parent, "GameplayPrompt", "DIP THE ROBOT IN INK, THEN STAMP THE PITCH", 28, FontStyle.Bold, TextAnchor.MiddleCenter, s_Winner, new Vector2(0f, 290f), new Vector2(1200f, 40f));

            BuildGameplayLane(parent, m_Lanes[0], -470f);
            BuildGameplayLane(parent, m_Lanes[1], 470f);
        }

        private void BuildGameplayLane(RectTransform parent, LaneState lane, float x)
        {
            lane.Root = CreateContainer(parent, $"{lane.Label}Root", new Vector2(x, -18f));

            lane.PhonePanel = CreatePanel(lane.Root, "Phone", new Vector2(0f, 360f), new Vector2(360f, 180f), s_Phone);
            AddOutline(lane.PhonePanel.gameObject, s_PhoneGlow);
            CreateText(lane.PhonePanel, "PhoneLabel", lane.Label, 18, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 70f), new Vector2(240f, 24f));
            CreateText(lane.PhonePanel, "RobotBadge", lane.StamperName, 18, FontStyle.Bold, TextAnchor.MiddleCenter, lane.StamperColor, new Vector2(0f, 46f), new Vector2(240f, 24f));
            lane.TimerText = CreateText(lane.PhonePanel, "Timer", "1:00", 44, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 28f), new Vector2(240f, 50f));
            lane.ProfitText = CreateText(lane.PhonePanel, "Profit", "NET $0.0M", 22, FontStyle.Bold, TextAnchor.MiddleCenter, s_Winner, new Vector2(0f, -12f), new Vector2(280f, 30f));
            lane.NotificationHeadline = CreateText(lane.PhonePanel, "Headline", "Waiting for the first big release...", 18, FontStyle.Bold, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, -54f), new Vector2(300f, 32f));
            lane.NotificationBody = CreateText(lane.PhonePanel, "Body", "Approvals will send box-office notes here.", 16, FontStyle.Normal, TextAnchor.UpperLeft, s_Slate, new Vector2(0f, -92f), new Vector2(300f, 58f));

            lane.HotPanel = CreatePanel(lane.Root, "HotPanel", new Vector2(-128f, 184f), new Vector2(240f, 120f), Hex("#3A2A21"));
            AddOutline(lane.HotPanel.gameObject, s_Hot);
            lane.HotText = CreateText(lane.HotPanel, "HotText", string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, 0f), new Vector2(188f, 92f));

            lane.NotPanel = CreatePanel(lane.Root, "NotPanel", new Vector2(128f, 184f), new Vector2(240f, 120f), Hex("#1C2740"));
            AddOutline(lane.NotPanel.gameObject, s_Not);
            lane.NotText = CreateText(lane.NotPanel, "NotText", string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, 0f), new Vector2(188f, 92f));

            lane.PitchCard = CreatePanel(lane.Root, "PitchCard", new Vector2(0f, -12f), new Vector2(520f, 360f), s_Card);
            AddOutline(lane.PitchCard.gameObject, s_Cream);
            lane.PitchCanvasGroup = lane.PitchCard.gameObject.AddComponent<CanvasGroup>();
            lane.PitchTitle = CreateText(lane.PitchCard, "Title", "Pitch Title", 34, FontStyle.Bold, TextAnchor.UpperCenter, s_CardInk, new Vector2(0f, 108f), new Vector2(400f, 40f));
            lane.PitchTags = CreateText(lane.PitchCard, "Tags", "TAGS", 20, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 66f), new Vector2(400f, 26f));
            lane.PitchLogline = CreateText(lane.PitchCard, "Logline", "Pitch logline goes here.", 24, FontStyle.Normal, TextAnchor.UpperLeft, s_CardInk, new Vector2(0f, 6f), new Vector2(392f, 126f));
            lane.ActionText = CreateText(lane.PitchCard, "Action", string.Empty, 20, FontStyle.Bold, TextAnchor.LowerCenter, s_CardInk, new Vector2(0f, -112f), new Vector2(380f, 54f));
            lane.StampText = CreateText(lane.PitchCard, "Stamp", string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleCenter, s_Approve, Vector2.zero, new Vector2(320f, 80f));
            lane.StampText.gameObject.SetActive(false);

            lane.RejectPad = CreatePanel(lane.Root, "RejectPad", new Vector2(-128f, -328f), new Vector2(230f, 165f), Hex("#421C27"));
            lane.RejectPadImage = lane.RejectPad.GetComponent<Image>();
            AddOutline(lane.RejectPad.gameObject, s_Reject);
            CreateText(lane.RejectPad, "RejectLabel", "RED INK", 28, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 16f), new Vector2(160f, 32f));
            CreateText(lane.RejectPad, "RejectHint", "PASS", 20, FontStyle.Bold, TextAnchor.MiddleCenter, s_Reject, new Vector2(0f, -22f), new Vector2(160f, 24f));

            lane.ApprovePad = CreatePanel(lane.Root, "ApprovePad", new Vector2(128f, -328f), new Vector2(230f, 165f), Hex("#173123"));
            lane.ApprovePadImage = lane.ApprovePad.GetComponent<Image>();
            AddOutline(lane.ApprovePad.gameObject, s_Approve);
            CreateText(lane.ApprovePad, "ApproveLabel", "GREEN INK", 28, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 16f), new Vector2(160f, 32f));
            CreateText(lane.ApprovePad, "ApproveHint", "GREENLIGHT", 20, FontStyle.Bold, TextAnchor.MiddleCenter, s_Approve, new Vector2(0f, -22f), new Vector2(160f, 24f));
        }

        private void BuildResults(RectTransform parent)
        {
            m_WinnerBanner = CreateText(parent, "WinnerBanner", string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleCenter, s_Winner, new Vector2(0f, 280f), new Vector2(1200f, 54f));
            m_ResultsPrompt = CreateText(parent, "ResultsPrompt", "Stamp PLAY AGAIN to rerun the pitch meeting, or EXIT to close the vignette.", 24, FontStyle.Normal, TextAnchor.MiddleCenter, s_Slate, new Vector2(0f, 236f), new Vector2(1360f, 40f));

            BuildResultsLane(parent, m_Lanes[0], -470f);
            BuildResultsLane(parent, m_Lanes[1], 470f);

            m_PlayAgainZone = CreatePanel(parent, "PlayAgainZone", new Vector2(-180f, -320f), new Vector2(320f, 150f), Hex("#163525"));
            AddOutline(m_PlayAgainZone.gameObject, s_Approve);
            CreateText(m_PlayAgainZone, "PlayAgain", "PLAY AGAIN", 34, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 14f), new Vector2(220f, 44f));
            CreateText(m_PlayAgainZone, "PlayAgainHint", "Stamp with the Pink Robot", 20, FontStyle.Normal, TextAnchor.MiddleCenter, s_Approve, new Vector2(0f, -26f), new Vector2(260f, 28f));

            m_ExitZone = CreatePanel(parent, "ExitZone", new Vector2(180f, -320f), new Vector2(320f, 150f), Hex("#381A22"));
            AddOutline(m_ExitZone.gameObject, s_Reject);
            CreateText(m_ExitZone, "Exit", "EXIT", 34, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 14f), new Vector2(220f, 44f));
            CreateText(m_ExitZone, "ExitHint", "Stamp to close the app", 20, FontStyle.Normal, TextAnchor.MiddleCenter, s_Reject, new Vector2(0f, -26f), new Vector2(220f, 28f));
        }

        private void BuildResultsLane(RectTransform parent, LaneState lane, float x)
        {
            lane.ResultsCard = CreatePanel(parent, $"{lane.Label}ResultsCard", new Vector2(x, -20f), new Vector2(520f, 430f), Hex("#212B40"));
            AddOutline(lane.ResultsCard.gameObject, lane.id == LaneId.Left ? s_Hot : s_Not);
            lane.ResultsText = CreateText(lane.ResultsCard, "ResultsText", string.Empty, 23, FontStyle.Normal, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, 0f), new Vector2(420f, 330f));
        }

        private void ResetToIntro()
        {
            m_State = GameState.Intro;
            m_TimerExpired = false;
            m_StartQueued = false;
            m_RemainingTime = MatchDurationSeconds;
            m_ContactTraces.Clear();

            SetGroupVisibility(m_IntroGroup, true);
            SetGroupVisibility(m_GameplayGroup, false);
            SetGroupVisibility(m_ResultsGroup, false);

            m_HeaderSubtitle.text = "Left studio uses the Orange Robot. Right studio uses the Pink Robot.";

            foreach (var lane in m_Lanes)
            {
                lane.ResetForIntro();
                lane.IntroStamp.text = "READY";
                lane.IntroStamp.gameObject.SetActive(false);
            }
        }

        private void StartMatch()
        {
            m_State = GameState.Playing;
            m_RemainingTime = MatchDurationSeconds;
            m_TimerExpired = false;
            m_StartQueued = false;
            m_ContactTraces.Clear();

            SetGroupVisibility(m_IntroGroup, false);
            SetGroupVisibility(m_GameplayGroup, true);
            SetGroupVisibility(m_ResultsGroup, false);
            m_HeaderSubtitle.text = "Orange Robot runs left. Pink Robot runs right. Dip in ink, then stamp the pitch.";

            foreach (var lane in m_Lanes)
            {
                lane.ResetForMatch(ShuffleDeck());
                ApplyTrendText(lane);
                AdvancePitch(lane, isInitialPitch: true);
                UpdateTimerAndScore(lane);
                UpdateInkPresentation(lane);
            }
        }

        private void ShowResults()
        {
            m_State = GameState.Results;

            SetGroupVisibility(m_IntroGroup, false);
            SetGroupVisibility(m_GameplayGroup, false);
            SetGroupVisibility(m_ResultsGroup, true);
            m_HeaderSubtitle.text = "The money is counted. The critics have spoken.";

            var left = m_Lanes[0];
            var right = m_Lanes[1];

            if (Mathf.Approximately(left.TotalProfitMillions, right.TotalProfitMillions))
            {
                m_WinnerBanner.text = $"DEAD HEAT  |  {FormatMoney(left.TotalProfitMillions)} EACH";
            }
            else
            {
                var winner = left.TotalProfitMillions > right.TotalProfitMillions ? left : right;
                m_WinnerBanner.text = $"{winner.Label} WINS  |  {FormatMoney(winner.TotalProfitMillions)}";
            }

            foreach (var lane in m_Lanes)
            {
                lane.ResultsText.text = BuildResultsSummary(lane);
            }
        }

        private void TickTimer(float deltaTime)
        {
            m_RemainingTime = Mathf.Max(0f, m_RemainingTime - deltaTime);

            foreach (var lane in m_Lanes)
            {
                while (lane.NextTrendThresholdIndex < s_TrendSwapThresholds.Length &&
                       m_RemainingTime <= s_TrendSwapThresholds[lane.NextTrendThresholdIndex])
                {
                    lane.PendingTrendSwaps++;
                    lane.NextTrendThresholdIndex++;
                }

                UpdateTimerAndScore(lane);
            }

            if (m_RemainingTime <= 0f && !m_TimerExpired)
            {
                m_TimerExpired = true;
                TryCompleteMatch();
            }
        }

        private void ProcessStamperContacts()
        {
            m_SeenContacts.Clear();

            var contacts = BoardInput.GetActiveContacts(BoardContactType.Glyph);
            for (var i = 0; i < contacts.Length; i++)
            {
                var contact = contacts[i];
                if (!IsConfiguredStamperGlyph(contact.glyphId))
                {
                    continue;
                }

                var unityScreenPoint = ToUnityScreenPoint(contact.screenPosition);
                var zone = HitTestZone(unityScreenPoint);

                m_SeenContacts.Add(contact.contactId);

                ContactTrace previous;
                m_ContactTraces.TryGetValue(contact.contactId, out previous);

                if (zone.kind != previous.kind || zone.lane != previous.lane)
                {
                    HandleZoneEntry(zone, contact, unityScreenPoint);
                }

                m_ContactTraces[contact.contactId] = new ContactTrace(zone.kind, zone.lane);
            }

            m_ExpiredContacts.Clear();
            foreach (var contactId in m_ContactTraces.Keys)
            {
                if (!m_SeenContacts.Contains(contactId))
                {
                    m_ExpiredContacts.Add(contactId);
                }
            }

            for (var i = 0; i < m_ExpiredContacts.Count; i++)
            {
                m_ContactTraces.Remove(m_ExpiredContacts[i]);
            }
        }

        private void HandleZoneEntry(ZoneHit zone, BoardContact contact, Vector2 unityScreenPoint)
        {
            if (!CanStampZone(contact, zone))
            {
                return;
            }

            switch (m_State)
            {
                case GameState.Intro:
                    if (zone.kind == ZoneKind.IntroCard && zone.lane != null && !zone.lane.IntroReady)
                    {
                        StampStaticCard(zone.lane.IntroCard, zone.lane.IntroStamp, "READY", s_PinkRobot, unityScreenPoint, contact.orientation);
                        zone.lane.IntroReady = true;

                        if (!m_StartQueued && m_Lanes[0].IntroReady && m_Lanes[1].IntroReady)
                        {
                            m_StartQueued = true;
                            StartCoroutine(BeginMatchAfterDelay());
                        }
                    }

                    break;

                case GameState.Playing:
                    if (m_TimerExpired || zone.lane == null || zone.lane.IsResolving)
                    {
                        return;
                    }

                    switch (zone.kind)
                    {
                        case ZoneKind.ApprovePad:
                            zone.lane.LoadedInk = InkColor.Approve;
                            UpdateInkPresentation(zone.lane);
                            break;
                        case ZoneKind.RejectPad:
                            zone.lane.LoadedInk = InkColor.Reject;
                            UpdateInkPresentation(zone.lane);
                            break;
                        case ZoneKind.PitchCard:
                            if (zone.lane.LoadedInk != InkColor.None)
                            {
                                StartCoroutine(ResolvePitch(zone.lane, contact, unityScreenPoint));
                            }

                            break;
                    }

                    break;

                case GameState.Results:
                    if (zone.kind == ZoneKind.PlayAgain)
                    {
                        StartMatch();
                    }
                    else if (zone.kind == ZoneKind.Exit)
                    {
                        BoardApplication.Exit();
                    }

                    break;
            }
        }

        private IEnumerator BeginMatchAfterDelay()
        {
            yield return new WaitForSeconds(0.4f);
            StartMatch();
        }

        private IEnumerator ResolvePitch(LaneState lane, BoardContact contact, Vector2 unityScreenPoint)
        {
            if (lane.IsResolving || lane.CurrentPitch == null)
            {
                yield break;
            }

            lane.IsResolving = true;

            var ink = lane.LoadedInk;
            lane.LoadedInk = InkColor.None;
            UpdateInkPresentation(lane);

            var approval = ink == InkColor.Approve;
            StampStaticCard(
                lane.PitchCard,
                lane.StampText,
                approval ? "GREENLIT" : "PASSED",
                approval ? s_Approve : s_Reject,
                unityScreenPoint,
                contact.orientation);

            yield return new WaitForSeconds(0.18f);

            if (approval)
            {
                var outcome = EvaluatePitch(lane.CurrentPitch, m_TrendPacks[lane.CurrentTrendIndex]);
                lane.ApprovedOutcomes.Add(outcome);
                lane.TotalProfitMillions += outcome.BoxOfficeMillions;
                lane.NotificationHeadline.text = outcome.Headline;
                lane.NotificationBody.text = $"{outcome.Body}\n{FormatMoney(outcome.BoxOfficeMillions)}  |  {outcome.Stars:0.0}/5";
            }
            else
            {
                lane.NotificationHeadline.text = $"{lane.CurrentPitch.Title} heads back to the slush pile.";
                lane.NotificationBody.text = "No release. No risk. Next pitch up.";
            }

            UpdateTimerAndScore(lane);

            yield return SlidePitchCard(lane.PitchCard, lane.PitchCanvasGroup, approval ? 540f : -540f);

            lane.StampText.gameObject.SetActive(false);
            lane.PitchCard.anchoredPosition = Vector2.zero;
            lane.PitchCanvasGroup.alpha = 1f;

            if (!m_TimerExpired)
            {
                AdvancePitch(lane, isInitialPitch: false);
                UpdateInkPresentation(lane);
            }

            lane.IsResolving = false;
            TryCompleteMatch();
        }

        private IEnumerator SlidePitchCard(RectTransform card, CanvasGroup canvasGroup, float xOffset)
        {
            const float duration = 0.22f;
            var elapsed = 0f;
            var start = Vector2.zero;
            var target = new Vector2(xOffset, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                card.anchoredPosition = Vector2.Lerp(start, target, eased);
                canvasGroup.alpha = Mathf.Lerp(1f, 0.15f, eased);
                yield return null;
            }
        }

        private void AdvancePitch(LaneState lane, bool isInitialPitch)
        {
            if (!isInitialPitch && lane.PendingTrendSwaps > 0)
            {
                lane.CurrentTrendIndex = (lane.CurrentTrendIndex + 1) % m_TrendPacks.Count;
                lane.PendingTrendSwaps--;
            }

            lane.CurrentPitch = DrawPitch(lane);
            ApplyTrendText(lane);
            ApplyPitchText(lane);
        }

        private PitchTemplate DrawPitch(LaneState lane)
        {
            if (lane.PitchDeck.Count == 0)
            {
                lane.PitchDeck = ShuffleDeck();
            }

            var pitch = lane.PitchDeck[0];
            lane.PitchDeck.RemoveAt(0);
            return pitch;
        }

        private List<PitchTemplate> ShuffleDeck()
        {
            var deck = new List<PitchTemplate>(m_PitchLibrary);
            for (var i = deck.Count - 1; i > 0; i--)
            {
                var j = m_Random.Next(i + 1);
                var swap = deck[i];
                deck[i] = deck[j];
                deck[j] = swap;
            }

            return deck;
        }

        private PitchOutcome EvaluatePitch(PitchTemplate pitch, TrendPack trendPack)
        {
            var hotMatches = FindMatches(pitch.Tags, trendPack.Hots);
            var notMatches = FindMatches(pitch.Tags, trendPack.Nots);

            var hotCount = hotMatches.Count;
            var notCount = notMatches.Count;

            var swing = RandomRange(-0.8f, 0.8f);
            var score = (hotCount * 1.35f) - (notCount * 1.45f) + swing;

            OutcomeTone tone;
            if (score >= 0.85f)
            {
                tone = OutcomeTone.Success;
            }
            else if (score <= -0.7f)
            {
                tone = OutcomeTone.Failure;
            }
            else
            {
                tone = OutcomeTone.Meh;
            }

            float boxOfficeMillions;
            switch (tone)
            {
                case OutcomeTone.Success:
                    boxOfficeMillions = 26f + (hotCount * 28f) + Mathf.Max(0f, score) * 32f + RandomRange(0f, 58f);
                    break;
                case OutcomeTone.Failure:
                    boxOfficeMillions = -(18f + (notCount * 26f) + Mathf.Abs(Mathf.Min(0f, score)) * 34f + RandomRange(0f, 52f));
                    break;
                default:
                    var mehBase = 4f + Mathf.Abs(score) * 10f + RandomRange(0f, 18f);
                    boxOfficeMillions = score >= 0f ? mehBase : -mehBase;
                    break;
            }

            boxOfficeMillions = Mathf.Round(boxOfficeMillions * 10f) / 10f;

            var stars = 3f + (score * 0.9f) + RandomRange(-0.25f, 0.25f);
            stars = Mathf.Clamp(Mathf.Round(stars * 2f) / 2f, 0.5f, 5f);

            return new PitchOutcome(
                pitch,
                boxOfficeMillions,
                stars,
                BuildHeadline(pitch.Title, tone),
                BuildOutcomeBody(hotMatches, notMatches, tone));
        }

        private List<string> FindMatches(string[] tags, string[] trends)
        {
            var matches = new List<string>();
            for (var i = 0; i < trends.Length; i++)
            {
                for (var j = 0; j < tags.Length; j++)
                {
                    if (string.Equals(trends[i], tags[j], StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(trends[i]);
                        break;
                    }
                }
            }

            return matches;
        }

        private string BuildHeadline(string title, OutcomeTone tone)
        {
            string[] options;
            switch (tone)
            {
                case OutcomeTone.Success:
                    options = new[]
                    {
                        $"{title} crushes opening weekend!",
                        $"{title} wins over crowds!",
                        $"{title} turns into a breakout hit!",
                    };
                    break;
                case OutcomeTone.Failure:
                    options = new[]
                    {
                        $"{title} stalls at the multiplex.",
                        $"{title} crashes with critics and crowds.",
                        $"{title} misses its mark.",
                    };
                    break;
                default:
                    options = new[]
                    {
                        $"{title} lands with a shrug.",
                        $"{title} hangs around but never pops.",
                        $"{title} finds an audience, barely.",
                    };
                    break;
            }

            return options[m_Random.Next(options.Length)];
        }

        private string BuildOutcomeBody(List<string> hotMatches, List<string> notMatches, OutcomeTone tone)
        {
            if (hotMatches.Count >= 3)
            {
                return $"The audience devoured the {hotMatches[0]}, {hotMatches[1]}, and {hotMatches[2]}.";
            }

            if (notMatches.Count >= 3)
            {
                return $"Critics could barely stand the {notMatches[0]}, {notMatches[1]}, and {notMatches[2]}.";
            }

            if (hotMatches.Count >= 2)
            {
                return $"{hotMatches[0]} and {hotMatches[1]} proved to be a winning formula.";
            }

            if (notMatches.Count >= 2)
            {
                return $"Both the {notMatches[0]} and the {notMatches[1]} fell flat with theatergoers.";
            }

            if (hotMatches.Count == 1 && notMatches.Count == 1)
            {
                return $"Critics loved the {hotMatches[0]} but tired of the {notMatches[0]}.";
            }

            if (hotMatches.Count == 1)
            {
                return tone == OutcomeTone.Success
                    ? $"{hotMatches[0]} was a hit with audiences."
                    : $"{hotMatches[0]} helped, but it was not enough to save the release.";
            }

            if (notMatches.Count == 1)
            {
                return tone == OutcomeTone.Failure
                    ? $"The {notMatches[0]} sent audiences reaching for the exits."
                    : $"The {notMatches[0]} dulled what should have been a hotter opening.";
            }

            switch (tone)
            {
                case OutcomeTone.Success:
                    return "No clear trend advantage, but the crowd showed up anyway.";
                case OutcomeTone.Failure:
                    return "Nothing in the market helped this one find a crowd.";
                default:
                    return "It found a modest audience without ever catching fire.";
            }
        }

        private void TryCompleteMatch()
        {
            if (!m_TimerExpired)
            {
                return;
            }

            for (var i = 0; i < m_Lanes.Length; i++)
            {
                if (m_Lanes[i].IsResolving)
                {
                    return;
                }
            }

            ShowResults();
        }

        private void ApplyTrendText(LaneState lane)
        {
            var pack = m_TrendPacks[lane.CurrentTrendIndex];
            lane.HotText.text = $"HOT\n{pack.Hots[0]}\n{pack.Hots[1]}\n{pack.Hots[2]}";
            lane.NotText.text = $"NOT\n{pack.Nots[0]}\n{pack.Nots[1]}\n{pack.Nots[2]}";
        }

        private void ApplyPitchText(LaneState lane)
        {
            lane.PitchTitle.text = lane.CurrentPitch.Title.ToUpperInvariant();
            lane.PitchTags.text = $"TAGS  |  {lane.CurrentPitch.Tags[0]}  |  {lane.CurrentPitch.Tags[1]}  |  {lane.CurrentPitch.Tags[2]}";
            lane.PitchLogline.text = lane.CurrentPitch.Logline;
        }

        private void UpdateTimerAndScore(LaneState lane)
        {
            var totalSeconds = Mathf.CeilToInt(m_RemainingTime);
            lane.TimerText.text = $"{totalSeconds / 60}:{totalSeconds % 60:00}";
            lane.ProfitText.text = $"NET {FormatMoney(lane.TotalProfitMillions)}";
        }

        private void UpdateInkPresentation(LaneState lane)
        {
            lane.ApprovePadImage.color = lane.LoadedInk == InkColor.Approve ? Hex("#26563A") : Hex("#173123");
            lane.RejectPadImage.color = lane.LoadedInk == InkColor.Reject ? Hex("#6A2431") : Hex("#421C27");

            if (m_TimerExpired)
            {
                lane.ActionText.text = "Time is up. Finalizing the slate.";
                return;
            }

            switch (lane.LoadedInk)
            {
                case InkColor.Approve:
                    lane.ActionText.text = "GREEN ink loaded. Stamp the pitch to approve it.";
                    lane.ActionText.color = s_Approve;
                    break;
                case InkColor.Reject:
                    lane.ActionText.text = "RED ink loaded. Stamp the pitch to pass on it.";
                    lane.ActionText.color = s_Reject;
                    break;
                default:
                    lane.ActionText.text = $"Dip the {lane.StamperName} in ink, then stamp the card.";
                    lane.ActionText.color = s_CardInk;
                    break;
            }
        }

        private string BuildResultsSummary(LaneState lane)
        {
            var builder = new StringBuilder();
            builder.AppendLine(lane.Label);
            builder.AppendLine();
            builder.AppendLine($"Approved: {lane.ApprovedOutcomes.Count}");
            builder.AppendLine($"Net Profit: {FormatMoney(lane.TotalProfitMillions)}");
            builder.AppendLine($"Avg. Rating: {AverageStars(lane):0.0}/5");
            builder.AppendLine();

            if (lane.ApprovedOutcomes.Count == 0)
            {
                builder.AppendLine("No films were approved.");
                builder.AppendLine("The studio played it safe and finished with a blank slate.");
                return builder.ToString();
            }

            var best = lane.ApprovedOutcomes[0];
            var worst = lane.ApprovedOutcomes[0];

            for (var i = 1; i < lane.ApprovedOutcomes.Count; i++)
            {
                var outcome = lane.ApprovedOutcomes[i];
                if (outcome.BoxOfficeMillions > best.BoxOfficeMillions)
                {
                    best = outcome;
                }

                if (outcome.BoxOfficeMillions < worst.BoxOfficeMillions)
                {
                    worst = outcome;
                }
            }

            builder.AppendLine($"Best Hit: {best.Pitch.Title} ({FormatMoney(best.BoxOfficeMillions)})");
            builder.AppendLine($"Biggest Flop: {worst.Pitch.Title} ({FormatMoney(worst.BoxOfficeMillions)})");
            return builder.ToString();
        }

        private float AverageStars(LaneState lane)
        {
            if (lane.ApprovedOutcomes.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < lane.ApprovedOutcomes.Count; i++)
            {
                total += lane.ApprovedOutcomes[i].Stars;
            }

            return total / lane.ApprovedOutcomes.Count;
        }

        private ZoneHit HitTestZone(Vector2 unityScreenPoint)
        {
            switch (m_State)
            {
                case GameState.Intro:
                    foreach (var lane in m_Lanes)
                    {
                        if (ContainsScreenPoint(lane.IntroCard, unityScreenPoint, s_IntroHitPadding))
                        {
                            return new ZoneHit(ZoneKind.IntroCard, lane);
                        }
                    }

                    break;

                case GameState.Playing:
                    foreach (var lane in m_Lanes)
                    {
                        if (ContainsScreenPoint(lane.ApprovePad, unityScreenPoint, s_PadHitPadding))
                        {
                            return new ZoneHit(ZoneKind.ApprovePad, lane);
                        }

                        if (ContainsScreenPoint(lane.RejectPad, unityScreenPoint, s_PadHitPadding))
                        {
                            return new ZoneHit(ZoneKind.RejectPad, lane);
                        }

                        if (ContainsScreenPoint(lane.PitchCard, unityScreenPoint, s_PitchHitPadding))
                        {
                            return new ZoneHit(ZoneKind.PitchCard, lane);
                        }
                    }

                    break;

                case GameState.Results:
                    if (ContainsScreenPoint(m_PlayAgainZone, unityScreenPoint, s_ResultHitPadding))
                    {
                        return new ZoneHit(ZoneKind.PlayAgain, null);
                    }

                    if (ContainsScreenPoint(m_ExitZone, unityScreenPoint, s_ResultHitPadding))
                    {
                        return new ZoneHit(ZoneKind.Exit, null);
                    }

                    break;
            }

            return new ZoneHit(ZoneKind.None, null);
        }

        private void StampStaticCard(RectTransform target, Text stampText, string label, Color color, Vector2 screenPoint, float orientationRadians)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screenPoint, null, out localPoint);

            var rect = target.rect;
            localPoint.x = Mathf.Clamp(localPoint.x, rect.xMin + 90f, rect.xMax - 90f);
            localPoint.y = Mathf.Clamp(localPoint.y, rect.yMin + 50f, rect.yMax - 50f);

            stampText.text = label;
            stampText.color = color;
            stampText.rectTransform.anchoredPosition = localPoint;
            stampText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -orientationRadians * Mathf.Rad2Deg);
            stampText.gameObject.SetActive(true);
        }

        private bool CanStampZone(BoardContact contact, ZoneHit zone)
        {
            switch (m_State)
            {
                case GameState.Intro:
                case GameState.Playing:
                    return zone.lane != null && contact.glyphId == zone.lane.StamperGlyphId;
                case GameState.Results:
                    return zone.kind != ZoneKind.None && IsConfiguredStamperGlyph(contact.glyphId);
                default:
                    return false;
            }
        }

        private static bool IsConfiguredStamperGlyph(int glyphId)
        {
            return glyphId == OrangeRobotGlyphId || glyphId == PinkRobotGlyphId;
        }

        private bool ContainsScreenPoint(RectTransform rectTransform, Vector2 unityScreenPoint, Vector2 padding)
        {
            if (rectTransform == null)
            {
                return false;
            }

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, unityScreenPoint, null, out localPoint))
            {
                return false;
            }

            var rect = rectTransform.rect;
            return localPoint.x >= rect.xMin - padding.x &&
                   localPoint.x <= rect.xMax + padding.x &&
                   localPoint.y >= rect.yMin - padding.y &&
                   localPoint.y <= rect.yMax + padding.y;
        }

        private static Vector2 ToUnityScreenPoint(Vector2 boardPoint)
        {
            // Board screenPosition is already in Unity screen space for this overlay UI.
            return boardPoint;
        }

        private float RandomRange(float minInclusive, float maxInclusive)
        {
            return Mathf.Lerp(minInclusive, maxInclusive, (float)m_Random.NextDouble());
        }

        private static string FormatMoney(float millions)
        {
            return $"{(millions >= 0f ? "+" : "-")}${Mathf.Abs(millions):0.0}M";
        }

        private static RectTransform CreateGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        private static RectTransform CreateContainer(Transform parent, string name, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateBand(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            return CreatePanel(parent, name, anchoredPosition, sizeDelta, color);
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return rect;
        }

        private Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor anchor,
            Color color,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.font = m_Font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return text;
        }

        private static void AddOutline(GameObject target, Color color)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(4f, -4f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetGroupVisibility(RectTransform group, bool visible)
        {
            if (group != null)
            {
                group.gameObject.SetActive(visible);
            }
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out var color);
            return color;
        }

        private enum GameState
        {
            Intro,
            Playing,
            Results,
        }

        private enum InkColor
        {
            None,
            Reject,
            Approve,
        }

        private enum ZoneKind
        {
            None,
            IntroCard,
            RejectPad,
            ApprovePad,
            PitchCard,
            PlayAgain,
            Exit,
        }

        private enum OutcomeTone
        {
            Failure,
            Meh,
            Success,
        }

        private enum LaneId
        {
            Left,
            Right,
        }

        private readonly struct ContactTrace
        {
            public readonly ZoneKind kind;
            public readonly LaneState lane;

            public ContactTrace(ZoneKind kind, LaneState lane)
            {
                this.kind = kind;
                this.lane = lane;
            }
        }

        private readonly struct ZoneHit
        {
            public readonly ZoneKind kind;
            public readonly LaneState lane;

            public ZoneHit(ZoneKind kind, LaneState lane)
            {
                this.kind = kind;
                this.lane = lane;
            }
        }

        private sealed class TrendPack
        {
            public readonly string[] Hots;
            public readonly string[] Nots;

            public TrendPack(string[] hots, string[] nots)
            {
                Hots = hots;
                Nots = nots;
            }
        }

        private sealed class PitchTemplate
        {
            public readonly string Title;
            public readonly string Logline;
            public readonly string[] Tags;

            public PitchTemplate(string title, string logline, params string[] tags)
            {
                Title = title;
                Logline = logline;
                Tags = tags;
            }
        }

        private sealed class PitchOutcome
        {
            public readonly PitchTemplate Pitch;
            public readonly float BoxOfficeMillions;
            public readonly float Stars;
            public readonly string Headline;
            public readonly string Body;

            public PitchOutcome(PitchTemplate pitch, float boxOfficeMillions, float stars, string headline, string body)
            {
                Pitch = pitch;
                BoxOfficeMillions = boxOfficeMillions;
                Stars = stars;
                Headline = headline;
                Body = body;
            }
        }

        private sealed class LaneState
        {
            public readonly LaneId id;
            public readonly string Label;
            public readonly int StamperGlyphId;
            public readonly string StamperName;
            public readonly Color StamperColor;

            public RectTransform Root;
            public RectTransform IntroCard;
            public RectTransform PhonePanel;
            public RectTransform HotPanel;
            public RectTransform NotPanel;
            public RectTransform PitchCard;
            public RectTransform RejectPad;
            public RectTransform ApprovePad;
            public RectTransform ResultsCard;

            public Image RejectPadImage;
            public Image ApprovePadImage;
            public CanvasGroup PitchCanvasGroup;

            public Text IntroStamp;
            public Text TimerText;
            public Text ProfitText;
            public Text NotificationHeadline;
            public Text NotificationBody;
            public Text HotText;
            public Text NotText;
            public Text PitchTitle;
            public Text PitchTags;
            public Text PitchLogline;
            public Text ActionText;
            public Text StampText;
            public Text ResultsText;

            public bool IntroReady;
            public bool IsResolving;
            public InkColor LoadedInk;
            public float TotalProfitMillions;
            public int CurrentTrendIndex;
            public int PendingTrendSwaps;
            public int NextTrendThresholdIndex;
            public PitchTemplate CurrentPitch;
            public List<PitchTemplate> PitchDeck = new List<PitchTemplate>();
            public List<PitchOutcome> ApprovedOutcomes = new List<PitchOutcome>();

            public LaneState(LaneId id, string label, int stamperGlyphId, string stamperName, Color stamperColor)
            {
                this.id = id;
                Label = label;
                StamperGlyphId = stamperGlyphId;
                StamperName = stamperName;
                StamperColor = stamperColor;
            }

            public void ResetForIntro()
            {
                IntroReady = false;
                LoadedInk = InkColor.None;
                IsResolving = false;
                TotalProfitMillions = 0f;
                CurrentTrendIndex = 0;
                PendingTrendSwaps = 0;
                NextTrendThresholdIndex = 0;
                CurrentPitch = null;
                PitchDeck.Clear();
                ApprovedOutcomes.Clear();
            }

            public void ResetForMatch(List<PitchTemplate> pitchDeck)
            {
                LoadedInk = InkColor.None;
                IsResolving = false;
                TotalProfitMillions = 0f;
                CurrentTrendIndex = 0;
                PendingTrendSwaps = 0;
                NextTrendThresholdIndex = 0;
                CurrentPitch = null;
                PitchDeck = pitchDeck;
                ApprovedOutcomes.Clear();
                NotificationHeadline.text = "Waiting for the first big release...";
                NotificationBody.text = "Approvals will send box-office notes here.";
                StampText.gameObject.SetActive(false);
            }
        }
    }
}
