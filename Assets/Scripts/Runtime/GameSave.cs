using System;
using UnityEngine;

namespace SherrysGame
{
    internal static class GameSave
    {
        private const string SaveKey = "SherrysGame.Learning.v1";

        internal static LearningProgress Load(out string warning)
        {
            warning = "";
            if (!PlayerPrefs.HasKey(SaveKey)) return new LearningProgress();
            try
            {
                LearningProgress progress = JsonUtility.FromJson<LearningProgress>(PlayerPrefs.GetString(SaveKey));
                if (progress != null && progress.IsValid()) return progress;
                warning = "Saved progress is invalid. A fresh session has been started.";
            }
            catch (ArgumentException exception)
            {
                warning = "Saved progress could not be read. A fresh session has been started.";
                Debug.LogWarning(warning + " " + exception.Message);
                return new LearningProgress();
            }
            Debug.LogWarning(warning);
            return new LearningProgress();
        }

        internal static bool Save(LearningProgress progress, out string warning)
        {
            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(progress));
                PlayerPrefs.Save();
                warning = "";
                return true;
            }
            catch (PlayerPrefsException exception)
            {
                warning = "Progress could not be saved on this device. Keep this session open.";
                Debug.LogWarning(warning + " " + exception.Message);
                return false;
            }
        }
    }
}
