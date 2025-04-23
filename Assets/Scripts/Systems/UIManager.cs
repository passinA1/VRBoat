using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI������������������ϷUI�ĸ��º͹���
/// ����ͳһ����UI�仯�͹���Ч��
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("��Ϸ״̬UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timerText;
    public Slider progressBar;
    public Image fullPowerIndicator;

    [Header("�׶���ϢUI")]
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI stageDescriptionText;
    public GameObject[] stageIcons;

    [Header("��Ϸ�˵�")]
    public GameObject pauseMenu;
    public GameObject gameOverMenu;
    public GameObject optionsMenu;
    public GameObject helpMenu;

    [Header("���ɶ���")]
    public CanvasGroup screenFader;
    public float fadeSpeed = 1.5f;

    [Header("������ʾ")]
    public GameObject keyboardPrompts;
    public GameObject vrControllerPrompts;

    [Header("�����ж���")]
    public GameObject leftFlagMarker;
    public GameObject rightFlagMarker;
    public GameObject judgmentLine;

    // ˽�б���
    private DragonBoatMovement boatMovement;
    private ScoreSystem scoreSystem;
    private GameManager gameManager;
    private StageManager stageManager;
    private bool isVRMode = false;
    private bool isFading = false;

    void Start()
    {
        // ��ȡ��Ϸ��������
        boatMovement = FindObjectOfType<DragonBoatMovement>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        gameManager = FindObjectOfType<GameManager>();
        stageManager = FindObjectOfType<StageManager>();

        // ���VRģʽ
        isVRMode = IsVRModeActive();

        // ��ʼ��UI
        InitializeUI();

        // �������в˵�
        HideAllMenus();
    }

    void Update()
    {
        // ������Ϸ״̬UI
        UpdateGameStateUI();

        // ���½׶���Ϣ
        UpdateStageInfo();
    }

    // ����Ƿ�ΪVRģʽ
    private bool IsVRModeActive()
    {
        bool vrDeviceActive = UnityEngine.XR.XRSettings.isDeviceActive;

        // ����Ƿ���ǿ��ʹ�ü���ģʽ������
        PaddleController paddleController = FindObjectOfType<PaddleController>();
        if (paddleController != null && paddleController.forceKeyboardMode)
            return false;

        return vrDeviceActive;
    }

    // ��ʼ��UI
    private void InitializeUI()
    {
        // ��ʾ��Ӧ�Ŀ�����ʾ
        if (keyboardPrompts != null)
            keyboardPrompts.SetActive(!isVRMode);

        if (vrControllerPrompts != null)
            vrControllerPrompts.SetActive(isVRMode);

        // ��ʼ��ȫ��ָʾ��
        if (fullPowerIndicator != null)
            fullPowerIndicator.gameObject.SetActive(false);

        // ��ʼ�������ж��㣨Ĭ�����أ��ڵ����׶���ʾ��
        SetFlagMarkersVisible(false);
    }

    // ������Ϸ״̬UI
    private void UpdateGameStateUI()
    {
        // ���·���
        if (scoreText != null && scoreSystem != null)
        {
            scoreText.text = "����: " + scoreSystem.GetScore().ToString();
        }

        // ��������
        if (comboText != null && scoreSystem != null)
        {
            int combo = scoreSystem.comboCount;
            comboText.text = "����: " + combo.ToString();

            // �����������ı���ɫ
            if (combo >= scoreSystem.comboThreshold)
                comboText.color = Color.magenta; // ȫ��״̬
            else if (combo >= scoreSystem.comboThreshold / 2)
                comboText.color = Color.yellow; // �ӽ�ȫ��
            else
                comboText.color = Color.white; // ��ͨ״̬
        }

        // �����ٶ�
        if (speedText != null && boatMovement != null)
        {
            float speed = boatMovement.GetCurrentSpeed();
            speedText.text = "�ٶ�: " + speed.ToString("F1") + " m/s";

            // �����ٶȸı���ɫ
            if (speed >= boatMovement.maxSpeed * 0.9f)
                speedText.color = Color.green; // �ӽ�����ٶ�
            else if (speed <= boatMovement.minSpeed * 1.2f)
                speedText.color = Color.red; // �ӽ�����ٶ�
            else
                speedText.color = Color.white; // ��ͨ�ٶ�
        }

        // ���¼�ʱ��
        if (timerText != null && gameManager != null)
        {
            float time = gameManager.gameTimer;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // ���½�����
        if (progressBar != null && boatMovement != null)
        {
            progressBar.value = boatMovement.GetCompletionPercentage();
        }
    }

    // ���½׶���Ϣ
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

        // ���½׶��ı�
        if (stageText != null)
        {
            stageText.text = "�׶� " + currentStage + "/" + maxStage;
        }

        // ���½׶�����
        if (stageDescriptionText != null)
        {
            stageDescriptionText.text = stageDescription;
        }

        // ���½׶�ͼ��
        if (stageIcons != null)
        {
            for (int i = 0; i < stageIcons.Length; i++)
            {
                if (stageIcons[i] != null)
                {
                    // ��ǰ�׶θ�����ʾ
                    bool isCurrentStage = (i + 1) == currentStage;
                    bool isPastStage = (i + 1) < currentStage;

                    // ��ȡͼ���Image���
                    Image iconImage = stageIcons[i].GetComponent<Image>();
                    if (iconImage != null)
                    {
                        if (isCurrentStage)
                            iconImage.color = Color.yellow; // ��ǰ�׶�
                        else if (isPastStage)
                            iconImage.color = Color.green; // ����ɽ׶�
                        else
                            iconImage.color = Color.gray; // δ�����׶�
                    }
                }
            }
        }
    }

    // ��ʾȫ��״ָ̬ʾ��
    public void ShowFullPowerIndicator(bool show)
    {
        if (fullPowerIndicator != null)
        {
            fullPowerIndicator.gameObject.SetActive(show);

            // ��Ӷ���Ч��
            if (show)
            {
                StartCoroutine(PulseEffect(fullPowerIndicator));
            }
        }
    }

    // ����Ч��Э��
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

    // ��ʾ/������ͣ�˵�
    public void ShowPauseMenu(bool show)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(show);

            // �����ʾ��ͣ�˵�����ͣ��Ϸ
            Time.timeScale = show ? 0f : 1f;
        }
    }

    // ��ʾ��Ϸ�����˵�
    public void ShowGameOverMenu(bool show)
    {
        if (gameOverMenu != null)
        {
            gameOverMenu.SetActive(show);

            // ����гɼ���ʾ�����³ɼ�
            if (show && scoreSystem != null)
            {
                TextMeshProUGUI[] texts = gameOverMenu.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var text in texts)
                {
                    if (text.name.Contains("ScoreText"))
                    {
                        text.text = "���յ÷�: " + scoreSystem.GetScore().ToString();
                    }
                    else if (text.name.Contains("ComboText"))
                    {
                        text.text = "�������: " + scoreSystem.GetHighestCombo().ToString();
                    }
                    else if (text.name.Contains("TimeText") && gameManager != null)
                    {
                        float time = gameManager.gameTimer;
                        int minutes = Mathf.FloorToInt(time / 60f);
                        int seconds = Mathf.FloorToInt(time % 60f);
                        text.text = "���ʱ��: " + string.Format("{0:00}:{1:00}", minutes, seconds);
                    }
                }
            }
        }
    }

    // �������в˵�
    public void HideAllMenus()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (gameOverMenu != null) gameOverMenu.SetActive(false);
        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (helpMenu != null) helpMenu.SetActive(false);

        // �ָ���Ϸʱ��
        Time.timeScale = 1f;
    }

    // ��Ļ����
    public void FadeIn()
    {
        if (screenFader != null && !isFading)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCoroutine(1, 0));
        }
    }

    // ��Ļ����
    public void FadeOut()
    {
        if (screenFader != null && !isFading)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCoroutine(0, 1));
        }
    }

    // ���뵭��Э��
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

        // �����ȫ͸�������ö���
        if (endAlpha == 0)
            screenFader.gameObject.SetActive(false);

        isFading = false;
    }

    // ���ñ����ж���ɼ���
    public void SetFlagMarkersVisible(bool visible)
    {
        if (leftFlagMarker != null) leftFlagMarker.SetActive(visible);
        if (rightFlagMarker != null) rightFlagMarker.SetActive(visible);
        if (judgmentLine != null) judgmentLine.SetActive(visible);
    }

    // ����UI�ߴ�����Ӧ��ͬ�ķֱ���
    public void UpdateUIScale(float scale)
    {
        RectTransform canvasRect = GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.localScale = new Vector3(scale, scale, 1);
        }
    }

    // ���VRģʽ�仯������UI
    public void CheckVRModeChanged()
    {
        bool newVRMode = IsVRModeActive();

        if (newVRMode != isVRMode)
        {
            isVRMode = newVRMode;

            // ���¿�����ʾ
            if (keyboardPrompts != null)
                keyboardPrompts.SetActive(!isVRMode);

            if (vrControllerPrompts != null)
                vrControllerPrompts.SetActive(isVRMode);
        }
    }
}