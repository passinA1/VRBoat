using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class LevelData
{
    public int levelIndex;
    public float bestScore;
    public float bestTime;
    public float lastScore;
    public float lastTime;
}
public class GameManager : MonoBehaviour
{
    [Header("游戏设置")]
    public bool isPaused = false;
    public GameObject pauseMenu;
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("关卡进度")]
    public int currentStage = 1;  // 当前所在关卡
    public int maxStage = 1;      // 最大关卡数量
    public int highScore = 0;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI currentScoreText;
    public float trackProgress;
    public float currentSpeed;
    public Slider progressSlider; // 进度条
    private DragonBoatMovement boatMovement;

    [Header("游戏状态")]
    public float gameTimer = 0f;
    public TextMeshProUGUI timerText;
    public bool isGameCompleted = false;

    // 引用

    private StageManager stageManager;
    private ScoreSystem scoreSystem;
    private FeedbackSystem feedbackSystem;
    private UIManager uiManager;
    private DragonBoatMovement dragonBoatMovement;
    private GameMenuManager gameMenuManager;

    // 单例实例
    public static GameManager Instance { get; private set; }
    [SerializeField] private LevelRecordManager recordManager;

    void Awake()
    {
        // 确保只有一个GameManager实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 获取引用
        stageManager = FindObjectOfType<StageManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        feedbackSystem = FindObjectOfType<FeedbackSystem>();
        
        dragonBoatMovement = FindObjectOfType<DragonBoatMovement>();
        gameMenuManager = FindObjectOfType<GameMenuManager>();

        // 隐藏暂停菜单和游戏结束屏幕
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        

        // 加载本地高分
        LoadHighScore();

        // 初始化游戏状态
        isGameCompleted = false;
        gameTimer = 0f;
        Time.timeScale = 1f;

        // 更新UI
        UpdateUI();
    }

    void Update()
    {
        // 处理暂停
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        // 更新游戏计时器
        if (!isPaused && !isGameCompleted)
        {
            gameTimer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        // 暂停/恢复游戏
        Time.timeScale = isPaused ? 0f : 1f;

        // 显示/隐藏暂停菜单
        if (pauseMenu != null)
            pauseMenu.SetActive(isPaused);

        // 使用UIManager显示暂停菜单
        if (uiManager != null)
            uiManager.ShowPauseMenu(isPaused);
    }

    public void OnLevelCompleted(int score)
    {
        isGameCompleted = true;
        SaveScore();

        // 检查是否为新高分
        if (score > highScore)
        {
            highScore = score;
            
        }

        // 更新UI显示
        if (currentScoreText != null)
            currentScoreText.text = "得分: " + score;

        if (highScoreText != null)
            highScoreText.text = "最高分: " + highScore;



        // 使用UIManager显示游戏结束屏幕
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowGameOverMenu(true);
        }
        else
        {
            Debug.Log("ui manager is null");
        }
    }

    public void RestartLevel()
    {
        // 恢复正常时间流
        Time.timeScale = 1f;

        // 重置游戏状态
        isGameCompleted = false;
        gameTimer = 0f;

        // 重新加载当前场景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void InitializeLevelUI()
    {
        // 动态绑定新场景的UI元素
        currentScoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        
        progressSlider = GameObject.Find("ProgressSlider").GetComponent<Slider>();

        // 初始化进度条
        progressSlider.minValue = 0;
        progressSlider.maxValue = 1;
    }

    void ResetCurrentData()
    {
        gameTimer = 0f;
        trackProgress = 0f;
        
    }

    public void LoadNextLevel()
    {
        // 计算下一关的索引
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;

        // 检查是否还有下一关
        if (nextLevelIndex < SceneManager.sceneCountInBuildSettings)
        {
            currentStage++;
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextLevelIndex);
        }
        else
        {
            ReturnToMainMenu();
        }
    }

    public void ReturnToMainMenu()
    {
        // 恢复正常时间流
        Time.timeScale = 1f;

        // 加载主菜单场景（假设它是构建索引为0的场景）
        SceneManager.LoadScene("0_MainMenu_Scene");
        
    }

    public void QuitGame()
    {
        // 保存所有数据
        SaveScore();

#if UNITY_EDITOR
        // 在编辑器中停止播放模式
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建的应用程序中退出
        Application.Quit();
#endif
    }

    private void SaveScore()
    {
        int currentScore = scoreSystem.GetScore();
        // 1. 创建新记录
        GameRecord newRecord = new GameRecord(currentStage, currentScore, gameTimer);

        // 2. 加载历史记录
        List<GameRecord> allRecords = LoadAllRecords();

        // 3. 添加新记录
        allRecords.Add(newRecord);

        // 4. 排序并保留前N条（示例保留10条）
        allRecords = allRecords
            .OrderByDescending(r => r.score)
            .ThenBy(r => r.time)
            .Take(10)
            .ToList();

        // 5. 保存更新后的列表
        SaveAllRecords(allRecords);
    }

    public List<GameRecord> LoadAllRecords()
    {
        if (PlayerPrefs.HasKey("GameRecords"))
        {
            string json = PlayerPrefs.GetString("GameRecords");
            GameRecordList wrapper = JsonUtility.FromJson<GameRecordList>(json);
            return wrapper?.records ?? new List<GameRecord>();
        }
        return new List<GameRecord>();
    }

    private void SaveAllRecords(List<GameRecord> records)
    {
        GameRecordList wrapper = new GameRecordList { records = records };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString("GameRecords", json);
        PlayerPrefs.Save();

        Debug.Log($"已保存{records.Count}条记录：\n{json}");
    }


    private void LoadHighScore()
    {
        // 从PlayerPrefs加载高分
        highScore = PlayerPrefs.GetInt("HighScore_Level" + currentStage, 0);
    }

    private void UpdateUI()
    {
        // 更新高分显示
        if (highScoreText != null)
            highScoreText.text = "最高分: " + highScore;

        // 更新计时器显示
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            // 格式化时间为分:秒.毫秒
            int minutes = Mathf.FloorToInt(gameTimer / 60f);
            int seconds = Mathf.FloorToInt(gameTimer % 60f);
            int milliseconds = Mathf.FloorToInt((gameTimer * 100f) % 100f);

            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
    }

    // 获取当前舞台描述
    public string GetCurrentStageDescription()
    {
        if (stageManager != null)
            return stageManager.GetCurrentStageDescription();

        return "第一关: 宁静峡湾";
    }

    // 设置当前舞台
    public void SetStage(int stageNumber)
    {
        if (stageManager != null && stageNumber >= 1 && stageNumber <= maxStage)
        {
            currentStage = stageNumber;
            stageManager.JumpToStage(stageNumber);
        }
    }

    // 用于在Editor中重置玩家数据的辅助方法
    public void ResetAllPlayerData()
    {
        recordManager = FindAnyObjectByType<LevelRecordManager>();
        // 清除本地存储
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 重置内存数据
        highScore = 0;

        // 如果使用独立数据类
        if (recordManager != null)
        {
            recordManager.ClearLocalRecords(); // 调用记录管理器的清理方法
        }

        // 重置UI显示
        UpdateUI();

        // 强制资源清理（可选）
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Debug.Log("所有数据已重置，包括：\n- 玩家偏好设置\n- 排行榜记录\n- 当前游戏状态");
    }
    
    
    [System.Serializable]
    public class GameRecord
    {
        public int levelIndex;  // 关卡编号（根据currentStage）
        public int score;
        public float time;
        public string timestamp; // ISO8601格式时间戳

        public GameRecord(int level, int score, float time)
        {
            this.levelIndex = level;
            this.score = score;
            this.time = time;
            this.timestamp = System.DateTime.UtcNow.ToString("O");
        }
    }

    // 用于JSON序列化的包装类
    [System.Serializable]
    private class GameRecordList
    {
        public List<GameRecord> records = new List<GameRecord>();
    }

}