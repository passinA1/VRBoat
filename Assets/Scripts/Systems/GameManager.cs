using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("游戏设置")]
    public bool isPaused = false;
    public GameObject pauseMenu;
    public GameObject gameOverScreen;
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("关卡进度")]
    public int currentStage = 1;  // 当前所在关卡
    public int maxStage = 1;      // 最大关卡数量
    public int highScore = 0;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI currentScoreText;

    [Header("游戏状态")]
    public float gameTimer = 0f;
    public TextMeshProUGUI timerText;
    public bool isGameCompleted = false;

    // 引用
    private StageManager stageManager;
    private ScoreSystem scoreSystem;
    private FeedbackSystem feedbackSystem;
    private UIManager uiManager;

    // 单例实例
    public static GameManager Instance { get; private set; }

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
        uiManager = FindObjectOfType<UIManager>();

        // 隐藏暂停菜单和游戏结束屏幕
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

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

        // 检查是否为新高分
        if (score > highScore)
        {
            highScore = score;
            SaveHighScore();
        }

        // 更新UI显示
        if (currentScoreText != null)
            currentScoreText.text = "得分: " + score;

        if (highScoreText != null)
            highScoreText.text = "最高分: " + highScore;

        // 显示游戏结束屏幕
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        // 使用UIManager显示游戏结束屏幕
        if (uiManager != null)
            uiManager.ShowGameOverMenu(true);
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
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        // 保存所有数据
        SaveHighScore();

#if UNITY_EDITOR
        // 在编辑器中停止播放模式
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 在构建的应用程序中退出
        Application.Quit();
#endif
    }

    private void SaveHighScore()
    {
        // 保存高分到PlayerPrefs
        PlayerPrefs.SetInt("HighScore_Level" + currentStage, highScore);
        PlayerPrefs.Save();
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
        PlayerPrefs.DeleteAll();
        highScore = 0;
        UpdateUI();
        Debug.Log("所有玩家数据已重置");
    }
}