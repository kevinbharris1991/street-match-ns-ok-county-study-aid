using System;
using System.Collections.Generic;

namespace StreetMatch
{
    [Serializable]
    public sealed class Pair
    {
        public int Id;
        public string Key;
        public int Value;

        public Pair(int id, string key, int value)
        {
            Id = id;
            Key = key;
            Value = value;
        }
    }

    public static class PairCatalog
    {
        // Labels and numbers intentionally preserve the supplied list.
        public static readonly Pair[] All =
        {
            new Pair(0, "Banner", 17000),
            new Pair(1, "Gregory", 16200),
            new Pair(2, "Cimarron", 15400),
            new Pair(3, "Richland", 14500),
            new Pair(4, "Frisco", 13800),
            new Pair(5, "Cemetary/Garth Brooks", 13000),
            new Pair(6, "Czech", 12200),
            new Pair(7, "Sara", 10500),
            new Pair(8, "Morgan", 9800),
            new Pair(9, "County Line", 9000),
            new Pair(10, "Council", 8000),
            new Pair(11, "Rockwell", 7000),
            new Pair(12, "Macarthur", 5900),
            new Pair(13, "Meridian", 4500),
            new Pair(14, "Portland", 3700),
            new Pair(15, "May", 2900),
            new Pair(16, "Penn", 2000),
            new Pair(17, "Western", 1000),
            new Pair(18, "Santa Fe", 0),
            new Pair(19, "Kelly/High", 900),
            new Pair(20, "MLK/Eastern", 2000),
            new Pair(21, "Bryant", 3000),
            new Pair(22, "Sunnylane/Coltraine", 4300),
            new Pair(23, "Sooner", 5600),
            new Pair(24, "Air Depot", 7000),
            new Pair(25, "Midwest Blv", 8000),
            new Pair(26, "Douglas", 9000),
            new Pair(27, "Post", 10000),
            new Pair(28, "Westministe", 11000),
            new Pair(29, "Anderson", 12000),
            new Pair(30, "Hiwassee", 13000),
            new Pair(31, "Henney", 14000),
            new Pair(32, "Choctaw", 15000),
            new Pair(33, "Indian/Meridian", 16000),
            new Pair(34, "Triple X", 17000),
            new Pair(35, "Peebly", 18000),
            new Pair(36, "Luther", 19000),
            new Pair(37, "Dobbs", 10000),
            new Pair(38, "Harrah/Newalla", 21000),
            new Pair(39, "Pottawatom", 12000)
        };
    }

    [Serializable]
    public sealed class PairProgress
    {
        public int Attempts;
        public int Correct;
        public int Streak;
        public long DueUtcTicks;
    }

    [Serializable]
    public sealed class LearningProgress
    {
        public int Version = 1;
        public int UnlockedLevel = 1;
        public int TotalPoints;
        public PairProgress[] Pairs = NewPairs();

        private static PairProgress[] NewPairs()
        {
            var result = new PairProgress[PairCatalog.All.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new PairProgress();
            return result;
        }

        public bool IsValid()
        {
            if (Version != 1 || UnlockedLevel < 1 || UnlockedLevel > 8 ||
                TotalPoints < 0 || Pairs == null || Pairs.Length != PairCatalog.All.Length)
                return false;
            foreach (PairProgress pair in Pairs)
            {
                if (pair == null || pair.Attempts < 0 || pair.Correct < 0 ||
                    pair.Correct > pair.Attempts || pair.Streak < 0 || pair.Streak > pair.Correct ||
                    pair.DueUtcTicks < 0 || pair.DueUtcTicks > DateTime.MaxValue.Ticks)
                    return false;
            }
            return true;
        }

        public int MasteredCount
        {
            get
            {
                int count = 0;
                foreach (PairProgress pair in Pairs)
                    if (pair.Streak >= 3) count++;
                return count;
            }
        }

        public void RecordMistake(int id)
        {
            Pairs[id].Attempts++;
            Pairs[id].Streak = 0;
            Pairs[id].DueUtcTicks = 0;
        }

        public void RecordMatch(int id, bool unaided, DateTime now)
        {
            PairProgress pair = Pairs[id];
            pair.Attempts++;
            pair.Correct++;
            pair.Streak = unaided ? pair.Streak + 1 : 0;
            // Short initial intervals support practice now; later ones support retention.
            double minutes = pair.Streak == 0 ? 0 : pair.Streak == 1 ? 1 :
                pair.Streak == 2 ? 10 : pair.Streak == 3 ? 1440 : 4320;
            pair.DueUtcTicks = now.AddMinutes(minutes).Ticks;
            TotalPoints++;
        }
    }

