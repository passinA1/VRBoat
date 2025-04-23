using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreSystem : MonoBehaviour
{
    [Header("�÷ֹ���")]
    public int pointsPerPaddle = 10; // ÿ�λ��������÷�
    public int comboBonus = 2; // ÿ����������
    public int missPenalty = -5; // miss�ͷ�����
    public int syncPaddleBonus = 25; // ˫��ͬ����������

    [Header("�����ж�")]
    public float rhythmWindowTime = 0.5f; // �ж�����(��)
    public float perfectTiming = 0.1f; // �����ж�����(��)
    public float goodTiming = 0.2f; // �����ж�����(��)
                                    // ����goodTiming���ж�Ϊmiss

    [Header("��������")]
    public int comboThreshold = 10; // ����"ȫ��"״̬��������
    public float comboMultiplier = 1.5f; // "ȫ��"״̬�÷ֱ���
    public float fullPowerDuration = 5f; // "ȫ��"״̬����ʱ��

    [Header("���ҽ�������")]
    public bool requireAlternating = false; // �Ƿ�Ҫ�����ҽ��滮��
    private bool lastPaddleWasLeft = false; // ��һ���Ƿ�Ϊ��

    [Header("����ģʽ����")]
    public bool debugMode = false; // ����ģʽ
    public KeyCode perfectHitKey = KeyCode.F; // ǿ�������ж��İ���

    [Header("UI����")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI speedText;
    public GameObject fullPowerEffect;

    // ˽�б���
    private int score = 0;
    public int comboCount { get; private set; } = 0; // ���Ի����ṩ�ⲿ��ȡ
    private float lastPaddleTime = 0f;
    private bool isFullPower = false;
    private Coroutine fullPowerCoroutine;
    private DragonBoatMovement boatMovement;
    private int highestCombo = 0; // ���������¼

    // �ж�ö��
    public enum JudgmentResult { Perfect, Good, Miss }

    // ����
    private FeedbackSystem feedbackSystem;
    private UIManager uiManager;

    void Start()
    {
        // ��ȡ����
        boatMovement = FindObjectOfType<DragonBoatMovement>();
        feedbackSystem = FindObjectOfType<FeedbackSystem>();
        uiManager = FindObjectOfType<UIManager>();

        // ��ʼ��UI
        UpdateUI();

        // ���Ļ����¼�
        PaddleController.OnPaddleAction += OnPaddleAction;

        // ����˫��ͬ���¼�
        PaddleController.OnSyncPaddle += OnSyncPaddle;

        // ��ʼ��ȫ����Ч
        if (fullPowerEffect != null)
            fullPowerEffect.SetActive(false);

        if (uiManager != null && uiManager.fullPowerIndicator != null)
            uiManager.fullPowerIndicator.gameObject.SetActive(false);

        if (debugMode)
            Debug.Log("ScoreSystem����������ģʽ");
    }

    void OnDestroy()
    {
        // ȡ������
        PaddleController.OnPaddleAction -= OnPaddleAction;
        PaddleController.OnSyncPaddle -= OnSyncPaddle;
    }

    void Update()
    {
        // �����ٶ���ʾ
        if (speedText != null && boatMovement != null)
        {
            speedText.text = "Speed: " + boatMovement.GetCurrentSpeed().ToString("F1") + " m/s";
        }

        // ������Ϣ
        if (debugMode && Input.GetKeyDown(KeyCode.R))
        {
            ResetCombo();
            Debug.Log("�ֶ���������");
        }
    }

    // ��Ӧ˫��ͬ���¼�
    void OnSyncPaddle(float strength)
    {
        // ˫��ͬ���÷�
        int syncScore = syncPaddleBonus;

        // Ӧ����������
        syncScore += comboCount * comboBonus * 2; // ˫����������

        // �����"ȫ��"״̬���÷ּӳ�
        if (isFullPower)
        {
            syncScore = Mathf.RoundToInt(syncScore * comboMultiplier);
        }

        // ��������
        comboCount += 2; // ˫��ͬ��������������
        if (comboCount > highestCombo)
            highestCombo = comboCount;

        // ��ӵ÷�
        score += syncScore;

        // ��ʾ����
        if (feedbackSystem != null)
        {
            feedbackSystem.ShowTextFeedback("Row in unison! +" + syncScore, Color.cyan);
            feedbackSystem.PlayComboEffect(comboCount);
        }

        // ��������ƶ�������ڣ������ٶ�
        if (boatMovement != null)
            boatMovement.AddSpeed(strength * 1.5f);

        // ����Ƿ񴥷�"ȫ��"״̬
        if (comboCount >= comboThreshold && !isFullPower)
        {
            ActivateFullPower();
        }

        // ����UI
        UpdateUI();

        if (debugMode)
            Debug.Log($"ͬ������! �÷�+{syncScore}, ����:{comboCount}, ����:{strength}");
    }

    void OnPaddleAction(bool isLeftPaddle, float strength)
    {
        // �������ģʽ�°�ס�ض�����ǿ�������ж�
        bool forcePerfect = debugMode && Input.GetKey(perfectHitKey);

        // �������÷ֽ���
        float currentTime = Time.time;
        float timeSinceLastPaddle = currentTime - lastPaddleTime;

        JudgmentResult judgment = forcePerfect ? JudgmentResult.Perfect : JudgeRhythm(timeSinceLastPaddle);

        // ���Ҫ�����ҽ�������������0������Ƿ���Ͻ���Ҫ��
        if (requireAlternating && comboCount > 0 && isLeftPaddle == lastPaddleWasLeft && !forcePerfect)
        {
            judgment = JudgmentResult.Miss;

            // ��ʾ����
            if (feedbackSystem != null)
            {
                feedbackSystem.ShowTextFeedback("Alternating left-and-right rowing!", Color.red);
            }

            if (debugMode)
                Debug.Log("����ʧ��! ӦΪ" + (lastPaddleWasLeft ? "��" : "��") + "��");
        }

        ProcessJudgment(judgment, isLeftPaddle, strength);

        // ���������ּ�¼
        lastPaddleWasLeft = isLeftPaddle;

        // ������󻮽�ʱ��
        lastPaddleTime = currentTime;

        // ����UI
        UpdateUI();
    }

    JudgmentResult JudgeRhythm(float timeSinceLastPaddle)
    {
        // ��ֹͬһ���������
        if (timeSinceLastPaddle < 0.1f)
            return JudgmentResult.Miss;

        // ����Ƿ��ڽ��ര����
        if (timeSinceLastPaddle > rhythmWindowTime)
            return JudgmentResult.Miss;

        // ����Ƿ�������ʱ��
        if (Mathf.Abs(timeSinceLastPaddle - (rhythmWindowTime / 2)) < perfectTiming)
            return JudgmentResult.Perfect;

        // ����Ƿ�������ʱ��
        if (Mathf.Abs(timeSinceLastPaddle - (rhythmWindowTime / 2)) < goodTiming)
            return JudgmentResult.Good;

        // ����������Miss
        return JudgmentResult.Miss;
    }

    void ProcessJudgment(JudgmentResult judgment, bool isLeftPaddle, float strength)
    {
        int paddleScore = 0;

        switch (judgment)
        {
            case JudgmentResult.Perfect:
                // ��������
                comboCount++;
                if (comboCount > highestCombo)
                    highestCombo = comboCount;

                // ����÷�
                paddleScore = pointsPerPaddle * 2; // �����ж�˫������

                // Ӧ����������
                paddleScore += comboCount * comboBonus;

                // ��ʾ����
                if (feedbackSystem != null)
                    feedbackSystem.PlayPaddleEffect(isLeftPaddle ? new Vector3(-0.5f, 0, 0) : new Vector3(0.5f, 0, 0), true);

                // ��������ƶ�������ڣ������ٶ�
                if (boatMovement != null)
                    boatMovement.AddSpeed(strength * 1.5f);

                if (debugMode)
                    Debug.Log($"{(isLeftPaddle ? "��" : "��")}�������ж�! +{paddleScore}��, ����:{comboCount}");
                break;

            case JudgmentResult.Good:
                // ��������
                comboCount++;
                if (comboCount > highestCombo)
                    highestCombo = comboCount;

                // ����÷�
                paddleScore = pointsPerPaddle;

                // Ӧ����������
                paddleScore += comboCount * comboBonus;

                // ��ʾ����
                if (feedbackSystem != null)
                    feedbackSystem.PlayPaddleEffect(isLeftPaddle ? new Vector3(-0.5f, 0, 0) : new Vector3(0.5f, 0, 0), false);

                // ��������ƶ�������ڣ������ٶ�
                if (boatMovement != null)
                    boatMovement.AddSpeed(strength);

                if (debugMode)
                    Debug.Log($"{(isLeftPaddle ? "��" : "��")}�������ж�! +{paddleScore}��, ����:{comboCount}");
                break;

            case JudgmentResult.Miss:
                // ��������
                ResetCombo();

                // ����÷�
                paddleScore = missPenalty;

                // ��ʾ����
                if (feedbackSystem != null)
                    feedbackSystem.ShowMissFeedback();

                // ��������ƶ�������ڣ������ٶ�
                if (boatMovement != null)
                    boatMovement.ReduceSpeed(0.5f);

                if (debugMode)
                    Debug.Log($"{(isLeftPaddle ? "��" : "��")}��δ����! {paddleScore}��, ��������");
                break;
        }

        // �����"ȫ��"״̬���÷ּӳ�
        if (isFullPower && paddleScore > 0)
        {
            paddleScore = Mathf.RoundToInt(paddleScore * comboMultiplier);
        }

        // ��ӵ÷�
        score += paddleScore;
        if (score < 0) score = 0; // ��ֹ����

        // ����Ƿ񴥷�"ȫ��"״̬
        if (comboCount >= comboThreshold && !isFullPower)
        {
            ActivateFullPower();
        }
    }

    // ��������
    private void ResetCombo()
    {
        comboCount = 0;

        // �����ȫ��ģʽ��ȡ��ȫ��ģʽ
        if (isFullPower)
        {
            isFullPower = false;

            // �ر���Ч
            if (fullPowerEffect != null)
                fullPowerEffect.SetActive(false);

            // ����UI��ȫ��ָʾ��
            if (uiManager != null && uiManager.fullPowerIndicator != null)
                uiManager.fullPowerIndicator.gameObject.SetActive(false);

            // ����м�ʱЭ�̣�ֹͣ
            if (fullPowerCoroutine != null)
                StopCoroutine(fullPowerCoroutine);

            if (debugMode)
                Debug.Log("ȫ��״̬���ж�!");
        }
    }

    void ActivateFullPower()
    {
        isFullPower = true;

        // ������Ч
        if (fullPowerEffect != null)
            fullPowerEffect.SetActive(true);

        // ����UI��ȫ��ָʾ��
        if (uiManager != null && uiManager.fullPowerIndicator != null)
            uiManager.fullPowerIndicator.gameObject.SetActive(true);

        // ��ʾ����
        if (feedbackSystem != null)
        {
            feedbackSystem.ShowTextFeedback("FULL POWER!", Color.magenta);
            feedbackSystem.PlayFullPowerEffect();
        }

        if (debugMode)
            Debug.Log("ȫ��״̬�������" + fullPowerDuration + "��");

        // ������м�ʱЭ�̣���ֹͣ
        if (fullPowerCoroutine != null)
            StopCoroutine(fullPowerCoroutine);

        // �����µļ�ʱЭ��
        fullPowerCoroutine = StartCoroutine(FullPowerTimer());
    }

    IEnumerator FullPowerTimer()
    {
        yield return new WaitForSeconds(fullPowerDuration);

        // ����ȫ��״̬
        isFullPower = false;

        // �ر���Ч
        if (fullPowerEffect != null)
            fullPowerEffect.SetActive(false);

        // ����UI��ȫ��ָʾ��
        if (uiManager != null && uiManager.fullPowerIndicator != null)
            uiManager.fullPowerIndicator.gameObject.SetActive(false);

        if (debugMode)
            Debug.Log("ȫ��״̬����");
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (comboText != null)
            comboText.text = "Combo: " + comboCount;
    }

    // ��ȡ��ǰ�÷�
    public int GetScore()
    {
        return score;
    }

    // ��ȡ���������
    public int GetHighestCombo()
    {
        return highestCombo;
    }

    // �����Ƿ���Ҫ���ҽ���
    public void SetRequireAlternating(bool require)
    {
        requireAlternating = require;

        if (debugMode)
            Debug.Log("���ҽ���Ҫ��: " + (require ? "����" : "����"));
    }

    // �����÷��� - �ֶ���ӵ÷�
    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();

        if (debugMode)
            Debug.Log("�ֶ���ӷ���: " + amount);
    }

    // �����÷��� - �ֶ���������
    public void SetCombo(int amount)
    {
        comboCount = amount;
        if (comboCount > highestCombo)
            highestCombo = comboCount;

        UpdateUI();

        // ����Ƿ񴥷�"ȫ��"״̬
        if (comboCount >= comboThreshold && !isFullPower)
        {
            ActivateFullPower();
        }

        if (debugMode)
            Debug.Log("�ֶ���������: " + amount);
    }

    // �����÷��� - �ֶ�����ȫ��״̬
    public void ActivateFullPowerManually()
    {
        if (!isFullPower)
        {
            ActivateFullPower();
        }
    }
}