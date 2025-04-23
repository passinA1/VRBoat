using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI管理器，负责所有游戏UI的更新和管理
/// 方便统一处理UI变化和过渡效果
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("游戏状态UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timerText;
    public Slider progressBar;
    public Image fullPowerIndicator;

    [Header("阶段信息UI")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI stageDescriptionText;
    public GameObject[] stageIcons;

    [Header("游戏菜单")]
    public GameObject pauseMenu;
    public GameObject gameOverMenu;
    public GameObject optionsMenu;
    public GameObject helpMenu;

    [Header("过渡动画")]
    public CanvasGroup screenFader;
    public float fadeSpeed = 1.5f;

    [Header("按键提示")]
    public GameObject keyboardPrompts;
    public GameObject vrControllerPrompts;

    [Header("标旗判定点")]
    public GameObject leftFlagMarker;
    public GameObject rightFlagMarker;
    public GameObject judgmentLine;

    // 私有变量
    private DragonBoatMovement boatMovement;
    private ScoreSystem scoreSystem;
    private GameManager gameManager;
    private StageManager stageManager;
    private bool isVRMode = false;
    private bool isFading = false;

    void Start()
    {
        // 获取游戏对象引用
        boatMovement = FindObjectOfType<DragonBoatMovement>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        gameManager = FindObjectOfType<GameManager>();
        stageManager = FindObjectOfType<StageManager>();

        // 检测VR模式
        isVRMode = IsVRModeActive();

        // 初始化UI
        InitializeUI();

        // 隐藏所有菜单
        HideAllMenus();
    }

    void Update()
    {
        // 更新游戏状态UI
        UpdateGameStateUI();

        // 更新阶段信息
        UpdateStageInfo();
    }

    // 检测是否为VR模式
    private bool IsVRModeActive()
    {
        bool vrDeviceActive = UnityEngine.XR.XRSettings.isDeviceActive;

        // 检查是否有强制使用键盘模式的设置
        PaddleController paddleController = FindObjectOfType<PaddleController>();
        if (paddleController != null && paddleController.forceKeyboardMode)
            return false;

        return vrDeviceActive;
    }

    // 初始化UI
    private void InitializeUI()
    {
        // 显示对应的控制提示
        if (keyboardPrompts != null)
            keyboardPrompts.SetActive(!isVRMode);

        if (vrControllerPrompts != null)
            vrControllerPrompts.SetActive(isVRMode);

        // 初始化全力指示器
        if (fullPowerIndicator != null)
            fullPowerIndicator.gameObject.SetActive(false);

        // 初始化标旗判定点（默认隐藏，在第三阶段显示）
        SetFlagMarkersVisible(false);
    }

    // 更新游戏状态UI
    private void UpdateGameStateUI()
    {
        // 更新分数
        if (scoreText != null && scoreSystem != null)
        {
            scoreText.text = "分数: " + scoreSystem.GetScore().ToString();
        }

        // 更新连击
        if (comboText != null && scoreSystem != null)
        {
            int combo = scoreSystem.comboCount;
            comboText.text = "连击: " + combo.ToString();

            // 根据连击数改变颜色
            if (combo >= scoreSystem.comboThreshold)
                comboText.color = Color.magenta; // 全力状态
            else if (combo >= scoreSystem.comboThreshold / 2)
                comboText.color = Color.yellow; // 接近全力
            else
                comboText.color = Color.white; // 普通状态
        }

        // 更新速度
        if (speedText != null && boatMovement != null)
        {
            float speed = boatMovement.GetCurrentSpeed();
            speedText.text = "速度: " + speed.ToString("F1") + " m/s";

            // 根据速度改变颜色
            if (speed >= boatMovement.maxSpeed * 0.9f)
                speedText.color = Color.green; // 接近最高速度
            else if (speed <= boatMovement.minSpeed * 1.2f)
                speedText.color = Color.red; // 接近最低速度
            else
                speedText.color = Color.white; // 普通速度
        }

        // 更新计时器
        if (timerText != null && gameManager != null)
        {
            float time = gameManager.gameTimer;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // 更新进度条
        if (progressBar != null && boatMovement != null)
        {
            progressBar.value = boatMovement.GetCompletionPercentage();
        }
    }

    // 更新阶段信息
    private void UpdateStageInfo()
    {
        if (gameManager == null && stageManager == null) return;

        int currentStage = 1;
        int maxStage = 1;
        string stageDescription = "";

        if (gameManager != null)
        {
            currentStage = gameManager.currentStage;
            maxStage = gameManager.maxStage;
        }

        if (stageManager != null)
        {
            stageDescription = stageManager.GetCurrentStageDescription();
            maxStage = stageManager.GetTotalStages();
        }

        // 更新阶段文本
        if (stageText != null)
        {
            stageText.text = "阶段 " + currentStage + "/" + maxStage;
        }

        // 更新阶段描述
        if (stageDescriptionText != null)
        {
            stageDescriptionText.text = stageDescription;
        }

        // 更新阶段图标
        if (stageIcons != null)
        {
            for (int i = 0; i < stageIcons.Length; i++)
            {
                if (stageIcons[i] != null)
                {
                    // 当前阶段高亮显示
                    bool isCurrentStage = (i + 1) == currentStage;
                    bool isPastStage = (i + 1) < currentStage;

                    // 获取图标的Image组件
                    Image iconImage = stageIcons[i].GetComponent<Image>();
                    if (iconImage != null)
                    {
                        if (isCurrentStage)
                            iconImage.color = Color.yellow; // 当前阶段
                        else if (isPastStage)
                            iconImage.color = Color.green; // 已完成阶段
                        else
                            iconImage.color = Color.gray; // 未解锁阶段
                    }
                }
            }
        }
    }

    // 显示全力状态指示器
    public void ShowFullPowerIndicator(bool show)
    {
        if (fullPowerIndicator != null)
        {
            fullPowerIndicator.gameObject.SetActive(show);

            // 添加动画效果
            if (show)
            {
                StartCoroutine(PulseEffect(fullPowerIndicator));
            }
        }
    }

    // 脉冲效果协程
    private IEnumerator PulseEffect(Image image)
    {
        float duration = 0.5f;
        float elapsed = 0;
        Color startColor = image.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0.5f);

        while (image.gameObject.activeSelf)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed / duration, 1f);

            image.color = Color.Lerp(startColor, targetColor, t);

            yield return null;
        }
    }

    // 显示/隐藏暂停菜单
    public void ShowPauseMenu(bool show)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(show);

            // 如果显示暂停菜单，暂停游戏
            Time.timeScale = show ? 0f : 1f;
        }
    }

    // 显示游戏结束菜单
    public void ShowGameOverMenu(bool show)
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(show);

            // 如果有成绩显示，更新成绩
            if (show && scoreSystem != null)
            {
                TextMeshProUGUI[] texts = gameOverMenu.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (text.name.Contains("ScoreText"))
                    {
                        text.text = "最终得分: " + scoreSystem.GetScore().ToString();
                    }
                    else if (text.name.Contains("ComboText"))
                    {
                        text.text = "最高连击: " + scoreSystem.GetHighestCombo().ToString();
                    }
                    else if (text.name.Contains("TimeText") && gameManager != null)
                    {
                        float time = gameManager.gameTimer;
                        int minutes = Mathf.FloorToInt(time / 60f);
                        int seconds = Mathf.FloorToInt(time % 60f);
                        text.text = "完成时间: " + string.Format("{0:00}:{1:00}", minutes, seconds);
                    }
                }
            }
        }
    }

    // 隐藏所有菜单
    public void HideAllMenus()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (helpMenu != null) helpMenu.SetActive(false);

        // 恢复游戏时间
        Time.timeScale = 1f;
    }

    // 屏幕淡入
    public void FadeIn()
    {
        if (screenFader != null && !isFading)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCoroutine(1, 0));
        }
    }

    // 屏幕淡出
    public void FadeOut()
    {
        if (screenFader != null && !isFading)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCoroutine(0, 1));
        }
    }

    // 淡入淡出协程
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha)
    {
        isFading = true;
        screenFader.gameObject.SetActive(true);
        screenFader.alpha = startAlpha;

        float elapsed = 0;
        float duration = Mathf.Abs(endAlpha - startAlpha) / fadeSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            screenFader.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        screenFader.alpha = endAlpha;

        // 如果完全透明，禁用对象
        if (endAlpha == 0)
            screenFader.gameObject.SetActive(false);

        isFading = false;
    }

    // 设置标旗判定点可见性
    public void SetFlagMarkersVisible(bool visible)
    {
        if (leftFlagMarker != null) leftFlagMarker.SetActive(visible);
        if (rightFlagMarker != null) rightFlagMarker.SetActive(visible);
        if (judgmentLine != null) judgmentLine.SetActive(visible);
    }

    // 更新UI尺寸以适应不同的分辨率
    public void UpdateUIScale(float scale)
    {
        RectTransform canvasRect = GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.localScale = new Vector3(scale, scale, 1);
        }
    }

    // 检测VR模式变化并更新UI
    public void CheckVRModeChanged()
    {
        bool newVRMode = IsVRModeActive();

        if (newVRMode != isVRMode)
        {
            isVRMode = newVRMode;

            // 更新控制提示
            if (keyboardPrompts != null)
                keyboardPrompts.SetActive(!isVRMode);

            if (vrControllerPrompts != null)
                vrControllerPrompts.SetActive(isVRMode);
        }
    }
}