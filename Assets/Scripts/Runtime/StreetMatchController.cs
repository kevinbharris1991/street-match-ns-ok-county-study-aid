using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = System.Random;

namespace StreetMatch
{
    public sealed class StreetMatchController : MonoBehaviour
    {
        private enum Mode { Level, Review, Mix }
        private readonly Random random = new Random();
        private readonly List<MatchCard> cards = new List<MatchCard>();
        private readonly List<Button> levelButtons = new List<Button>();
        private LearningProgress progress;
        private MatchRound round;
        private List<int> roundIds;
        private List<int> mixDeck;
        private int mixPage;
        private int level = 1;
        private Mode mode;
        private RectTransform canvasRect;
        private RectTransform content;
        private RectTransform ghost;
        private RectTransform gameMenu;
        private RectTransform splashScreen;
        private InputField nameInput;
        private Text stats;
        private Text feedback;
        private Text storageStatus;
        private Text menuStatus;
        private Text roundStatus;
        private Image progressFill;
        private MatchCard selected;
        private MatchCard dragged;
        private Button primary;
        private Button mixButton;
        private Button soundButton;
        private AudioSource audioSource;
        private AudioClip correctSound;
        private AudioClip wrongSound;
        private bool muted;
        private string playerName;
        private string storageWarning;
        private string loadWarning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "StreetMatch" ||
                FindObjectOfType<StreetMatchController>() != null) return;
            new GameObject("Street Match (N & S): OK County — Study Aid").AddComponent<StreetMatchController>();
        }

        private void Awake()
        {
            progress = GameSave.Load(out loadWarning);
            level = progress.UnlockedLevel;
            muted = PlayerPrefs.GetInt("SherrysGame.Muted", 0) == 1;
            playerName = PlayerPrefs.GetString("SherrysGame.PlayerName", "");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            if (FindObjectOfType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            correctSound = Tone(660, 880);
            wrongSound = Tone(190, 220);
            BuildShell();
            ShowLevel(level);
            ShowSplashScreen();
        }

        private void BuildShell()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("2D Camera", typeof(Camera));
                cameraObject.transform.SetParent(transform, false);
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = GameUI.Ink;
                camera.cullingMask = 0;
            }
            var canvasObject = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasRect = (RectTransform)canvasObject.transform;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 1000);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            GameUI.Surface(canvasRect, GameUI.Ink, false);

            if (FindObjectOfType<EventSystem>() == null)
                new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));

            RectTransform root = GameUI.Column("Safe Area", canvasRect, 24, 12);
            GameUI.Stretch(root);
            root.gameObject.AddComponent<ScreenSafeArea>();
            RectTransform header = GameUI.Row("Header", root, 16);
            GameUI.Size(header, 88);
            RectTransform heading = GameUI.Column("Title", header, 0, 0);
            GameUI.FlexWidth(heading, 2);
            Text eyebrow = GameUI.Label("A LITTLE PRACTICE. A LOT MORE CONFIDENCE.", heading, 14, GameUI.Mint);
            GameUI.Size(eyebrow.rectTransform, 22);
            Text title = GameUI.Label("Street Match (N & S): OK County — Study Aid", heading, 36);
            title.fontStyle = FontStyle.Bold;
            GameUI.Size(title.rectTransform, 42);
            stats = GameUI.Label("", header, 18, GameUI.Muted, TextAnchor.MiddleRight);
            GameUI.FlexWidth(stats.rectTransform);
            soundButton = GameUI.Button(muted ? "Sound off" : "Sound on", header, ToggleSound);
            GameUI.Size((RectTransform)soundButton.transform, width: 100);
            Button menuButton = GameUI.Button("Menu", header, ShowGameMenu);
            GameUI.Size((RectTransform)menuButton.transform, width: 100);

            RectTransform navigation = GameUI.Row("Levels", root, 8);
            GameUI.Size(navigation, 48);
            for (int i = 1; i <= 8; i++)
            {
                int target = i;
                Button button = GameUI.Button("Level " + i, navigation, () => ShowLevel(target));
                GameUI.FlexWidth((RectTransform)button.transform);
                levelButtons.Add(button);
            }
            Button reviewButton = GameUI.Button("Smart review", navigation, ShowReview,
                GameUI.Lavender, GameUI.Ink);
            GameUI.FlexWidth((RectTransform)reviewButton.transform, 1.6f);
            mixButton = GameUI.Button("Mix all 40", navigation, StartMix, GameUI.Mint, GameUI.Ink);
            GameUI.FlexWidth((RectTransform)mixButton.transform, 1.6f);

            Text instructions = GameUI.Label(
                "Drag a name onto its number, or a number onto its name. You can also tap two cards to match.",
                root, 19, GameUI.Muted, TextAnchor.MiddleCenter);
            GameUI.Size(instructions.rectTransform, 38);
            content = GameUI.Rect("Page", root);
            GameUI.Size(content, flex: 1);
            RectTransform footer = GameUI.Column("Feedback", root, 0, 0);
            GameUI.Size(footer, 64);
            feedback = GameUI.Label("", footer, 21, GameUI.Mint, TextAnchor.MiddleCenter);
            GameUI.Size(feedback.rectTransform, 34);
            storageStatus = GameUI.Label("", footer, 14, GameUI.Muted, TextAnchor.MiddleCenter);
            GameUI.Size(storageStatus.rectTransform, 24);
            UpdateHeader();
        }

        private void Update()
        {
            if (splashScreen != null)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetKeyDown(KeyCode.Escape))
                    DismissSplashScreen();
                return;
            }
            if (gameMenu != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseGameMenu();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ClearSelection();
            }
            if (Input.GetKeyDown(KeyCode.H) && round != null && !round.Complete) Hint();
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
                (nameInput == null || !nameInput.isFocused) &&
                primary != null && primary.isActiveAndEnabled && primary.interactable)
                primary.onClick.Invoke();
        }

        private void ShowSplashScreen()
        {
            Texture2D splashTexture = Resources.Load<Texture2D>("StreetMatchSplash");
            if (splashTexture == null)
                throw new InvalidOperationException("The Street Match splash artwork is missing from Resources.");

            splashScreen = GameUI.Rect("Splash Screen", canvasRect);
            GameUI.Stretch(splashScreen);
            GameUI.Surface(splashScreen, GameUI.Ink, false);
            RectTransform artwork = GameUI.Rect("Splash Artwork", splashScreen);
            RawImage background = artwork.gameObject.AddComponent<RawImage>();
            background.texture = splashTexture;
            background.raycastTarget = false;
            var aspect = artwork.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)splashTexture.width / splashTexture.height;

            RectTransform startButtonRect = GameUI.Rect("Start Game", splashScreen);
            GameUI.Stretch(startButtonRect);
            Image clickSurface = startButtonRect.gameObject.AddComponent<Image>();
            clickSurface.color = new Color(0, 0, 0, 0);
            Button startButton = startButtonRect.gameObject.AddComponent<Button>();
            startButton.targetGraphic = clickSurface;
            startButton.transition = Selectable.Transition.None;
            startButton.onClick.AddListener(DismissSplashScreen);

            Text startLabel = GameUI.Label("TAP OR CLICK TO START", startButtonRect, 22,
                GameUI.Mint, TextAnchor.MiddleCenter);
            startLabel.rectTransform.anchorMin = startLabel.rectTransform.anchorMax = new Vector2(.5f, 0);
            startLabel.rectTransform.pivot = new Vector2(.5f, 0);
            startLabel.rectTransform.anchoredPosition = new Vector2(0, 18);
            startLabel.rectTransform.sizeDelta = new Vector2(500, 48);
        }

        private void DismissSplashScreen()
        {
            if (splashScreen == null) return;
            Destroy(splashScreen.gameObject);
            splashScreen = null;
        }

        private void ShowGameMenu()
        {
            if (gameMenu != null) return;

            gameMenu = GameUI.Rect("Game Menu", canvasRect);
            GameUI.Stretch(gameMenu);
            GameUI.Surface(gameMenu, new Color32(10, 14, 27, 235), false);

            RectTransform panel = GameUI.Column("Menu Panel", gameMenu, 24, 14);
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(460, 420);
            GameUI.Surface(panel, GameUI.Panel);

            Text title = GameUI.Label("Game Menu", panel, 32, GameUI.White, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;
            GameUI.Size(title.rectTransform, 46);
            Text nameLabel = GameUI.Label("Your name", panel, 18, GameUI.White);
            GameUI.Size(nameLabel.rectTransform, 28);
            RectTransform inputRect = GameUI.Rect("Name Input", panel);
            GameUI.Surface(inputRect, GameUI.Card);
            nameInput = inputRect.gameObject.AddComponent<InputField>();
            nameInput.targetGraphic = inputRect.GetComponent<Image>();
            nameInput.characterLimit = 32;
            nameInput.lineType = InputField.LineType.SingleLine;
            nameInput.textComponent = GameUI.Label("", inputRect, 20, GameUI.White);
            GameUI.Stretch(nameInput.textComponent.rectTransform, 12);
            Text placeholder = GameUI.Label("Type your name", inputRect, 20, GameUI.Muted);
            GameUI.Stretch(placeholder.rectTransform, 12);
            nameInput.placeholder = placeholder;
            nameInput.text = playerName;
            nameInput.onEndEdit.AddListener(OnPlayerNameEdited);
            GameUI.Size(inputRect, 48);
            menuStatus = GameUI.Label("Your name and progress are saved on this device.", panel, 17,
                GameUI.Muted, TextAnchor.MiddleCenter);
            GameUI.Size(menuStatus.rectTransform, 42);
            GameUI.Button("Return to game", panel, CloseGameMenu, GameUI.Mint, GameUI.Ink);
            GameUI.Button("Exit game", panel, ExitGame, GameUI.Card, GameUI.White);
        }

        private void CloseGameMenu()
        {
            if (gameMenu == null) return;
            SavePlayerName();
            Destroy(gameMenu.gameObject);
            gameMenu = null;
            nameInput = null;
            menuStatus = null;
        }

        private void OnPlayerNameEdited(string value)
        {
            playerName = value.Trim();
            if (nameInput != null && nameInput.text != playerName)
                nameInput.SetTextWithoutNotify(playerName);
            SavePlayerName();
        }

        private void SavePlayerName()
        {
            if (nameInput != null) playerName = nameInput.text.Trim();
            try
            {
                PlayerPrefs.SetString("SherrysGame.PlayerName", playerName);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException exception)
            {
                storageWarning = "Name could not be saved on this device.";
                Debug.LogWarning(storageWarning + " " + exception.Message);
                UpdateHeader();
            }
        }

        private void ExitGame()
        {
            SavePlayerName();
#if UNITY_WEBGL && !UNITY_EDITOR
            menuStatus.text = "To exit the game, close this browser tab.";
#else
            Application.Quit();
#endif
        }

        private void ShowLevel(int target)
        {
            if (target > progress.UnlockedLevel) return;
            mode = Mode.Level;
            level = target;
            roundIds = RoundPlanner.ForLevel(level, progress, DateTime.UtcNow, random);
            ShowPreview();
        }

        private void ShowReview()
        {
            mode = Mode.Review;
            roundIds = RoundPlanner.ForReview(progress, DateTime.UtcNow, random);
            ShowPreview();
        }

        private void StartMix()
        {
            if (progress.UnlockedLevel < 8) return;
            mode = Mode.Mix;
            mixDeck = RoundPlanner.ForMix(random);
            mixPage = 0;
            ShowMixPage();
        }

        private void ShowMixPage()
        {
            roundIds = mixDeck.GetRange(mixPage * 8, 8);
            ShowPreview();
        }

        private string RoundTitle => mode == Mode.Level ? "Level " + level + " / 8" :
            mode == Mode.Review ? "Smart review" : "Full mix / " + (mixPage + 1) + " of 5";

        private void ClearPage()
        {
            StopAllCoroutines();
            ClearSelection();
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            dragged = null;
            primary = null;
            progressFill = null;
            roundStatus = null;
            cards.Clear();
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                GameObject child = content.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }

        private RectTransform Page()
        {
            RectTransform page = GameUI.Column("Page Surface", content, 22, 10);
            GameUI.Stretch(page);
            GameUI.Surface(page, GameUI.Panel);
            return page;
        }

        private void ShowPreview()
        {
            ClearPage();
            round = null;
            RectTransform page = Page();
            Text title = GameUI.Label(RoundTitle + "  /  Learn the pairs", page, 28, GameUI.White);
            title.fontStyle = FontStyle.Bold;
            GameUI.Size(title.rectTransform, 38);
            string description = mode == Mode.Level
                ? "Five new pairs" + (level > 1 ? " + earlier pairs to keep them fresh." : ". Take a moment to notice the pattern.")
                : mode == Mode.Review
                    ? "Due and difficult pairs come first. Try to recall the number before reading it."
                    : "All 40 pairs, in five shuffled boards. Each pair appears once in this mix.";
            Text subtitle = GameUI.Label(description, page, 19, GameUI.Muted);
            GameUI.Size(subtitle.rectTransform, 32);
            RectTransform list = GameUI.Column("Study List", page, 0, 5);
            GameUI.Size(list, flex: 1);
            foreach (int id in roundIds)
            {
                RectTransform row = GameUI.Row("Study Pair", list, 14);
                GameUI.Surface(row, GameUI.Card);
                GameUI.Size(row, 54, 1).minHeight = 32;
                Text key = GameUI.Label("  " + PairCatalog.All[id].Key, row, 23);
                GameUI.FlexWidth(key.rectTransform, 2);
                Text value = GameUI.Label(PairCatalog.All[id].Value.ToString(), row, 25, GameUI.Lavender);
                value.fontStyle = FontStyle.Bold;
                GameUI.FlexWidth(value.rectTransform);
                string state = progress.Pairs[id].Streak >= 3 ? "MASTERED" :
                    progress.Pairs[id].Attempts == 0 ? "NEW" : "PRACTICING";
                Text badge = GameUI.Label(state, row, 13,
                    state == "MASTERED" ? GameUI.Mint : GameUI.Muted, TextAnchor.MiddleCenter);
                GameUI.FlexWidth(badge.rectTransform, .8f);
            }
            primary = GameUI.Button("Ready? Start matching", page, BeginRound, GameUI.Mint, GameUI.Ink);
            GameUI.Size((RectTransform)primary.transform, 50);
            SetFeedback("No timer. No pressure. One point for each correct match.", GameUI.Mint);
            UpdateHeader();
        }

        private void BeginRound()
        {
            ClearPage();
            round = new MatchRound(roundIds, progress);
            RectTransform page = Page();
            RectTransform top = GameUI.Row("Round Heading", page);
            GameUI.Size(top, 48);
            roundStatus = GameUI.Label("", top, 24);
            GameUI.FlexWidth(roundStatus.rectTransform, 3);
            Button hint = GameUI.Button("Hint (H)", top, Hint);
            GameUI.FlexWidth((RectTransform)hint.transform);
            Button study = GameUI.Button("Study again", top, ShowPreview);
            GameUI.FlexWidth((RectTransform)study.transform);

            RectTransform track = GameUI.Rect("Progress Track", page);
            GameUI.Size(track, 6);
            GameUI.Surface(track, GameUI.Card);
            RectTransform fill = GameUI.Rect("Progress Fill", track);
            GameUI.Stretch(fill);
            progressFill = GameUI.Surface(fill, GameUI.Mint);
            progressFill.raycastTarget = false;

            RectTransform board = GameUI.Row("Match Board", page, 24);
            GameUI.Size(board, flex: 1);
            RectTransform keys = GameUI.Column("Keys", board, 0, 9);
            RectTransform values = GameUI.Column("Values", board, 0, 9);
            GameUI.FlexWidth(keys);
            GameUI.FlexWidth(values);
            Text keyHeader = GameUI.Label("NAMES / DRAG ME", keys, 15, GameUI.Mint);
            Text valueHeader = GameUI.Label("VALUES / DRAG ME TOO", values, 15, GameUI.Lavender);
            GameUI.Size(keyHeader.rectTransform, 26);
            GameUI.Size(valueHeader.rectTransform, 26);
            var keyOrder = new List<int>(roundIds);
            var valueOrder = new List<int>(roundIds);
            RoundPlanner.Shuffle(keyOrder, random);
            RoundPlanner.Shuffle(valueOrder, random);
            foreach (int id in keyOrder) MakeCard(keys, id, true);
            foreach (int id in valueOrder) MakeCard(values, id, false);
            SetFeedback("Match from memory. Misses stay on the board so you can try again.", GameUI.Muted);
            UpdateRoundStatus();
        }

        private void MakeCard(Transform parent, int id, bool isKey)
        {
            RectTransform rect = GameUI.Rect(isKey ? "Key " + id : "Value " + id, parent);
            MatchCard card = rect.gameObject.AddComponent<MatchCard>();
            card.Initialize(this, id, isKey);
            cards.Add(card);
        }

        public void Select(MatchCard card)
        {
            if (round == null || round.Complete || card.Matched) return;
            if (selected == card)
            {
                ClearSelection();
                return;
            }
            if (selected != null && selected.IsKey != card.IsKey)
            {
                MatchCard other = selected;
                ClearSelection();
                Match(other, card);
                return;
            }
            ClearSelection();
            selected = card;
            card.SetSelected(true);
            SetFeedback(card.IsKey ? "Now choose its number." : "Now choose its name.", GameUI.Muted);
        }

        private void ClearSelection()
        {
            if (selected != null) selected.SetSelected(false);
            selected = null;
        }

        public void Match(MatchCard first, MatchCard second)
        {
            if (round == null || first.Matched || second.Matched || first.IsKey == second.IsKey) return;
            MatchCard key = first.IsKey ? first : second;
            MatchCard value = first.IsKey ? second : first;
            MatchResult result = round.TryMatch(key.PairId, value.PairId, DateTime.UtcNow);
            ClearSelection();
            if (result == MatchResult.Ignored) return;
            if (result == MatchResult.Wrong)
            {
                key.Reject();
                value.Reject();
                Play(wrongSound);
                SetFeedback("Not quite. Try another number, or use a hint to learn this pair.", GameUI.Coral);
            }
            else
            {
                key.Disappear();
                value.Disappear();
                Play(correctSound);
                SetFeedback("+1   " + PairCatalog.All[key.PairId].Key + " = " +
                    PairCatalog.All[key.PairId].Value, GameUI.Mint);
                if (round.Complete && mode == Mode.Level)
                    progress.UnlockedLevel = Math.Max(progress.UnlockedLevel, Math.Min(8, level + 1));
            }
            Save();
            UpdateRoundStatus();
            if (round.Complete) StartCoroutine(CompleteAfterAnimation());
        }

        private void Hint()
        {
            if (round == null || round.Complete) return;
            MatchCard key = selected != null && selected.IsKey ? selected : null;
            if (key == null)
            {
                foreach (MatchCard candidate in cards)
                {
                    if (!candidate.IsKey || candidate.Matched) continue;
                    if (selected != null && !selected.IsKey &&
                        PairCatalog.All[candidate.PairId].Value != PairCatalog.All[selected.PairId].Value) continue;
                    if (key == null || progress.Pairs[candidate.PairId].Streak < progress.Pairs[key.PairId].Streak)
                        key = candidate;
                }
            }
            if (key == null || !round.Hint(key.PairId)) return;
            int number = PairCatalog.All[key.PairId].Value;
            foreach (MatchCard card in cards)
                card.SetHinted(!card.Matched && (card == key || !card.IsKey && PairCatalog.All[card.PairId].Value == number));
            ClearSelection();
            selected = key;
            key.SetSelected(true);
            SetFeedback("Remember: " + PairCatalog.All[key.PairId].Key + " = " + number +
                ". Highlighted numbers are valid matches.", new Color32(255, 211, 131, 255));
            Save();
            UpdateRoundStatus();
        }

        private IEnumerator CompleteAfterAnimation()
        {
            yield return new WaitForSecondsRealtime(.4f);
            ShowComplete();
        }

        private void ShowComplete()
        {
            int points = round.Points;
            int count = round.Count;
            int unaided = round.Unaided;
            int mistakes = round.Mistakes;
            int hints = round.Hints;
            ClearPage();
            RectTransform page = Page();
            GameUI.Size(GameUI.Rect("Top Space", page), flex: 1);
            Text kicker = GameUI.Label("ROUND COMPLETE", page, 19, GameUI.Mint, TextAnchor.MiddleCenter);
            GameUI.Size(kicker.rectTransform, 32);
            string greeting = string.IsNullOrEmpty(playerName) ? "Nicely done!" : "Nicely done, " + playerName + "!";
            Text heading = GameUI.Label(greeting, page, 46, GameUI.White, TextAnchor.MiddleCenter);
            heading.fontStyle = FontStyle.Bold;
            GameUI.Size(heading.rectTransform, 72);
            Text score = GameUI.Label("+" + points + " points", page, 56, GameUI.Mint, TextAnchor.MiddleCenter);
            GameUI.Size(score.rectTransform, 80);
            Text summary = GameUI.Label(unaided + " / " + count + " from memory   |   " +
                mistakes + " misses   |   " + hints + " hints", page, 24, GameUI.Muted, TextAnchor.MiddleCenter);
            GameUI.Size(summary.rectTransform, 48);
            string message = unaided == count ? "Every pair recalled without help. That's how confidence grows." :
                "Your tricky pairs will return in smart review. Every repetition helps.";
            Text encouragement = GameUI.Label(message, page, 22, GameUI.White, TextAnchor.MiddleCenter);
            GameUI.Size(encouragement.rectTransform, 60);
            bool mixFinished = mode == Mode.Mix && mixPage == 4;
            string nextLabel = mode == Mode.Level && level < 8 ? "Continue to level " + (level + 1) :
                mode == Mode.Mix && !mixFinished ? "Next board / " + (mixPage + 2) + " of 5" :
                mixFinished ? "All 40 done! Start a new mix" : "Practice tricky pairs";
            primary = GameUI.Button(nextLabel, page, Continue, GameUI.Mint, GameUI.Ink);
            GameUI.Size((RectTransform)primary.transform, 52);
            Button replay = GameUI.Button("Practice these pairs again", page, ShowPreview);
            GameUI.Size((RectTransform)replay.transform, 46);
            GameUI.Size(GameUI.Rect("Bottom Space", page), flex: 1);
            SetFeedback("Mastery = three consecutive unaided recalls of a pair, across separate rounds.", GameUI.Mint);
            UpdateHeader();
        }

        private void Continue()
        {
            if (mode == Mode.Level && level < 8) ShowLevel(level + 1);
            else if (mode == Mode.Mix && mixPage < 4)
            {
                mixPage++;
                ShowMixPage();
            }
            else if (mode == Mode.Mix) StartMix();
            else ShowReview();
        }

        public void BeginDrag(MatchCard card, string caption, PointerEventData eventData)
        {
            ClearSelection();
            dragged = card;
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = GameUI.Rect("Dragged Card", canvasRect);
            ghost.sizeDelta = new Vector2(Mathf.Min(((RectTransform)card.transform).rect.width, 440), 72);
            GameUI.Surface(ghost, card.IsKey ? GameUI.Mint : GameUI.Lavender);
            var group = ghost.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = .96f;
            Text label = GameUI.Label(caption, ghost, 25, GameUI.Ink, TextAnchor.MiddleCenter);
            GameUI.Stretch(label.rectTransform, 12);
            ghost.SetAsLastSibling();
            MoveDrag(eventData);
        }

        public void MoveDrag(PointerEventData eventData)
        {
            if (ghost == null) return;
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                eventData.position, eventData.pressEventCamera, out local))
                ghost.anchoredPosition = local;
        }

        public void EndDrag(MatchCard card)
        {
            if (dragged != card) return;
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            dragged = null;
        }

        private void UpdateRoundStatus()
        {
            if (roundStatus != null)
                roundStatus.text = RoundTitle + "   |   " + round.Points + " / " + round.Count + " matched";
            if (progressFill != null)
            {
                RectTransform rect = progressFill.rectTransform;
                rect.anchorMax = new Vector2((float)round.Points / round.Count, 1);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var scaler = GetComponentInParent<CanvasScaler>();
                if (scaler != null && safe.width > 0 && safe.height > 0)
                {
                    // Reserve the reference layout inside the usable screen, not behind cutouts.
                    scaler.referenceResolution = new Vector2(
                        1440f * size.x / safe.width, 1000f * size.y / safe.height);
                }
            }
            UpdateHeader();
        }

        private void UpdateHeader()
        {
            stats.text = "ALL-TIME  " + progress.TotalPoints + " points\nMASTERED  " + progress.MasteredCount + " / 40";
            for (int i = 0; i < levelButtons.Count; i++)
            {
                Button button = levelButtons[i];
                button.interactable = i + 1 <= progress.UnlockedLevel;
                button.GetComponent<Image>().color = mode == Mode.Level && i + 1 == level ? GameUI.Mint : GameUI.Card;
                button.GetComponentInChildren<Text>().color = mode == Mode.Level && i + 1 == level ? GameUI.Ink : GameUI.White;
            }
            mixButton.interactable = progress.UnlockedLevel == 8;
            if (!string.IsNullOrEmpty(storageWarning) || !string.IsNullOrEmpty(loadWarning))
            {
                storageStatus.text = !string.IsNullOrEmpty(storageWarning) ? storageWarning : loadWarning;
                storageStatus.color = GameUI.Coral;
            }
            else
            {
                storageStatus.text = progress.UnlockedLevel == 8
                    ? "Progress saved on this device. Same-number cards are interchangeable."
                    : "Progress saved on this device. Finish each level to unlock the next; Mix all 40 unlocks with level 8.";
                storageStatus.color = GameUI.Muted;
            }
        }

        private void SetFeedback(string message, Color color)
        {
            feedback.text = message;
            feedback.color = color;
        }

        private void Save()
        {
            GameSave.Save(progress, out storageWarning);
            UpdateHeader();
        }

        private void ToggleSound()
        {
            muted = !muted;
            soundButton.GetComponentInChildren<Text>().text = muted ? "Sound off" : "Sound on";
            try
            {
                PlayerPrefs.SetInt("SherrysGame.Muted", muted ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException exception)
            {
                storageWarning = "Sound preference could not be saved on this device.";
                Debug.LogWarning(storageWarning + " " + exception.Message);
                UpdateHeader();
            }
        }

        private void Play(AudioClip clip)
        {
            if (!muted) audioSource.PlayOneShot(clip, .12f);
        }

        private static AudioClip Tone(float first, float second)
        {
            const int sampleRate = 22050;
            const int length = 4410;
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Min(1, t * 80) * (1 - (float)i / length);
                samples[i] = (Mathf.Sin(t * first * Mathf.PI * 2) +
                    Mathf.Sin(t * second * Mathf.PI * 2) * .5f) * envelope * .5f;
            }
            AudioClip clip = AudioClip.Create("Match tone", length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && progress != null)
            {
                Save();
                SavePlayerName();
            }
        }

        private void OnApplicationQuit()
        {
            if (progress != null)
            {
                GameSave.Save(progress, out storageWarning);
                SavePlayerName();
            }
        }

        private void OnDestroy()
        {
            if (correctSound != null) Destroy(correctSound);
            if (wrongSound != null) Destroy(wrongSound);
        }
    }

    internal sealed class ScreenSafeArea : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int previousSize;

        private void Update()
        {
            Rect safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == previous && size == previousSize || size.x == 0 || size.y == 0) return;
            previous = safe;
            previousSize = size;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
            rect.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