    public static class RoundPlanner
    {
        public static List<int> ForLevel(int level, LearningProgress progress, DateTime now, Random random)
        {
            if (level < 1 || level > 8) throw new ArgumentOutOfRangeException(nameof(level));
            var selected = new List<int>();
            int first = (level - 1) * 5;
            for (int i = first; i < first + 5; i++) selected.Add(i);
            List<int> previous = Ranked(first, progress, now, random);
            int reviewCount = Math.Min(3, level - 1);
            for (int i = 0; i < reviewCount; i++) selected.Add(previous[i]);
            return selected;
        }

        public static List<int> ForReview(LearningProgress progress, DateTime now, Random random)
        {
            int available = progress.UnlockedLevel * 5;
            List<int> ranked = Ranked(available, progress, now, random);
            return ranked.GetRange(0, Math.Min(8, available));
        }

        public static List<int> ForMix(Random random)
        {
            var result = new List<int>();
            for (int i = 0; i < PairCatalog.All.Length; i++) result.Add(i);
            Shuffle(result, random);
            return result;
        }

        private static List<int> Ranked(int count, LearningProgress progress, DateTime now, Random random)
        {
            var ids = new List<int>();
            for (int i = 0; i < count; i++) ids.Add(i);
            Shuffle(ids, random);
            // List.Sort is not stable; retain shuffled tie order explicitly.
            var tieOrder = new int[count];
            for (int i = 0; i < ids.Count; i++) tieOrder[ids[i]] = i;
            ids.Sort((a, b) =>
            {
                PairProgress left = progress.Pairs[a];
                PairProgress right = progress.Pairs[b];
                bool leftDue = left.DueUtcTicks <= now.Ticks;
                bool rightDue = right.DueUtcTicks <= now.Ticks;
                if (leftDue != rightDue) return leftDue ? -1 : 1;
                int streakOrder = left.Streak.CompareTo(right.Streak);
                if (streakOrder != 0) return streakOrder;
                int dueOrder = left.DueUtcTicks.CompareTo(right.DueUtcTicks);
                return dueOrder != 0 ? dueOrder : tieOrder[a].CompareTo(tieOrder[b]);
            });
            return ids;
        }

        public static void Shuffle<T>(IList<T> items, Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                T temporary = items[i];
                items[i] = items[other];
                items[other] = temporary;
            }
        }
    }

    public enum MatchResult { Ignored, Wrong, Correct }

    public sealed class MatchRound
    {
        private readonly HashSet<int> keys;
        private readonly HashSet<int> values;
        private readonly HashSet<int> assisted = new HashSet<int>();
        private readonly LearningProgress progress;

        public int Points { get; private set; }
        public int Mistakes { get; private set; }
        public int Hints { get; private set; }
        public int Unaided { get; private set; }
        public int Count { get; private set; }
        public int Remaining => keys.Count;
        public bool Complete => keys.Count == 0;

        public MatchRound(IList<int> ids, LearningProgress progress)
        {
            if (ids == null || ids.Count == 0) throw new ArgumentException("A round needs pairs.", nameof(ids));
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            keys = new HashSet<int>();
            foreach (int id in ids)
            {
                if (id < 0 || id >= PairCatalog.All.Length || !keys.Add(id))
                    throw new ArgumentException("Round IDs must be valid and unique.", nameof(ids));
            }
            values = new HashSet<int>(keys);
            Count = keys.Count;
            this.progress = progress;
        }

        public bool HasKey(int id) => keys.Contains(id);
        public bool HasValue(int id) => values.Contains(id);

        public MatchResult TryMatch(int keyId, int valueId, DateTime now)
        {
            if (!keys.Contains(keyId) || !values.Contains(valueId)) return MatchResult.Ignored;
            if (PairCatalog.All[keyId].Value != PairCatalog.All[valueId].Value)
            {
                Mistakes++;
                assisted.Add(keyId);
                progress.RecordMistake(keyId);
                return MatchResult.Wrong;
            }
            keys.Remove(keyId);
            values.Remove(valueId);
            bool unaided = !assisted.Contains(keyId);
            if (unaided) Unaided++;
            progress.RecordMatch(keyId, unaided, now);
            Points++;
            return MatchResult.Correct;
        }

        public bool Hint(int keyId)
        {
            if (!keys.Contains(keyId)) return false;
            assisted.Add(keyId);
            Hints++;
            progress.Pairs[keyId].Streak = 0;
            progress.Pairs[keyId].DueUtcTicks = 0;
            return true;
        }
    }
}
