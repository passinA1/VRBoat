using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static GameManager;
using System.Linq;
using System;

public class LevelRecordManager : MonoBehaviour
{
    [Header("配置")]
    public int targetLevel = 1;

    public GameObject contentPrefab; // 预制件
    public GameObject emptyRecordPrefab;
    public Transform contentParent; // Content 的 Transform
    private List<GameRecord> levelRecords = new List<GameRecord>(); // 存储关卡记录

    void Start()
    {   
        DontDestroyOnLoad(this);
        LoadRecords(); // 从 PlayerPrefs 加载记录
        DisplayTopRecords(5); // 显示前五名记录
    }

    private void LoadRecords()
    {
        levelRecords.Clear();

        // 从GameManager获取数据（需添加引用）
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            List<GameRecord> allRecords = gameManager.LoadAllRecords();

            // 筛选当前关卡记录（假设currentStage从其他途径获取）
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

            // 使用更健壮的组件查找方式
            TextMeshProUGUI[] texts = instance.GetComponentsInChildren<TextMeshProUGUI>();
            texts.FirstOrDefault(t => t.name == "RankText").text = $"{i + 1}";
            texts.FirstOrDefault(t => t.name == "ScoreText").text = record.score.ToString();
            texts.FirstOrDefault(t => t.name == "TimeText").text = $"Time: {record.time:F1}";
            
        }
    }

    public void ClearLocalRecords()
    {
        // 清空内存记录
        levelRecords.Clear();

        // 立即刷新UI
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 显示空状态提示
        Instantiate(emptyRecordPrefab, contentParent); // 需要提前创建空状态提示预制件
    }
}

    // 定义关卡记录结构
    [System.Serializable]
    public class LevelRecord
    {
        public int levelIndex;   // 关卡编号
        public int score;    // 最高分
        public float time;   // 最佳时间

        public LevelRecord(int levelIndex, int score, float time)
        {
            this.levelIndex = levelIndex;
            this.score = score;
            this.time = time;
        }
    }

