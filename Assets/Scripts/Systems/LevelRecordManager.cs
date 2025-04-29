using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static GameManager;
using System.Linq;
using System;

public class LevelRecordManager : MonoBehaviour
{
    [Header("����")]
    public int targetLevel = 1;

    public GameObject contentPrefab; // Ԥ�Ƽ�
    public GameObject emptyRecordPrefab;
    public Transform contentParent; // Content �� Transform
    private List<GameRecord> levelRecords = new List<GameRecord>(); // �洢�ؿ���¼

    void Start()
    {   
        DontDestroyOnLoad(this);
        LoadRecords(); // �� PlayerPrefs ���ؼ�¼
        DisplayTopRecords(5); // ��ʾǰ������¼
    }

    private void LoadRecords()
    {
        levelRecords.Clear();

        // ��GameManager��ȡ���ݣ���������ã�
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            List<GameRecord> allRecords = gameManager.LoadAllRecords();

            // ɸѡ��ǰ�ؿ���¼������currentStage������;����ȡ��
            levelRecords = allRecords
                .Where(r => r.levelIndex == targetLevel)
                .OrderByDescending(r => r.score)
                .ThenBy(r => r.time)
                .ToList();
        }
    }




    private void DisplayTopRecords(int topCount)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < Mathf.Min(topCount, levelRecords.Count); i++)
        {
            GameRecord record = levelRecords[i];
            GameObject instance = Instantiate(contentPrefab, contentParent);

            // ʹ�ø���׳��������ҷ�ʽ
            TextMeshProUGUI[] texts = instance.GetComponentsInChildren<TextMeshProUGUI>();
            texts.FirstOrDefault(t => t.name == "RankText").text = $"{i + 1}";
            texts.FirstOrDefault(t => t.name == "ScoreText").text = record.score.ToString();
            texts.FirstOrDefault(t => t.name == "TimeText").text = $"Time: {record.time:F1}";
            
        }
    }

    public void ClearLocalRecords()
    {
        // ����ڴ��¼
        levelRecords.Clear();

        // ����ˢ��UI
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // ��ʾ��״̬��ʾ
        Instantiate(emptyRecordPrefab, contentParent); // ��Ҫ��ǰ������״̬��ʾԤ�Ƽ�
    }
}

    // ����ؿ���¼�ṹ
    [System.Serializable]
    public class LevelRecord
    {
        public int levelIndex;   // �ؿ����
        public int score;    // ��߷�
        public float time;   // ���ʱ��

        public LevelRecord(int levelIndex, int score, float time)
        {
            this.levelIndex = levelIndex;
            this.score = score;
            this.time = time;
        }
    }

