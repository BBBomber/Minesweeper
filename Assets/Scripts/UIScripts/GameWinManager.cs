using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

public class GameWinManager : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private Transform scoreListContainer; // Parent object for score UI elements
    [SerializeField] private GameObject scoreEntryPrefab; // Prefab for score entry UI
    [SerializeField] private Button gotItButton;

    private const int MAX_SCORES = 10;
    private string[] difficultyNames = { "Easy", "Medium", "Hard", "Expert" };

    private void Start()
    {
        gotItButton.onClick.AddListener(ClosePopup);
    }

    public void ShowPopup(int difficulty, float elapsedTime)
    {
        popupPanel.SetActive(true);
        difficultyText.text = $"{difficultyNames[difficulty]}";

        // Save new score
        List<ScoreEntry> scores = LoadScores(difficulty);
        ScoreEntry newEntry = new ScoreEntry
        {
            date = DateTime.Now.ToString("d MMM yyyy"),
            time = elapsedTime
        };

        // Insert new score and sort
        scores = InsertAndSortScore(scores, newEntry);

        // Save back to PlayerPrefs
        SaveScores(difficulty, scores);

        // Update UI
        UpdateScoreUI(scores, newEntry);
    }

    private List<ScoreEntry> LoadScores(int difficulty)
    {
        string key = $"{difficultyNames[difficulty]}Scores";
        string json = PlayerPrefs.GetString(key, "");

        if (string.IsNullOrEmpty(json))
        {
            return new List<ScoreEntry>(); // Return an empty list if there's no data
        }

        // Correct way to deserialize a list
        ScoreList scoreList = JsonUtility.FromJson<ScoreList>(json);
        return scoreList != null ? scoreList.scores : new List<ScoreEntry>();
    }


    private void SaveScores(int difficulty, List<ScoreEntry> scores)
    {
        string key = $"{difficultyNames[difficulty]}Scores";
        ScoreList scoreList = new ScoreList { scores = scores };
        string json = JsonUtility.ToJson(scoreList);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    private List<ScoreEntry> InsertAndSortScore(List<ScoreEntry> scores, ScoreEntry newEntry)
    {
        scores.Add(newEntry);
        scores = scores.OrderBy(s => s.time).ThenBy(s => s.date).ToList(); // Sort by time, then by date

        if (scores.Count > MAX_SCORES)
        {
            scores.RemoveAt(MAX_SCORES); // Remove worst score if over limit
        }

        return scores;
    }

    private void UpdateScoreUI(List<ScoreEntry> scores, ScoreEntry newEntry)
    {
        // Clear previous entries
        foreach (Transform child in scoreListContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < scores.Count; i++)
        {
            ScoreEntry entry = scores[i];
            GameObject newEntryObj = Instantiate(scoreEntryPrefab, scoreListContainer);

            // Get UI Components
            TextMeshProUGUI orderText = newEntryObj.transform.Find("OrderText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI dateText = newEntryObj.transform.Find("DateText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI timeText = newEntryObj.transform.Find("TimeText").GetComponent<TextMeshProUGUI>();
            Image backgroundImage = newEntryObj.GetComponentInChildren<Image>(); // Highlight background

            // Format time (MM:SS)
            TimeSpan timeSpan = TimeSpan.FromSeconds(entry.time);
            string timeFormatted = $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";

            // Assign text values
            orderText.text = $"{i + 1}.";
            dateText.text = entry.date;
            timeText.text = timeFormatted;

            // Highlight only the current win
            backgroundImage.enabled = (entry == newEntry);
        }
    }



    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}

[Serializable]
public class ScoreEntry
{
    public string date;
    public float time;
}

[Serializable]
public class ScoreList
{
    public List<ScoreEntry> scores;
}
