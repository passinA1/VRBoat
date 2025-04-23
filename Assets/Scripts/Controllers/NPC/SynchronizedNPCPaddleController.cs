using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ʵ��ͬ����NPC������������ȷ�����NPC������ȫһ�µĻ��������ģʽ
/// �ر���������ҪNPC�������ҽ�����ͳһ�������������
/// </summary>
public class SynchronizedNPCPaddleController : MonoBehaviour
{
    [Header("NPC����")]
    public List<Transform> npcLeftPaddles = new List<Transform>(); // ����NPC����
    public List<Transform> npcRightPaddles = new List<Transform>(); // ����NPC���ҽ�

    [Header("��������")]
    public float paddleForwardAmount = 0.5f; // ��ǰ�ƶ���
    public float paddleBackwardAmount = 0.7f; // ���������
    public float paddleAnimSpeed = 7f; // ���������ٶ�
    public float paddleRecoveryTime = 0.1f; // ���ָ�ʱ��

    [Header("��������")]
    public float paddleInterval = 0.7f; // �������
    public bool startWithLeftPaddle = true; // ���󽰿�ʼ

    [Header("��Ϸ����")]
    public bool adaptToGameStage = true; // ������Ϸ�׶ε���
    public bool autoStart = true; // �Զ���ʼ����

    // ˽�б���
    private List<Vector3> leftPaddleRestPositions = new List<Vector3>();
    private List<Vector3> rightPaddleRestPositions = new List<Vector3>();
    private bool isPaddling = false;
    private Coroutine paddlingCoroutine;
    private bool isLeftPaddleTurn = true; // ��ǰ�Ƿ��ֵ���

    // ��̬��������ȫ��ͬ��
    public static SynchronizedNPCPaddleController instance;

    // ����
    private GameManager gameManager;
    private StageManager stageManager;

    void Awake()
    {
        // ����ģʽȷ��ֻ��һ��������ʵ��
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // ��ȡ����
        gameManager = FindObjectOfType<GameManager>();
        stageManager = FindObjectOfType<StageManager>();

        // ��ʼ�����������н��ĳ�ʼλ��
        if (npcLeftPaddles.Count != npcRightPaddles.Count)
        {
            Debug.LogError("�󽰺��ҽ�������ƥ�䣡��ȷ��ÿ��NPC���ж�Ӧ�����ҽ���");
            return;
        }

        // �������н��ĳ�ʼλ��
        foreach (Transform paddle in npcLeftPaddles)
        {
            if (paddle != null)
                leftPaddleRestPositions.Add(paddle.localPosition);
            else
                leftPaddleRestPositions.Add(Vector3.zero); // ռλ
        }

        foreach (Transform paddle in npcRightPaddles)
        {
            if (paddle != null)
                rightPaddleRestPositions.Add(paddle.localPosition);
            else
                rightPaddleRestPositions.Add(Vector3.zero); // ռλ
        }

        // ���ó�ʼ��������
        isLeftPaddleTurn = startWithLeftPaddle;

        // �Զ���ʼ����
        if (autoStart)
        {
            StartPaddling();
        }
    }

    void Update()
    {
        // ������Ϸ�׶ε�����Ϊ
        if (adaptToGameStage)
        {
            UpdateBehaviorForCurrentStage();
        }
    }

    /// <summary>
    /// ����ͬ������Э��
    /// </summary>
    public void StartPaddling()
    {
        if (!isPaddling)
        {
            if (paddlingCoroutine != null)
                StopCoroutine(paddlingCoroutine);

            paddlingCoroutine = StartCoroutine(SynchronizedPaddlingRoutine());
        }
    }

    /// <summary>
    /// ֹͣ����
    /// </summary>
    public void StopPaddling()
    {
        if (isPaddling && paddlingCoroutine != null)
        {
            StopCoroutine(paddlingCoroutine);
            paddlingCoroutine = null;
            isPaddling = false;
        }
    }

    /// <summary>
    /// ͬ������Э�̣�ȷ������NPC����һ�µĻ��������ģʽ
    /// </summary>
    private IEnumerator SynchronizedPaddlingRoutine()
    {
        isPaddling = true;
        float nextPaddleTime = Time.time;

        while (isPaddling)
        {
            // �ȴ�������һ�λ���ʱ�䣬ȷ����ȷ��ʱ�����
            while (Time.time < nextPaddleTime)
            {
                yield return null;
            }

            // ִ�е�ǰpaddle�Ļ�������
            if (isLeftPaddleTurn)
            {
                yield return StartCoroutine(AnimateAllPaddles(true));
            }
            else
            {
                yield return StartCoroutine(AnimateAllPaddles(false));
            }

            // �л�����һ��
            isLeftPaddleTurn = !isLeftPaddleTurn;

            // ��ȷ������һ�λ���ʱ��
            nextPaddleTime = Time.time + paddleInterval;
        }
    }

    /// <summary>
    /// ͬʱΪ����NPC��ͬһ�཰ִ�л�������
    /// </summary>
    private IEnumerator AnimateAllPaddles(bool isLeft)
    {
        List<Transform> paddlesToAnimate = isLeft ? npcLeftPaddles : npcRightPaddles;
        List<Vector3> restPositions = isLeft ? leftPaddleRestPositions : rightPaddleRestPositions;

        if (paddlesToAnimate.Count == 0)
            yield break;

        // ���㶯��ʱ��
        float strokeTime = 1f / paddleAnimSpeed;

        // 1. ��ǰ�ƶ���
        float forwardTime = strokeTime / 4;
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < forwardTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / forwardTime);

