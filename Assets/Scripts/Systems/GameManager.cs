using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("��Ϸ����")]
    public bool isPaused = false;
    public GameObject pauseMenu;
    public GameObject gameOverScreen;
    public KeyCode pauseKey = KeyCode.Escape;

    [Header("�ؿ�����")]
    public int currentStage = 1;  // ��ǰ���ڹؿ�
    public int maxStage = 1;      // ���ؿ�����
    public int highScore = 0;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI currentScoreText;

    [Header("��Ϸ״̬")]
    public float gameTimer = 0f;
    public TextMeshProUGUI timerText;
    public bool isGameCompleted = false;

    // ����
    private StageManager stageManager;
    private ScoreSystem scoreSystem;
    private FeedbackSystem feedbackSystem;
    private UIManager uiManager;

    // ����ʵ��
    public static GameManager Instance { get; private set; }

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
        uiManager = FindObjectOfType<UIManager>();

        // ������ͣ�˵�����Ϸ������Ļ
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

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

        // ����Ƿ�Ϊ�¸߷�
        if (score > highScore)
        {
            highScore = score;
            SaveHighScore();
        }

        // ����UI��ʾ
        if (currentScoreText != null)
            currentScoreText.text = "�÷�: " + score;

        if (highScoreText != null)
            highScoreText.text = "��߷�: " + highScore;

        // ��ʾ��Ϸ������Ļ
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        // ʹ��UIManager��ʾ��Ϸ������Ļ
        if (uiManager != null)
            uiManager.ShowGameOverMenu(true);
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
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        // ������������
        SaveHighScore();

#if UNITY_EDITOR
        // �ڱ༭����ֹͣ����ģʽ
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // �ڹ�����Ӧ�ó������˳�
        Application.Quit();
#endif
    }

    private void SaveHighScore()
    {
        // ����߷ֵ�PlayerPrefs
        PlayerPrefs.SetInt("HighScore_Level" + currentStage, highScore);
        PlayerPrefs.Save();
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
        PlayerPrefs.DeleteAll();
        highScore = 0;
        UpdateUI();
        Debug.Log("�����������������");
    }
}