using System;
using System.Collections.Generic;

namespace StreetMatch.Tests
{
    public static class GameRulesSuite
    {
        private static int assertions;
        private static readonly DateTime Now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

        public static int RunAll()
        {
            assertions = 0;
            CatalogIsExact();
            LevelsAndMixCoverEveryPair();
            DuplicateValuesAreInterchangeable();
            WrongMatchesCannotScoreOrDisappear();
            HintsDoNotCountAsUnaidedRecall();
            MasteryNeedsConsecutiveUnaidedRounds();
            ReviewPrioritizesDueAndWeakPairs();
            ProgressValidationRejectsInvalidSaves();
            InvalidRoundsAreRejected();
            return assertions;
        }

        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void CatalogIsExact()
        {
            string[] names = {
                "Banner", "Gregory", "Cimarron", "Richland", "Frisco",
                "Cemetary/Garth Brooks", "Czech", "Sara", "Morgan", "County Line",
                "Council", "Rockwell", "Macarthur", "Meridian", "Portland",
                "May", "Penn", "Western", "Santa Fe", "Kelly/High",
                "MLK/Eastern", "Bryant", "Sunnylane/Coltraine", "Sooner", "Air Depot",
                "Midwest Blv", "Douglas", "Post", "Westministe", "Anderson",
                "Hiwassee", "Henney", "Choctaw", "Indian/Meridian", "Triple X",
                "Peebly", "Luther", "Dobbs", "Harrah/Newalla", "Pottawatom"
            };
            int[] values = {
                17000, 16200, 15400, 14500, 13800, 13000, 12200, 10500, 9800, 9000,
                8000, 7000, 5900, 4500, 3700, 2900, 2000, 1000, 0, 900,
                2000, 3000, 4300, 5600, 7000, 8000, 9000, 10000, 11000, 12000,
                13000, 14000, 15000, 16000, 17000, 18000, 19000, 10000, 21000, 12000
            };
            Check(PairCatalog.All.Length == 40, "Exactly 40 supplied pairs.");
            for (int i = 0; i < 40; i++)
            {
                Check(PairCatalog.All[i].Id == i, "Stable unique pair IDs.");
                Check(PairCatalog.All[i].Key == names[i], "Exact supplied label at " + i);
                Check(PairCatalog.All[i].Value == values[i], "Exact supplied number at " + i);
            }
        }