            // ͬʱ�������н���λ��
            for (int i = 0; i < paddlesToAnimate.Count; i++)
            {
                if (paddlesToAnimate[i] != null && i < restPositions.Count)
                {
                    Vector3 restPos = restPositions[i];
                    Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
                    paddlesToAnimate[i].localPosition = Vector3.Lerp(restPos, forwardPos, progress);
                }
            }

            yield return null;
        }

        // ȷ�����н�������ǰ��λ��
        for (int i = 0; i < paddlesToAnimate.Count; i++)
        {
            if (paddlesToAnimate[i] != null && i < restPositions.Count)
            {
                Vector3 restPos = restPositions[i];
                Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
                paddlesToAnimate[i].localPosition = forwardPos;
            }
        }

        // 2. ���������
        float backwardTime = strokeTime / 2;
        startTime = Time.time;
        elapsedTime = 0f;

        while (elapsedTime < backwardTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / backwardTime);

            // ͬʱ�������н���λ��
            for (int i = 0; i < paddlesToAnimate.Count; i++)
            {
                if (paddlesToAnimate[i] != null && i < restPositions.Count)
                {
                    Vector3 restPos = restPositions[i];
                    Vector3 forwardPos = restPos + new Vector3(0, 0, paddleForwardAmount);
                    Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
                    paddlesToAnimate[i].localPosition = Vector3.Lerp(forwardPos, backwardPos, progress);
                }
            }

            yield return null;
        }

        // ȷ�����н����������λ��
        for (int i = 0; i < paddlesToAnimate.Count; i++)
        {
            if (paddlesToAnimate[i] != null && i < restPositions.Count)
            {
                Vector3 restPos = restPositions[i];
                Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
                paddlesToAnimate[i].localPosition = backwardPos;
            }
        }

        // 3. �ָ�����ʼλ��
        float recoveryTime = paddleRecoveryTime;
        startTime = Time.time;
        elapsedTime = 0f;

        while (elapsedTime < recoveryTime)
        {
            elapsedTime = Time.time - startTime;
            float progress = Mathf.Clamp01(elapsedTime / recoveryTime);

            // ͬʱ�������н���λ��
            for (int i = 0; i < paddlesToAnimate.Count; i++)
            {
                if (paddlesToAnimate[i] != null && i < restPositions.Count)
                {
                    Vector3 restPos = restPositions[i];
                    Vector3 backwardPos = restPos - new Vector3(0, 0, paddleBackwardAmount);
                    paddlesToAnimate[i].localPosition = Vector3.Lerp(backwardPos, restPos, progress);
                }
            }

            yield return null;
        }

        // ȷ�����н����ص���ȷ�ĳ�ʼλ��
        for (int i = 0; i < paddlesToAnimate.Count; i++)
        {
            if (paddlesToAnimate[i] != null && i < restPositions.Count)
            {
                paddlesToAnimate[i].localPosition = restPositions[i];
            }
        }
    }

    /// <summary>
    /// ���ݵ�ǰ��Ϸ�׶θ���NPC��Ϊ
    /// </summary>
    private void UpdateBehaviorForCurrentStage()
    {
        int currentStage = 1;

        // ��ȡ��ǰ�׶�
        if (gameManager != null)
            currentStage = gameManager.currentStage;
        else if (stageManager != null)
            currentStage = stageManager.currentStage;

        // ���ݽ׶ε����������
        switch (currentStage)
        {
            case 1: // ��������
                paddleInterval = 0.8f; // �������������ѧϰ
                break;

            case 2: // ����
                paddleInterval = 0.7f; // �Կ�
                break;

            case 3: // �������
                paddleInterval = 0.6f; // �ٿ�һЩ
                break;

            case 4: // ��ʽ����
                paddleInterval = 0.5f; // ���ٽ���
                break;
        }
    }

    /// <summary>
    /// ��������NPC�����Ľ�����
    /// </summary>
    public void SetPaddleInterval(float interval)
    {
        paddleInterval = Mathf.Max(0.2f, interval); // ��ֹ�����С
    }

    /// <summary>
    /// �������н���λ�õ���ʼ״̬
    /// </summary>
    public void ResetAllPaddles()
    {
        for (int i = 0; i < npcLeftPaddles.Count; i++)
        {
            if (npcLeftPaddles[i] != null && i < leftPaddleRestPositions.Count)
            {
                npcLeftPaddles[i].localPosition = leftPaddleRestPositions[i];
            }
        }

        for (int i = 0; i < npcRightPaddles.Count; i++)
        {
            if (npcRightPaddles[i] != null && i < rightPaddleRestPositions.Count)
            {
                npcRightPaddles[i].localPosition = rightPaddleRestPositions[i];
            }
        }
    }

    /// <summary>
    /// �л���ʼ��
    /// </summary>
    public void SetStartWithLeftPaddle(bool startLeft)
    {
        // ֻ���ڷǻ���״̬�£����ܸı���ʼ��
        if (!isPaddling)
        {
            startWithLeftPaddle = startLeft;
            isLeftPaddleTurn = startLeft;
        }
    }

    /// <summary>
    /// ��ȡ��ǰ����ʹ�õĽ�������ң�
    /// </summary>
    public bool IsLeftPaddleTurn()
    {
        return isLeftPaddleTurn;
    }

    void OnDisable()
    {
        StopPaddling();
    }

    void OnDestroy()
    {
        StopPaddling();

        // �����������
        if (instance == this)
        {
            instance = null;
        }
    }
}