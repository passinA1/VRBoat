using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// �׶ι�������ר�Ÿ��������Ϸ�ؿ��еĲ�ͬ�׶ι������߼�
/// ���GameManagerʹ�ã�����GameManager�ĸ���
/// </summary>
public class StageManager : MonoBehaviour
{
    [System.Serializable]
    public class StageConfig
    {
        public string stageName;
        public string description;
        public GameObject instructionUI;
        public bool requireAlternating;
        public bool allowSyncPaddle;
        public float stageDuration = 60f;
        public int targetCombo = 0;
        public int targetScore = 0;
    }

    [Header("�׶�����")]
    public StageConfig[] stages;  // �׶���������
    public int currentStage = 1;  // ��ǰ�׶Σ���1��ʼ
    public bool autoProgressStages = true; // �Ƿ��Զ�������һ�׶�

    [Header("��������")]
    public GameObject transitionScreen;
    public float transitionDuration = 1.0f;
    public AudioClip transitionSound;
    public TextMeshProUGUI stageAnnouncementText;

    [Header("UI����")]
    public TextMeshProUGUI currentStageText;
    public TextMeshProUGUI stageDescriptionText;
    public Slider stageProgressBar;

    [Header("��������")]
    public bool debugMode = false;

    // ˽�б���
    private float stageStartTime;
    private bool isTransitioning = false;
    private Coroutine stageTimerCoroutine;
    private GameManager gameManager;
    private ScoreSystem scoreSystem;
    private AudioSource audioSource;
    private Animator transitionAnimator;
    private int stageObjective = 0; // 0=ʱ��, 1=����, 2=����

    // ����
    private PaddleController paddleController;
    private UIManager uiManager;

    void Start()
    {
        // ��ȡ����
        gameManager = FindObjectOfType<GameManager>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        audioSource = GetComponent<AudioSource>();
        paddleController = FindObjectOfType<PaddleController>();
        uiManager = FindObjectOfType<UIManager>();

        if (transitionScreen != null)
        {
            transitionAnimator = transitionScreen.GetComponent<Animator>();
            transitionScreen.SetActive(false);
        }

        // ȷ���׶����鳤����Ч
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("StageManager: �׶�����Ϊ��!");
            return;
        }

        // ȷ��currentStage����Ч��Χ��
        currentStage = Mathf.Clamp(currentStage, 1, stages.Length);

        // ��ʼ����һ���׶�
        SetupCurrentStage();

        // ��������Զ����ȣ���ʼ��ʱ��
        if (autoProgressStages)
        {
            stageStartTime = Time.time;
            stageTimerCoroutine = StartCoroutine(StageProgressionTimer());
        }