        private static void LevelsAndMixCoverEveryPair()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var progress = new LearningProgress();
                var random = new Random(seed);
                var introduced = new HashSet<int>();
                for (int level = 1; level <= 8; level++)
                {
                    List<int> ids = RoundPlanner.ForLevel(level, progress, Now, random);
                    Check(ids.Count == 5 + Math.Min(3, level - 1), "Five new pairs plus bounded review.");
                    Check(new HashSet<int>(ids).Count == ids.Count, "No duplicated pair IDs in a round.");
                    int first = (level - 1) * 5;
                    for (int id = first; id < first + 5; id++)
                    {
                        Check(ids.Contains(id), "Every new pair appears in its level.");
                        introduced.Add(id);
                    }
                    foreach (int id in ids) Check(id < level * 5, "No future pair leaks into a level.");
                    var round = new MatchRound(ids, progress);
                    foreach (int id in ids)
                        Check(round.TryMatch(id, id, Now) == MatchResult.Correct, "A planned level is solvable.");
                    Check(round.Complete && round.Points == ids.Count, "All level points awarded exactly once.");
                }
                Check(introduced.Count == 40, "Eight levels introduce all 40 pairs.");
                List<int> mix = RoundPlanner.ForMix(random);
                Check(mix.Count == 40 && new HashSet<int>(mix).Count == 40, "Full mix covers every pair once.");
                for (int page = 0; page < 5; page++)
                {
                    var round = new MatchRound(mix.GetRange(page * 8, 8), progress);
                    for (int i = page * 8; i < page * 8 + 8; i++)
                        Check(round.TryMatch(mix[i], mix[i], Now) == MatchResult.Correct, "Every mix board is solvable.");
                    Check(round.Complete && round.Points == 8, "Each mix board awards eight points.");
                }
                for (int unlocked = 1; unlocked <= 8; unlocked++)
                {
                    progress.UnlockedLevel = unlocked;
                    List<int> review = RoundPlanner.ForReview(progress, Now, random);
                    Check(review.Count == Math.Min(8, unlocked * 5), "Review size is bounded.");
                    foreach (int id in review) Check(id < unlocked * 5, "Review only uses unlocked pairs.");
                }
            }
        }

        private static void DuplicateValuesAreInterchangeable()
        {
            var ids = new List<int>();
            for (int i = 0; i < 40; i++) ids.Add(i);
            var progress = new LearningProgress();
            var round = new MatchRound(ids, progress);
            var remainingValues = new List<int>(ids);
            foreach (int keyId in ids)
            {
                int valueId = -1;
                // Prefer a different pair's token to exercise all duplicate groups.
                for (int i = remainingValues.Count - 1; i >= 0; i--)
                {
                    int candidate = remainingValues[i];
                    if (PairCatalog.All[candidate].Value == PairCatalog.All[keyId].Value)
                    {
                        valueId = candidate;
                        remainingValues.RemoveAt(i);
                        break;
                    }
                }
                Check(valueId >= 0, "A matching visible value remains.");
                Check(round.TryMatch(keyId, valueId, Now) == MatchResult.Correct, "Duplicate token is accepted.");
                Check(!round.HasKey(keyId) && !round.HasValue(valueId), "Only the matched tokens disappear.");
            }
            Check(round.Complete && round.Points == 40 && progress.TotalPoints == 40, "All duplicate groups remain solvable.");
            Check(progress.Pairs[0].Correct == 1 && progress.Pairs[34].Correct == 1, "Recall belongs to the key, not value token.");
        }

        private static void WrongMatchesCannotScoreOrDisappear()
        {
            var progress = new LearningProgress();
            var round = new MatchRound(new[] { 0, 1 }, progress);
            Check(round.TryMatch(0, 1, Now) == MatchResult.Wrong, "Wrong match rejected.");
            Check(round.Points == 0 && round.Mistakes == 1 && round.Remaining == 2, "No point or removal on error.");
            Check(round.HasKey(0) && round.HasValue(1), "Both cards stay available after an error.");
            Check(progress.Pairs[0].Attempts == 1 && progress.Pairs[1].Attempts == 0, "Track the attempted key only.");
            Check(round.TryMatch(0, 0, Now) == MatchResult.Correct, "Correct retry scores.");
            Check(progress.Pairs[0].Streak == 0 && round.Unaided == 0, "Retry isn't unaided.");
            Check(round.TryMatch(0, 0, Now) == MatchResult.Ignored, "No double scoring.");
            Check(round.TryMatch(1, 0, Now) == MatchResult.Ignored, "Consumed value cannot be reused.");
            Check(round.TryMatch(-1, 999, Now) == MatchResult.Ignored, "Invalid stale IDs ignored.");
            Check(round.TryMatch(1, 1, Now) == MatchResult.Correct, "Other pair is still solvable.");
            Check(progress.TotalPoints == 2 && round.Points == 2 && round.Unaided == 1, "Exactly one point per correct pair.");
        }

        private static void HintsDoNotCountAsUnaidedRecall()
        {
            var progress = new LearningProgress();
            var round = new MatchRound(new[] { 18, 19 }, progress);
            Check(round.Hint(18), "Can hint zero-valued pair.");
            Check(!round.Hint(0), "Cannot hint a pair outside this round.");
            Check(round.TryMatch(18, 18, Now) == MatchResult.Correct, "Zero is a valid number.");
            Check(round.Points == 1 && round.Hints == 1 && round.Unaided == 0, "Hints allow points, not unaided credit.");
            Check(progress.Pairs[18].Streak == 0 && progress.Pairs[18].DueUtcTicks == Now.Ticks, "Hinted key is due now.");
            Check(!round.Hint(18), "Cannot hint an already matched pair.");
            Check(round.TryMatch(19, 19, Now) == MatchResult.Correct, "900 remains distinct from zero.");
        }

        private static void MasteryNeedsConsecutiveUnaidedRounds()
        {
            var progress = new LearningProgress();
            long[] intervals = { TimeSpan.TicksPerMinute, 10 * TimeSpan.TicksPerMinute, TimeSpan.TicksPerDay };
            for (int i = 0; i < 3; i++)
            {
                var round = new MatchRound(new[] { 0 }, progress);
                Check(round.TryMatch(0, 0, Now) == MatchResult.Correct, "Separate round recalls.");
                Check(progress.Pairs[0].Streak == i + 1, "Recall streak increments.");
                Check(progress.Pairs[0].DueUtcTicks == Now.Ticks + intervals[i], "Spacing interval is exact.");
                Check(progress.MasteredCount == (i == 2 ? 1 : 0), "Three unaided rounds required for mastery.");
            }
            var mistake = new MatchRound(new[] { 0, 1 }, progress);
            mistake.TryMatch(0, 1, Now);
            Check(progress.MasteredCount == 0 && progress.Pairs[0].Streak == 0, "An error removes mastery.");
            mistake.TryMatch(0, 0, Now);
            Check(progress.Pairs[0].Streak == 0, "Correcting an error doesn't restore mastery.");
            for (int i = 0; i < 3; i++)
                new MatchRound(new[] { 0 }, progress).TryMatch(0, 0, Now);
            var hinted = new MatchRound(new[] { 0 }, progress);
            hinted.Hint(0);
            Check(progress.MasteredCount == 0, "Hinting a mastered pair makes it practicing again.");
            Check(progress.IsValid(), "Normal play always produces valid progress.");
        }

        private static void ReviewPrioritizesDueAndWeakPairs()
        {
            var progress = new LearningProgress { UnlockedLevel = 8 };
            for (int i = 0; i < 40; i++)
            {
                progress.Pairs[i].Streak = 4;
                progress.Pairs[i].Attempts = 4;
                progress.Pairs[i].Correct = 4;
                progress.Pairs[i].DueUtcTicks = Now.AddDays(3).Ticks;
            }
            progress.Pairs[39].Streak = 1;
            progress.Pairs[39].DueUtcTicks = Now.AddMinutes(-1).Ticks;
            progress.Pairs[38].Streak = 0;
            progress.Pairs[38].DueUtcTicks = 0;
            List<int> review = RoundPlanner.ForReview(progress, Now, new Random(2));
            Check(review[0] == 38 && review[1] == 39, "Due weak keys precede future mastered keys.");
            progress.Pairs[38].DueUtcTicks = Now.AddDays(5).Ticks;
            review = RoundPlanner.ForReview(progress, Now, new Random(2));
            Check(review[0] == 39, "A due pair beats a future weak pair.");
        }

        private static void ProgressValidationRejectsInvalidSaves()
        {
            Check(new LearningProgress().IsValid(), "New save is valid.");
            Check(!new LearningProgress { Pairs = null }.IsValid(), "Missing records rejected.");
            Check(!new LearningProgress { Pairs = new PairProgress[1] }.IsValid(), "Wrong count rejected.");
            Check(!new LearningProgress { Version = 2 }.IsValid(), "Unsupported version rejected.");
            Check(!new LearningProgress { UnlockedLevel = 9 }.IsValid(), "Invalid level rejected.");
            Check(!new LearningProgress { TotalPoints = -1 }.IsValid(), "Negative total rejected.");
            var progress = new LearningProgress();
            progress.Pairs[0] = null;
            Check(!progress.IsValid(), "Null record rejected.");
            progress = new LearningProgress();
            progress.Pairs[0].Correct = 1;
            Check(!progress.IsValid(), "More correct answers than attempts rejected.");
            progress = new LearningProgress();
            progress.Pairs[0].Streak = 1;
            Check(!progress.IsValid(), "Impossible streak rejected.");
            progress = new LearningProgress();
            progress.Pairs[0].DueUtcTicks = -1;
            Check(!progress.IsValid(), "Invalid date rejected.");
        }

        private static void InvalidRoundsAreRejected()
        {
            int caught = 0;
            foreach (int[] ids in new[] { new int[0], new[] { 0, 0 }, new[] { -1 }, new[] { 40 } })
            {
                try { new MatchRound(ids, new LearningProgress()); }
                catch (ArgumentException) { caught++; }
            }
            Check(caught == 4, "Invalid round inputs explicitly rejected.");
        }
    }
}
