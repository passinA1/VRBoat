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
    [Header("��Ϸ����")]
    public bool isPaused = false;
    public GameObject pauseMenu;
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("�ؿ�����")]
    public int currentStage = 1;  // ��ǰ���ڹؿ�
    public int maxStage = 1;      // ���ؿ�����
    public int highScore = 0;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI currentScoreText;
    public float trackProgress;
    public float currentSpeed;
    public Slider progressSlider; // ������
    private DragonBoatMovement boatMovement;

    [Header("��Ϸ״̬")]
    public float gameTimer = 0f;
    public TextMeshProUGUI timerText;
    public bool isGameCompleted = false;

    // ����

    private StageManager stageManager;
    private ScoreSystem scoreSystem;
    private FeedbackSystem feedbackSystem;
    private UIManager uiManager;
    private DragonBoatMovement dragonBoatMovement;
    private GameMenuManager gameMenuManager;

    // ����ʵ��
    public static GameManager Instance { get; private set; }
    [SerializeField] private LevelRecordManager recordManager;

    void Awake()
    {
        // ȷ��ֻ��һ��GameManagerʵ��
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
        // ��ȡ����
        stageManager = FindObjectOfType<StageManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        feedbackSystem = FindObjectOfType<FeedbackSystem>();
        
        dragonBoatMovement = FindObjectOfType<DragonBoatMovement>();
        gameMenuManager = FindObjectOfType<GameMenuManager>();

        // ������ͣ�˵�����Ϸ������Ļ
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        

        // ���ر��ظ߷�
        LoadHighScore();

        // ��ʼ����Ϸ״̬
        isGameCompleted = false;
        gameTimer = 0f;
        Time.timeScale = 1f;

        // ����UI
        UpdateUI();
    }

    void Update()
    {
        // ������ͣ
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        // ������Ϸ��ʱ��
        if (!isPaused && !isGameCompleted)
        {
            gameTimer += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        // ��ͣ/�ָ���Ϸ
        Time.timeScale = isPaused ? 0f : 1f;

        // ��ʾ/������ͣ�˵�
        if (pauseMenu != null)
            pauseMenu.SetActive(isPaused);

        // ʹ��UIManager��ʾ��ͣ�˵�
        if (uiManager != null)
            uiManager.ShowPauseMenu(isPaused);
    }

    public void OnLevelCompleted(int score)
    {
        isGameCompleted = true;
        SaveScore();

        // ����Ƿ�Ϊ�¸߷�
        if (score > highScore)
        {
            highScore = score;
            
        }

        // ����UI��ʾ
        if (currentScoreText != null)
            currentScoreText.text = "�÷�: " + score;

        if (highScoreText != null)
            highScoreText.text = "��߷�: " + highScore;



        // ʹ��UIManager��ʾ��Ϸ������Ļ
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
        // �ָ�����ʱ����
        Time.timeScale = 1f;

        // ������Ϸ״̬
        isGameCompleted = false;
        gameTimer = 0f;

        // ���¼��ص�ǰ����
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void InitializeLevelUI()
    {
        // ��̬���³�����UIԪ��
        currentScoreText = GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        
        progressSlider = GameObject.Find("ProgressSlider").GetComponent<Slider>();

        // ��ʼ��������
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
        // ������һ�ص�����
        int nextLevelIndex = SceneManager.GetActiveScene().buildIndex + 1;

        // ����Ƿ�����һ��
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
        // �ָ�����ʱ����
        Time.timeScale = 1f;

        // �������˵��������������ǹ�������Ϊ0�ĳ�����
        SceneManager.LoadScene("0_MainMenu_Scene");
        
    }

    public void QuitGame()
    {
        // ������������
        SaveScore();

#if UNITY_EDITOR
        // �ڱ༭����ֹͣ����ģʽ
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // �ڹ�����Ӧ�ó������˳�
        Application.Quit();
#endif
    }

    private void SaveScore()
    {
        int currentScore = scoreSystem.GetScore();
        // 1. �����¼�¼
        GameRecord newRecord = new GameRecord(currentStage, currentScore, gameTimer);

        // 2. ������ʷ��¼
        List<GameRecord> allRecords = LoadAllRecords();

        // 3. ����¼�¼
        allRecords.Add(newRecord);

        // 4. ���򲢱���ǰN����ʾ������10����
        allRecords = allRecords
            .OrderByDescending(r => r.score)
            .ThenBy(r => r.time)
            .Take(10)
            .ToList();

        // 5. ������º���б�
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

        Debug.Log($"�ѱ���{records.Count}����¼��\n{json}");
    }


    private void LoadHighScore()
    {
        // ��PlayerPrefs���ظ߷�
        highScore = PlayerPrefs.GetInt("HighScore_Level" + currentStage, 0);
    }

    private void UpdateUI()
    {
        // ���¸߷���ʾ
        if (highScoreText != null)
            highScoreText.text = "��߷�: " + highScore;

        // ���¼�ʱ����ʾ
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            // ��ʽ��ʱ��Ϊ��:��.����
            int minutes = Mathf.FloorToInt(gameTimer / 60f);
            int seconds = Mathf.FloorToInt(gameTimer % 60f);
            int milliseconds = Mathf.FloorToInt((gameTimer * 100f) % 100f);

            timerText.text = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
        }
    }

    // ��ȡ��ǰ��̨����
    public string GetCurrentStageDescription()
    {
        if (stageManager != null)
            return stageManager.GetCurrentStageDescription();

        return "��һ��: ����Ͽ��";
    }

    // ���õ�ǰ��̨
    public void SetStage(int stageNumber)
    {
        if (stageManager != null && stageNumber >= 1 && stageNumber <= maxStage)
        {
            currentStage = stageNumber;
            stageManager.JumpToStage(stageNumber);
        }
    }

    // ������Editor������������ݵĸ�������
    public void ResetAllPlayerData()
    {
        recordManager = FindAnyObjectByType<LevelRecordManager>();
        // ������ش洢
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // �����ڴ�����
        highScore = 0;

        // ���ʹ�ö���������
        if (recordManager != null)
        {
            recordManager.ClearLocalRecords(); // ���ü�¼��������������
        }

        // ����UI��ʾ
        UpdateUI();

        // ǿ����Դ������ѡ��
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Debug.Log("�������������ã�������\n- ���ƫ������\n- ���а��¼\n- ��ǰ��Ϸ״̬");
    }
    
    
    [System.Serializable]
    public class GameRecord
    {
        public int levelIndex;  // �ؿ���ţ�����currentStage��
        public int score;
        public float time;
        public string timestamp; // ISO8601��ʽʱ���

        public GameRecord(int level, int score, float time)
        {
            this.levelIndex = level;
            this.score = score;
            this.time = time;
            this.timestamp = System.DateTime.UtcNow.ToString("O");
        }
    }

    // ����JSON���л��İ�װ��
    [System.Serializable]
    private class GameRecordList
    {
        public List<GameRecord> records = new List<GameRecord>();
    }

}