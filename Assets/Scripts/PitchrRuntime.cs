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
        private const float HotMatchScoreWeight = 1.2f;
        private const float NotMatchScoreWeight = 1.2f;
        private const float MajoritySwingMagnitude = 0.85f;
        private const float TieSwingMagnitude = 1.25f;
        private static readonly float[] s_TrendSwapThresholds = { 40f, 20f };
        private static readonly Vector2 s_IntroHitPadding = new Vector2(36f, 36f);
        private static readonly Vector2 s_PitchHitPadding = new Vector2(34f, 22f);
        private static readonly Vector2 s_PadHitPadding = new Vector2(42f, 26f);
        private static readonly Vector2 s_ResultHitPadding = new Vector2(42f, 30f);

        private static readonly Color s_Background = Hex("#0B1116");
        private static readonly Color s_Banner = Hex("#12191E");
        private static readonly Color s_Card = Hex("#F0E3C8");
        private static readonly Color s_CardInk = Hex("#262118");
        private static readonly Color s_Phone = Hex("#10171D");
        private static readonly Color s_PhoneGlow = Hex("#7E95A5");
        private static readonly Color s_Hot = Hex("#D58B52");
        private static readonly Color s_Not = Hex("#6A9FCB");
        private static readonly Color s_Approve = Hex("#4FA86A");
        private static readonly Color s_Reject = Hex("#B45357");
        private static readonly Color s_OrangeRobot = Hex("#D79157");
        private static readonly Color s_PinkRobot = Hex("#CF6B97");
        private static readonly Color s_Cream = Hex("#F7F0E1");
        private static readonly Color s_Slate = Hex("#A3A8A8");
        private static readonly Color s_Winner = Hex("#D1B57A");
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
        private Texture2D m_TableTexture;
        private Sprite m_NoiseSprite;
        private Sprite m_RadialSprite;
        private Sprite m_GlassSprite;

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

            BuildGeneratedArt();
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

        private void BuildGeneratedArt()
        {
            m_TableTexture = CreateWoodTexture(512, 512);
            m_NoiseSprite = CreateSprite(CreateNoiseTexture(256, 256));
            m_RadialSprite = CreateSprite(CreateRadialTexture(256, 256));
            m_GlassSprite = CreateSprite(CreateGlassTexture(256, 256));
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

            CreateTiledRawImage(canvasRect, "Tabletop", m_TableTexture, Color.white, new Rect(0f, 0f, 7.6f, 4.2f));
            CreatePanel(canvasRect, "BackdropTint", Vector2.zero, s_ReferenceResolution, new Color(s_Background.r, s_Background.g, s_Background.b, 0.46f));
            CreateImage(canvasRect, "OverheadGlow", new Vector2(0f, 44f), new Vector2(1720f, 1000f), new Color(1f, 0.9f, 0.72f, 0.14f), m_RadialSprite);

            var productionMat = CreatePanel(canvasRect, "ProductionMat", new Vector2(0f, -26f), new Vector2(1760f, 874f), Hex("#202B31"));
            StyleDeskMat(productionMat);
            CreateImage(productionMat, "LeftStudioGlow", new Vector2(-430f, 28f), new Vector2(760f, 700f), WithAlpha(s_OrangeRobot, 0.11f), m_RadialSprite);
            CreateImage(productionMat, "RightStudioGlow", new Vector2(430f, 28f), new Vector2(760f, 700f), WithAlpha(s_PinkRobot, 0.11f), m_RadialSprite);
            CreatePanel(productionMat, "CenterDivider", new Vector2(0f, 10f), new Vector2(2f, 760f), new Color(1f, 1f, 1f, 0.06f));

            var headerPlaque = CreatePanel(canvasRect, "HeaderPlaque", new Vector2(0f, 468f), new Vector2(1460f, 136f), s_Banner);
            ApplyMetalPanelStyle(headerPlaque, s_Winner, s_Banner);
            CreateBand(canvasRect, "FooterBand", new Vector2(0f, -494f), new Vector2(1680f, 58f), new Color(0f, 0f, 0f, 0.2f));

            m_HeaderTitle = CreateText(headerPlaque, "HeaderTitle", "PITCHR", 64, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 18f), new Vector2(820f, 68f));
            m_HeaderSubtitle = CreateText(
                headerPlaque,
                "HeaderSubtitle",
                "Pink Robot stamps movie pitches. Dip in ink, then make the call.",
                22,
                FontStyle.Normal,
                TextAnchor.MiddleCenter,
                s_Slate,
                new Vector2(0f, -24f),
                new Vector2(1180f, 34f));

            m_IntroGroup = CreateGroup(canvasRect, "Intro");
            m_GameplayGroup = CreateGroup(canvasRect, "Gameplay");
            m_ResultsGroup = CreateGroup(canvasRect, "Results");

            BuildIntro(m_IntroGroup);
            BuildGameplay(m_GameplayGroup);
            BuildResults(m_ResultsGroup);
        }

        private void BuildIntro(RectTransform parent)
        {
            var introRibbon = CreatePanel(parent, "IntroRibbon", new Vector2(0f, 252f), new Vector2(1080f, 108f), Hex("#11181D"));
            ApplyMetalPanelStyle(introRibbon, s_Winner, Hex("#11181D"));
            CreateText(introRibbon, "IntroTitle", "STAMP BOTH READY CARDS TO OPEN THE PITCH MEETING", 30, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 18f), new Vector2(980f, 34f));
            CreateText(introRibbon, "IntroBody", "Each studio uses its own robot. Once both ready cards are stamped, the slate opens and the pitch review begins.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, s_Slate, new Vector2(0f, -20f), new Vector2(980f, 46f));

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
            var accent = lane.id == LaneId.Left ? s_Hot : s_Not;
            lane.IntroCard = CreatePanel(parent, $"{lane.Label}IntroCard", new Vector2(x, -18f), new Vector2(520f, 320f), s_Card);
            ApplyPaperCardStyle(lane.IntroCard, accent);

            CreateText(lane.IntroCard, "LaneLabel", lane.Label, 18, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 116f), new Vector2(420f, 24f));
            CreateText(lane.IntroCard, "IntroBadge", "READY CARD", 17, FontStyle.Bold, TextAnchor.MiddleCenter, accent, new Vector2(0f, 80f), new Vector2(240f, 24f));
            CreateText(lane.IntroCard, "RobotLabel", $"STAMPER: {lane.StamperName}", 28, FontStyle.Bold, TextAnchor.MiddleCenter, lane.StamperColor, new Vector2(0f, 34f), new Vector2(420f, 40f));
            CreateText(lane.IntroCard, "IntroAction", "Stamp this card to lock the studio into the meeting.\nBoth studios must be ready before the slate opens.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, s_CardInk, new Vector2(0f, -42f), new Vector2(430f, 116f));

            lane.IntroStamp = CreateText(lane.IntroCard, "ReadyStamp", string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleCenter, s_PinkRobot, Vector2.zero, new Vector2(300f, 80f));
            lane.IntroStamp.gameObject.SetActive(false);
        }

        private void BuildGameplay(RectTransform parent)
        {
            var gameplayRibbon = CreatePanel(parent, "GameplayRibbon", new Vector2(0f, 356f), new Vector2(940f, 74f), Hex("#11181D"));
            ApplyMetalPanelStyle(gameplayRibbon, s_Winner, Hex("#11181D"));
            CreateText(gameplayRibbon, "GameplayPrompt", "DIP THE ROBOT IN INK, THEN STAMP THE PITCH", 27, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, Vector2.zero, new Vector2(860f, 32f));

            BuildGameplayLane(parent, m_Lanes[0], -470f);
            BuildGameplayLane(parent, m_Lanes[1], 470f);
        }

        private void BuildGameplayLane(RectTransform parent, LaneState lane, float x)
        {
            const float laneRootY = -140f;
            const float trendPanelY = 176f;
            const float pitchCardY = -56f;
            const float padY = -304f;
            var trendPanelSize = new Vector2(240f, 112f);
            var pitchCardSize = new Vector2(520f, 320f);
            var padSize = new Vector2(230f, 154f);

            lane.Root = CreateContainer(parent, $"{lane.Label}Root", new Vector2(x, laneRootY));

            lane.PhonePanel = CreatePanel(lane.Root, "Phone", new Vector2(0f, 360f), new Vector2(360f, 220f), s_Phone);
            ApplyGlassPanelStyle(lane.PhonePanel, lane.StamperColor);
            CreateText(lane.PhonePanel, "PhoneLabel", lane.Label, 14, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 92f), new Vector2(240f, 20f));
            CreateText(lane.PhonePanel, "RobotBadge", lane.StamperName, 16, FontStyle.Bold, TextAnchor.MiddleCenter, lane.StamperColor, new Vector2(0f, 68f), new Vector2(240f, 20f));
            lane.TimerText = CreateText(lane.PhonePanel, "Timer", "1:00", 36, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 32f), new Vector2(240f, 44f));
            lane.ProfitText = CreateText(lane.PhonePanel, "Profit", "NET $0.0M", 18, FontStyle.Bold, TextAnchor.MiddleCenter, s_Winner, new Vector2(0f, -6f), new Vector2(280f, 24f));
            lane.NotificationHeadline = CreateText(lane.PhonePanel, "Headline", "Waiting for the first big release...", 15, FontStyle.Bold, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, -40f), new Vector2(300f, 32f));
            lane.NotificationBody = CreateText(lane.PhonePanel, "Body", "Approvals will send box-office notes here.", 13, FontStyle.Normal, TextAnchor.UpperLeft, s_Slate, new Vector2(0f, -80f), new Vector2(300f, 48f));

            lane.HotPanel = CreatePanel(lane.Root, "HotPanel", new Vector2(-128f, trendPanelY), trendPanelSize, Hex("#4A3426"));
            ApplyTrendPanelStyle(lane.HotPanel, s_Hot, Hex("#4A3426"));
            lane.HotText = CreateText(lane.HotPanel, "HotText", string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, 0f), new Vector2(188f, 92f));
            ConfigureMultiLineBestFit(lane.HotText, minSize: 14, maxSize: 19);

            lane.NotPanel = CreatePanel(lane.Root, "NotPanel", new Vector2(128f, trendPanelY), trendPanelSize, Hex("#233545"));
            ApplyTrendPanelStyle(lane.NotPanel, s_Not, Hex("#233545"));
            lane.NotText = CreateText(lane.NotPanel, "NotText", string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft, s_Cream, new Vector2(0f, 0f), new Vector2(188f, 92f));
            ConfigureMultiLineBestFit(lane.NotText, minSize: 14, maxSize: 19);

            lane.PitchCard = CreatePanel(lane.Root, "PitchCard", new Vector2(0f, pitchCardY), pitchCardSize, s_Card);
            lane.PitchCardHomePosition = lane.PitchCard.anchoredPosition;
            ApplyPaperCardStyle(lane.PitchCard, lane.StamperColor);
            lane.PitchCanvasGroup = lane.PitchCard.gameObject.AddComponent<CanvasGroup>();
            CreateText(lane.PitchCard, "PitchLabel", "CURRENT PITCH", 16, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 138f), new Vector2(240f, 22f));
            lane.PitchTitle = CreateText(lane.PitchCard, "Title", "Pitch Title", 34, FontStyle.Bold, TextAnchor.UpperCenter, s_CardInk, new Vector2(0f, 104f), new Vector2(460f, 44f));
            ConfigureSingleLineBestFit(lane.PitchTitle, minSize: 20, maxSize: 34);
            lane.PitchTags = CreateText(lane.PitchCard, "Tags", "TAGS", 18, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 68f), new Vector2(500f, 24f));
            lane.PitchLogline = CreateText(lane.PitchCard, "Logline", "Pitch logline goes here.", 24, FontStyle.Normal, TextAnchor.UpperLeft, s_CardInk, new Vector2(0f, -10f), new Vector2(460f, 108f));
            lane.ActionText = CreateText(lane.PitchCard, "Action", string.Empty, 20, FontStyle.Bold, TextAnchor.LowerCenter, s_CardInk, new Vector2(0f, -114f), new Vector2(400f, 52f));
            lane.StampText = CreateText(lane.PitchCard, "Stamp", string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleCenter, s_Approve, Vector2.zero, new Vector2(320f, 80f));
            lane.StampText.gameObject.SetActive(false);

            lane.RejectPad = CreatePanel(lane.Root, "RejectPad", new Vector2(-128f, padY), padSize, Hex("#171616"));
            ApplyInkPadStyle(lane.RejectPad, s_Reject, out lane.RejectPadImage);
            CreateText(lane.RejectPad, "RejectLabel", "RED INK", 24, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, -18f), new Vector2(170f, 28f));
            CreateText(lane.RejectPad, "RejectHint", "PASS", 18, FontStyle.Bold, TextAnchor.MiddleCenter, s_Reject, new Vector2(0f, -46f), new Vector2(160f, 22f));

            lane.ApprovePad = CreatePanel(lane.Root, "ApprovePad", new Vector2(128f, padY), padSize, Hex("#171616"));
            ApplyInkPadStyle(lane.ApprovePad, s_Approve, out lane.ApprovePadImage);
            CreateText(lane.ApprovePad, "ApproveLabel", "GREEN INK", 24, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, -18f), new Vector2(170f, 28f));
            CreateText(lane.ApprovePad, "ApproveHint", "GREENLIGHT", 18, FontStyle.Bold, TextAnchor.MiddleCenter, s_Approve, new Vector2(0f, -46f), new Vector2(180f, 22f));
        }

        private void BuildResults(RectTransform parent)
        {
            m_WinnerBanner = CreateText(parent, "WinnerBanner", string.Empty, 42, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 284f), new Vector2(1200f, 54f));

            var resultsRibbon = CreatePanel(parent, "ResultsRibbon", new Vector2(0f, 236f), new Vector2(980f, 74f), Hex("#11181D"));
            ApplyMetalPanelStyle(resultsRibbon, s_Winner, Hex("#11181D"));
            m_ResultsPrompt = CreateText(resultsRibbon, "ResultsPrompt", "Stamp PLAY AGAIN to rerun the pitch meeting, or EXIT to close the vignette.", 22, FontStyle.Normal, TextAnchor.MiddleCenter, s_Slate, Vector2.zero, new Vector2(900f, 32f));

            BuildResultsLane(parent, m_Lanes[0], -470f);
            BuildResultsLane(parent, m_Lanes[1], 470f);

            m_PlayAgainZone = CreatePanel(parent, "PlayAgainZone", new Vector2(-180f, -320f), new Vector2(320f, 150f), Hex("#163525"));
            ApplyMetalPanelStyle(m_PlayAgainZone, s_Approve, Hex("#163525"));
            CreateText(m_PlayAgainZone, "PlayAgain", "PLAY AGAIN", 34, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 14f), new Vector2(220f, 44f));
            CreateText(m_PlayAgainZone, "PlayAgainHint", "Stamp with the Pink Robot", 20, FontStyle.Normal, TextAnchor.MiddleCenter, s_Approve, new Vector2(0f, -26f), new Vector2(260f, 28f));

            m_ExitZone = CreatePanel(parent, "ExitZone", new Vector2(180f, -320f), new Vector2(320f, 150f), Hex("#381A22"));
            ApplyMetalPanelStyle(m_ExitZone, s_Reject, Hex("#381A22"));
            CreateText(m_ExitZone, "Exit", "EXIT", 34, FontStyle.Bold, TextAnchor.MiddleCenter, s_Cream, new Vector2(0f, 14f), new Vector2(220f, 44f));
            CreateText(m_ExitZone, "ExitHint", "Stamp to close the app", 20, FontStyle.Normal, TextAnchor.MiddleCenter, s_Reject, new Vector2(0f, -26f), new Vector2(220f, 28f));
        }

        private void BuildResultsLane(RectTransform parent, LaneState lane, float x)
        {
            var accent = lane.id == LaneId.Left ? s_Hot : s_Not;
            lane.ResultsCard = CreatePanel(parent, $"{lane.Label}ResultsCard", new Vector2(x, -20f), new Vector2(520f, 430f), s_Card);
            ApplyPaperCardStyle(lane.ResultsCard, accent);
            CreateText(lane.ResultsCard, "ResultsLabel", lane.Label, 18, FontStyle.Bold, TextAnchor.UpperCenter, s_Slate, new Vector2(0f, 158f), new Vector2(320f, 22f));
            lane.ResultsText = CreateText(lane.ResultsCard, "ResultsText", string.Empty, 23, FontStyle.Normal, TextAnchor.UpperLeft, s_CardInk, new Vector2(0f, -8f), new Vector2(420f, 314f));
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
                AdvancePitch(lane);
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
                    lane.CurrentTrendIndex = (lane.CurrentTrendIndex + 1) % m_TrendPacks.Count;
                    ApplyTrendText(lane);
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
            lane.PitchCard.anchoredPosition = lane.PitchCardHomePosition;
            lane.PitchCanvasGroup.alpha = 1f;

            if (!m_TimerExpired)
            {
                AdvancePitch(lane);
                UpdateInkPresentation(lane);
            }

            lane.IsResolving = false;
            TryCompleteMatch();
        }

        private IEnumerator SlidePitchCard(RectTransform card, CanvasGroup canvasGroup, float xOffset)
        {
            const float duration = 0.22f;
            var elapsed = 0f;
            var start = card.anchoredPosition;
            var target = start + new Vector2(xOffset, 0f);

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

        private void AdvancePitch(LaneState lane)
        {
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

            var swingMagnitude = hotCount == notCount ? TieSwingMagnitude : MajoritySwingMagnitude;
            var swing = RandomRange(-swingMagnitude, swingMagnitude);
            var score = (hotCount * HotMatchScoreWeight) - (notCount * NotMatchScoreWeight) + swing;

            var tone = score >= 0f ? OutcomeTone.Success : OutcomeTone.Failure;

            var magnitude = 8f + RandomRange(0f, 24f) + (Mathf.Abs(score) * 19f);
            if (score >= 0f)
            {
                magnitude += (hotCount * 21f) + (Mathf.Max(0, hotCount - notCount) * 12f) - (notCount * 4f);
            }
            else
            {
                magnitude += (notCount * 21f) + (Mathf.Max(0, notCount - hotCount) * 12f) - (hotCount * 4f);
            }

            var boxOfficeMillions = Mathf.Round(Mathf.Max(2f, magnitude) * 10f) / 10f;
            if (score < 0f)
            {
                boxOfficeMillions *= -1f;
            }

            var stars = 3f + (score * 0.85f) + RandomRange(-0.35f, 0.35f);
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
            lane.HotText.text = $"HOTS\n{pack.Hots[0]}\n{pack.Hots[1]}\n{pack.Hots[2]}";
            lane.NotText.text = $"NOTS\n{pack.Nots[0]}\n{pack.Nots[1]}\n{pack.Nots[2]}";
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
            lane.ApprovePadImage.color = lane.LoadedInk == InkColor.Approve ? Hex("#3D8A59") : Hex("#193423");
            lane.RejectPadImage.color = lane.LoadedInk == InkColor.Reject ? Hex("#8C3138") : Hex("#3C1E25");

            if (m_TimerExpired)
            {
                lane.ActionText.text = "Time is up. Finalizing the slate.";
                lane.ActionText.color = s_Slate;
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
            stampText.color = WithAlpha(color, 0.9f);
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

        private void StyleDeskMat(RectTransform mat)
        {
            var image = mat.GetComponent<Image>();
            image.color = Hex("#202C32");
            image.sprite = m_NoiseSprite;

            AddDropShadow(mat.gameObject, new Color(0f, 0f, 0f, 0.36f), new Vector2(0f, -18f));
            AddOutline(mat.gameObject, new Color(s_PhoneGlow.r, s_PhoneGlow.g, s_PhoneGlow.b, 0.16f));

            CreateInsetImage(mat, "MatSheen", 16f, 16f, 16f, 16f, new Color(1f, 1f, 1f, 0.05f), m_GlassSprite);
            CreatePanel(mat, "MatTopEdge", new Vector2(0f, mat.sizeDelta.y * 0.5f - 10f), new Vector2(mat.sizeDelta.x - 120f, 2f), new Color(1f, 1f, 1f, 0.08f));
            CreatePanel(mat, "MatBottomEdge", new Vector2(0f, -mat.sizeDelta.y * 0.5f + 10f), new Vector2(mat.sizeDelta.x - 120f, 2f), new Color(0f, 0f, 0f, 0.18f));
        }

        private void ApplyMetalPanelStyle(RectTransform panel, Color accent, Color baseColor)
        {
            var image = panel.GetComponent<Image>();
            image.color = baseColor;
            image.sprite = m_NoiseSprite;

            AddDropShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.32f), new Vector2(0f, -12f));
            AddOutline(panel.gameObject, new Color(accent.r, accent.g, accent.b, 0.22f));

            CreateInsetImage(panel, "PanelSheen", 10f, 10f, 10f, 10f, new Color(1f, 1f, 1f, 0.06f), m_GlassSprite);
            CreatePanel(panel, "TopAccent", new Vector2(0f, panel.sizeDelta.y * 0.5f - 10f), new Vector2(panel.sizeDelta.x - 42f, 3f), new Color(accent.r, accent.g, accent.b, 0.62f));
            CreatePanel(panel, "BottomShadow", new Vector2(0f, -panel.sizeDelta.y * 0.5f + 10f), new Vector2(panel.sizeDelta.x - 42f, 2f), new Color(0f, 0f, 0f, 0.22f));
        }

        private void ApplyPaperCardStyle(RectTransform card, Color accent)
        {
            var image = card.GetComponent<Image>();
            image.color = s_Card;
            image.sprite = m_NoiseSprite;

            AddDropShadow(card.gameObject, new Color(0.08f, 0.05f, 0.03f, 0.42f), new Vector2(0f, -16f));
            AddOutline(card.gameObject, new Color(accent.r, accent.g, accent.b, 0.2f));

            var size = card.sizeDelta;
            CreatePanel(card, "AccentStrip", new Vector2(0f, size.y * 0.5f - 18f), new Vector2(size.x - 56f, 8f), new Color(accent.r, accent.g, accent.b, 0.76f));
            CreatePanel(card, "BottomRule", new Vector2(0f, -size.y * 0.5f + 20f), new Vector2(size.x - 56f, 2f), new Color(0f, 0f, 0f, 0.08f));

            var leftTape = CreatePanel(card, "LeftTape", new Vector2(-size.x * 0.28f, size.y * 0.5f - 12f), new Vector2(72f, 18f), new Color(1f, 0.98f, 0.86f, 0.34f));
            leftTape.localRotation = Quaternion.Euler(0f, 0f, -6f);

            var rightTape = CreatePanel(card, "RightTape", new Vector2(size.x * 0.28f, size.y * 0.5f - 12f), new Vector2(72f, 18f), new Color(1f, 0.98f, 0.86f, 0.34f));
            rightTape.localRotation = Quaternion.Euler(0f, 0f, 7f);
        }

        private void ApplyGlassPanelStyle(RectTransform panel, Color accent)
        {
            var image = panel.GetComponent<Image>();
            image.color = s_Phone;
            image.sprite = m_NoiseSprite;

            AddDropShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.42f), new Vector2(0f, -16f));
            AddOutline(panel.gameObject, new Color(accent.r, accent.g, accent.b, 0.16f));

            var size = panel.sizeDelta;
            var screen = CreatePanel(panel, "Screen", Vector2.zero, size - new Vector2(28f, 28f), Hex("#17212A"));
            var screenImage = screen.GetComponent<Image>();
            screenImage.sprite = m_GlassSprite;

            CreateImage(screen, "ScreenGlow", Vector2.zero, screen.sizeDelta, new Color(1f, 1f, 1f, 0.12f), m_GlassSprite);

            var noteTray = CreatePanel(panel, "NoteTray", new Vector2(0f, -72f), new Vector2(300f, 92f), new Color(1f, 1f, 1f, 0.05f));
            noteTray.GetComponent<Image>().sprite = m_GlassSprite;

            CreatePanel(panel, "Speaker", new Vector2(0f, size.y * 0.5f - 16f), new Vector2(72f, 6f), new Color(1f, 1f, 1f, 0.08f));
            CreateImage(panel, "StatusLamp", new Vector2(size.x * 0.5f - 22f, size.y * 0.5f - 18f), new Vector2(12f, 12f), new Color(accent.r, accent.g, accent.b, 0.9f), m_RadialSprite);
        }

        private void ApplyTrendPanelStyle(RectTransform panel, Color accent, Color baseColor)
        {
            var image = panel.GetComponent<Image>();
            image.color = baseColor;
            image.sprite = m_NoiseSprite;

            AddDropShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.3f), new Vector2(0f, -10f));
            AddOutline(panel.gameObject, new Color(accent.r, accent.g, accent.b, 0.24f));

            var size = panel.sizeDelta;
            CreatePanel(panel, "TrendTopBar", new Vector2(0f, size.y * 0.5f - 14f), new Vector2(size.x - 26f, 6f), new Color(accent.r, accent.g, accent.b, 0.76f));
            CreateImage(panel, "TrendPin", new Vector2(0f, size.y * 0.5f - 18f), new Vector2(14f, 14f), new Color(s_Winner.r, s_Winner.g, s_Winner.b, 0.85f), m_RadialSprite);
            CreateInsetImage(panel, "TrendSheen", 10f, 10f, 10f, 10f, new Color(1f, 1f, 1f, 0.04f), m_GlassSprite);
        }

        private void ApplyInkPadStyle(RectTransform panel, Color accent, out Image inkSurface)
        {
            var image = panel.GetComponent<Image>();
            image.color = Hex("#171616");
            image.sprite = m_NoiseSprite;

            AddDropShadow(panel.gameObject, new Color(0f, 0f, 0f, 0.34f), new Vector2(0f, -12f));
            AddOutline(panel.gameObject, new Color(accent.r, accent.g, accent.b, 0.24f));

            var size = panel.sizeDelta;
            var tray = CreatePanel(panel, "Tray", new Vector2(0f, 24f), new Vector2(size.x - 36f, 84f), Hex("#0E1012"));
            tray.GetComponent<Image>().sprite = m_NoiseSprite;

            var well = CreatePanel(panel, "InkWell", new Vector2(0f, 24f), new Vector2(size.x - 60f, 56f), Color.black);
            inkSurface = well.GetComponent<Image>();
            inkSurface.sprite = m_RadialSprite;

            CreateImage(well, "InkSheen", Vector2.zero, well.sizeDelta, new Color(1f, 1f, 1f, 0.08f), m_GlassSprite);
            CreatePanel(panel, "LabelPlate", new Vector2(0f, -38f), new Vector2(size.x - 48f, 30f), new Color(1f, 1f, 1f, 0.04f));
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
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

        private static RawImage CreateTiledRawImage(Transform parent, string name, Texture texture, Color color, Rect uvRect)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Stretch(rect);

            var rawImage = go.GetComponent<RawImage>();
            rawImage.texture = texture;
            rawImage.color = color;
            rawImage.uvRect = uvRect;
            rawImage.raycastTarget = false;
            return rawImage;
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

        private static Image CreateImage(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, Sprite sprite)
        {
            var rect = CreatePanel(parent, name, anchoredPosition, sizeDelta, color);
            var image = rect.GetComponent<Image>();
            image.sprite = sprite;
            return image;
        }

        private static Image CreateInsetImage(Transform parent, string name, float left, float right, float top, float bottom, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
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

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -2f);
            shadow.useGraphicAlpha = true;

            return text;
        }

        private static void ConfigureSingleLineBestFit(Text text, int minSize, int maxSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void ConfigureMultiLineBestFit(Text text, int minSize, int maxSize)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static void AddOutline(GameObject target, Color color)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        private static void AddDropShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
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

        private static Texture2D CreateWoodTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            var dark = Hex("#402A1F");
            var light = Hex("#7A543A");

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var u = x / (float)(width - 1);
                    var v = y / (float)(height - 1);

                    var plank = Mathf.PerlinNoise((u * 6.4f) + 1.1f, (v * 0.8f) + 2.8f);
                    var grain = Mathf.PerlinNoise((u * 36f) + (plank * 1.6f), (v * 3.4f) + 9.2f);
                    var detail = Mathf.PerlinNoise((u * 120f) + 3.3f, (v * 8.5f) + 4.8f);
                    var tone = Mathf.Clamp01((plank * 0.58f) + (grain * 0.28f) + (detail * 0.14f));

                    var color = Color.Lerp(dark, light, tone);
                    var warmth = 0.9f + (detail * 0.16f);
                    pixels[(y * width) + x] = new Color(color.r * warmth, color.g * warmth, color.b * warmth, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateNoiseTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var coarse = Mathf.PerlinNoise((x + 13f) * 0.08f, (y + 29f) * 0.08f);
                    var detail = Mathf.PerlinNoise((x + 101f) * 0.35f, (y + 53f) * 0.35f);
                    var value = Mathf.Lerp(0.88f, 1.06f, (coarse * 0.72f) + (detail * 0.28f));
                    pixels[(y * width) + x] = new Color(value, value, value, 1f);
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateRadialTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var u = (x / (float)(width - 1) * 2f) - 1f;
                    var v = (y / (float)(height - 1) * 2f) - 1f;
                    var distance = Mathf.Sqrt((u * u) + (v * v));
                    var alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * (3f - (2f * alpha));
                    pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateGlassTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var u = x / (float)(width - 1);
                    var v = y / (float)(height - 1);
                    var topGlow = Mathf.Clamp01(1f - Mathf.Abs(v - 0.12f) * 4.5f);
                    var diagonal = Mathf.Clamp01(1f - Mathf.Abs(u - (0.74f - (v * 0.55f))) * 6.8f);
                    var lowerFade = Mathf.Clamp01(1f - Mathf.Abs(v - 0.84f) * 5.2f);
                    var alpha = 0.12f + (topGlow * 0.16f) + (diagonal * 0.18f) + (lowerFade * 0.04f);
                    var shade = Mathf.Lerp(0.84f, 1f, topGlow * 0.55f);
                    pixels[(y * width) + x] = new Color(shade, shade, shade, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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
            public Vector2 PitchCardHomePosition;

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
                NextTrendThresholdIndex = 0;
                CurrentPitch = null;
                PitchDeck = pitchDeck;
                ApprovedOutcomes.Clear();
                NotificationHeadline.text = "Waiting for the first big release...";
                NotificationBody.text = "Approvals will send box-office notes here.";
                PitchCard.anchoredPosition = PitchCardHomePosition;
                PitchCanvasGroup.alpha = 1f;
                StampText.gameObject.SetActive(false);
            }
        }
    }
}