        if (debugMode)
            Debug.Log($"StageManager��ʼ����ɣ���ǰ�׶�: {currentStage} - {GetCurrentStageName()}");
    }

    void Update()
    {
        // ����UI
        UpdateUI();

        // ���Ŀ���Ƿ��ɣ�������ǻ���ʱ�䣩
        if (!isTransitioning && autoProgressStages && stageObjective != 0)
        {
            CheckObjectiveCompletion();
        }
    }

    // ���õ�ǰ�׶�
    public void SetupCurrentStage()
    {
        int index = currentStage - 1;
        if (index < 0 || index >= stages.Length) return;

        StageConfig stage = stages[index];

        // �������н׶�˵��
        foreach (StageConfig s in stages)
        {
            if (s.instructionUI != null)
                s.instructionUI.SetActive(false);
        }

        // ��ʾ��ǰ�׶�˵��
        if (stage.instructionUI != null)
            stage.instructionUI.SetActive(true);

        // ���õ÷�ϵͳ
        if (scoreSystem != null)
        {
            scoreSystem.SetRequireAlternating(stage.requireAlternating);
        }

        // ���ݵ�һ���ĵ�����ÿ���׶ε�����
        if (currentStage == 1)
        {
            // ��һ�׶�: ��������
            // 5�����ֽ�, 5�����ֽ�
            if (scoreSystem != null)
            {
                scoreSystem.SetRequireAlternating(false);
            }
        }
        else if (currentStage == 2)
        {
            // �ڶ��׶�: ����
            // ͬʱʹ��˫�ֻ���5��
            if (paddleController != null)
            {
                // ���������������������
            }
        }
        else if (currentStage == 3)
        {
            // �����׶�: �������
            // ����10���������У����������Ч
            if (scoreSystem != null)
            {
                scoreSystem.SetRequireAlternating(true); // Ҫ�����ҽ���
            }

            // ��ʾ�����ж���
            if (uiManager != null)
            {
                uiManager.SetFlagMarkersVisible(true);
            }
        }
        else if (currentStage == 4)
        {
            // ���Ľ׶�: ��ʽ����
            // ����л��ĵ�
            if (scoreSystem != null)
            {
                scoreSystem.SetRequireAlternating(true);
            }
        }

        // ����UI
        if (currentStageText != null)
            currentStageText.text = $"�׶� {currentStage}: {stage.stageName}";

        if (stageDescriptionText != null)
            stageDescriptionText.text = stage.description;

        // ȷ���׶�Ŀ������
        if (stage.targetCombo > 0)
            stageObjective = 1; // ����
        else if (stage.targetScore > 0)
            stageObjective = 2; // ����
        else
            stageObjective = 0; // ʱ��

        // ���ý׶ο�ʼʱ��
        stageStartTime = Time.time;

        // ֪ͨGameManager
        if (gameManager != null)
        {
            gameManager.currentStage = currentStage;
        }

        if (debugMode)
            Debug.Log($"���ý׶� {currentStage}: {stage.stageName}");
    }

    // ����UI
    private void UpdateUI()
    {
        if (stageProgressBar != null && !isTransitioning)
        {
            int index = currentStage - 1;
            if (index < 0 || index >= stages.Length) return;

            StageConfig stage = stages[index];

            switch (stageObjective)
            {
                case 0: // ʱ��
                    float elapsed = Time.time - stageStartTime;
                    float progress = elapsed / stage.stageDuration;
                    stageProgressBar.value = Mathf.Clamp01(progress);
                    break;

                case 1: // ����
                    if (scoreSystem != null)
                    {
                        float comboProgress = (float)scoreSystem.GetHighestCombo() / stage.targetCombo;
                        stageProgressBar.value = Mathf.Clamp01(comboProgress);
                    }
                    break;

                case 2: // ����
                    if (scoreSystem != null)
                    {
                        float scoreProgress = (float)scoreSystem.GetScore() / stage.targetScore;
                        stageProgressBar.value = Mathf.Clamp01(scoreProgress);
                    }
                    break;
            }
        }
    }

    // ���Ŀ���Ƿ����
    private void CheckObjectiveCompletion()
    {
        int index = currentStage - 1;
        if (index < 0 || index >= stages.Length) return;

        StageConfig stage = stages[index];

        bool objectiveCompleted = false;

        switch (stageObjective)
        {
            case 1: // ����
                if (scoreSystem != null && scoreSystem.GetHighestCombo() >= stage.targetCombo)
                    objectiveCompleted = true;
                break;

            case 2: // ����
                if (scoreSystem != null && scoreSystem.GetScore() >= stage.targetScore)
                    objectiveCompleted = true;
                break;
        }

        if (objectiveCompleted)
        {
            AdvanceToNextStage();
        }
    }

    // �׶ν��ȼ�ʱ��
    private IEnumerator StageProgressionTimer()
    {
        while (currentStage <= stages.Length && !isTransitioning)
        {
            // ֻ�е��׶�Ŀ���ǻ���ʱ��ʱ�ż��ʱ��
            if (stageObjective == 0)
            {
                int index = currentStage - 1;
                if (index >= 0 && index < stages.Length)
                {
                    // ����Ƿ�Ӧ�ý�����һ�׶�
                    if (Time.time - stageStartTime >= stages[index].stageDuration)
                    {
                        AdvanceToNextStage();
                        yield break; // ������ǰЭ�̣��½׶λ������µ�Э��
                    }
                }
            }

            yield return null;
        }
    }

    // ������һ�׶�
    public void AdvanceToNextStage()
    {
        if (currentStage < stages.Length && !isTransitioning)
        {
            StartCoroutine(StageTransition(currentStage, currentStage + 1));
        }
        else if (currentStage >= stages.Length && !isTransitioning)
        {
            // ���н׶���ɣ�֪ͨGameManager�����ؿ�
            if (gameManager != null && scoreSystem != null)
            {
                gameManager.OnLevelCompleted(scoreSystem.GetScore());
            }
        }
    }

    // ��ת���ض��׶�
    public void JumpToStage(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > stages.Length || isTransitioning)
            return;

        if (stageNumber != currentStage)
        {
            StartCoroutine(StageTransition(currentStage, stageNumber));
        }
    }

    // �׶ι���Э��
    private IEnumerator StageTransition(int fromStage, int toStage)
    {
        isTransitioning = true;

        // ֹͣ��ǰ�׶μ�ʱ��
        if (stageTimerCoroutine != null)
        {
            StopCoroutine(stageTimerCoroutine);
            stageTimerCoroutine = null;
        }

        // ��ʾ������Ļ
        if (transitionScreen != null)
            transitionScreen.SetActive(true);

        // ��ʾ�׶��ı�
        if (stageAnnouncementText != null)
        {
            int index = toStage - 1;
            if (index >= 0 && index < stages.Length)
            {
                stageAnnouncementText.text = $"�׶� {toStage}: {stages[index].stageName}";
            }
        }

        // ���Ź��ɶ���
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        // ���Ź�����Ч
        if (audioSource != null && transitionSound != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }

        // �ȴ�����ʱ���һ��
        yield return new WaitForSeconds(transitionDuration / 2);

        // ���µ�ǰ�׶�
        currentStage = toStage;

        // �����½׶�
        SetupCurrentStage();

        // �ȴ�ʣ�����ʱ��
        yield return new WaitForSeconds(transitionDuration / 2);

        // ���ع�����Ļ
        if (transitionScreen != null)
            transitionScreen.SetActive(false);

        // ��������Զ����ȣ���ʼ�µļ�ʱ��
        if (autoProgressStages)
        {
            stageTimerCoroutine = StartCoroutine(StageProgressionTimer());
        }

        isTransitioning = false;

        if (debugMode)
            Debug.Log($"���л����׶� {currentStage}: {GetCurrentStageName()}");
    }

    // ��ȡ��ǰ�׶�����
    public string GetCurrentStageName()
    {
        int index = currentStage - 1;
        return (index >= 0 && index < stages.Length) ? stages[index].stageName : "δ֪";
    }

    // ��ȡ��ǰ�׶�����
    public string GetCurrentStageDescription()
    {
        int index = currentStage - 1;
        return (index >= 0 && index < stages.Length) ? stages[index].description : "δ֪�׶�";
    }

    // ���õ�ǰ�׶�
    public void RestartCurrentStage()
    {
        if (isTransitioning) return;

        // ֹͣ��ǰ�׶μ�ʱ��
        if (stageTimerCoroutine != null)
        {
            StopCoroutine(stageTimerCoroutine);
            stageTimerCoroutine = null;
        }

        // �������õ�ǰ�׶�
        SetupCurrentStage();

        // ��������Զ����ȣ����¿�ʼ��ʱ��
        if (autoProgressStages)
        {
            stageTimerCoroutine = StartCoroutine(StageProgressionTimer());
        }

        if (debugMode)
            Debug.Log($"���������׶� {currentStage}: {GetCurrentStageName()}");
    }

    // ��ȡ�ܽ׶���
    public int GetTotalStages()
    {
        return stages.Length;
    }
}