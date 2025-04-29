using UnityEngine;
using System.Collections;

public class DragonBoatMovement : MonoBehaviour
{
    [Header("�ƶ�����")]
    public float initialSpeed = 10.0f; // ��ʼ�ٶ���Ϊ10m/s
    public float minSpeed = 5.0f;     // ����ٶ���Ϊ5m/s
    public float maxSpeed = 20.0f;    // ����ٶ���Ϊ20m/s
    public float baseForwardForce = 3.0f; // ���ӻ���ǰ������ʹ����Ч��������
    public float dragForce = 3.0f;    // ��������ϵ��
    public float boatSmoothTime = 0.05f; // ����ƽ���ƶ�ʱ��϶�
    public float moveTimeAfterPaddle = 0.3f; // ����������ƶ���ʱ��

    [Header("ҡ������")]
    public bool enableRocking = true; // �Ƿ�����ҡ��Ч��
    public float rockingAmount = 3.0f; // ҡ������
    public float rockingSpeed = 0.5f; // ҡ���ٶ�
    public float paddleRockImpact = 2.0f; // ������ҡ����Ӱ��

    [Header("��������")]
    public bool debugMode = false; // ����ģʽ
    public KeyCode speedUpKey = KeyCode.UpArrow; // ���ټ�
    public KeyCode speedDownKey = KeyCode.DownArrow; // ���ټ�
    public KeyCode resetBoatKey = KeyCode.Backspace; // ���ô�λ�ü�
    public bool showTrajectory = true; // ��ʾ�켣
    public int trajectorySteps = 10; // �켣������
    public Color trajectoryColor = Color.cyan; // �켣��ɫ

    [Header("����")]
    public Transform trackEnd;
    public GameObject victoryScreen;

    // ˽�б���
    private float currentSpeed;
    private float distanceTraveled = 0f;
    private float totalDistance;
    private bool raceCompleted = false;
    private Vector3 velocity = Vector3.zero; // ����SmoothDamp
    private float lastPaddleTime = 0f; // �ϴλ���ʱ��
    private float currentRockAngle = 0f; // ��ǰҡ���Ƕ�
    private float targetRockAngle = 0f; // Ŀ��ҡ���Ƕ�
    private Vector3 initialPosition; // ��ʼλ��
    private Quaternion initialRotation; // ��ʼ��ת

    // �¼�ϵͳ����
    private GameManager gameManager;

    private UIManager uiManager;
    private ScoreSystem scoreSystem;

    void Start()
    {
        // ��ȡ����
        gameManager = FindObjectOfType<GameManager>();
        if(gameManager == null)
        {
            Debug.Log("gameManager is null");
        }
        uiManager = FindObjectOfType<UIManager>();
        if(uiManager == null)
        {
            Debug.Log("ui manager is null");
        }

        scoreSystem = FindObjectOfType<ScoreSystem>();
        if(scoreSystem == null)
        {
            Debug.Log("score system is null");
        }

        victoryScreen = uiManager.gameOverMenu;

        // �����ʼλ�ú���ת
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // ��ʼ���ٶ�Ϊ�趨ֵ
        currentSpeed = initialSpeed;

        // �����ܾ���
        if (trackEnd != null)
            totalDistance = trackEnd.position.z - transform.position.z;
        else
            totalDistance = 1000f; // Ĭ�Ͼ���

        // ����ʤ������
        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        // ���Ļ����¼�
        PaddleController.OnPaddleAction += OnPaddleAction;
        PaddleController.OnSyncPaddle += OnSyncPaddle;

        Debug.Log($"DragonBoatMovement��ʼ����ɣ��ܾ���: {totalDistance}����ʼ�ٶ�: {currentSpeed}m/s");
    }

    void OnDestroy()
    {
        // ȡ������
        PaddleController.OnPaddleAction -= OnPaddleAction;
        PaddleController.OnSyncPaddle -= OnSyncPaddle;
    }

    void Update()
    {
        if (raceCompleted)
            return;

        // ���Թ���
        if (debugMode)
        {
            HandleDebugInput();
        }

        // ����Ƿ�Ӧ�ü���
        float timeSinceLastPaddle = Time.time - lastPaddleTime;
        if (timeSinceLastPaddle > moveTimeAfterPaddle)
        {
            // �����ƶ�ʱ�����٣�������������ٶ�
            currentSpeed = Mathf.Lerp(currentSpeed, minSpeed, dragForce * Time.deltaTime);
        }
        else
        {
            // ���ƶ�ʱ������΢����
            currentSpeed = Mathf.Max(minSpeed, currentSpeed - (dragForce * 0.5f * Time.deltaTime));
        }

        // ���㴬��ҡ��
        if (enableRocking)
        {
            UpdateBoatRocking();
        }

        // �ƶ�����(ʹ��SmoothDampʵ��ƽ���ƶ�)
        Vector3 targetPosition = transform.position + Vector3.forward * (currentSpeed * Time.deltaTime);
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, boatSmoothTime);

        // �������н�����
        float delta = Vector3.Distance(transform.position, transform.position - velocity * Time.deltaTime);
        distanceTraveled += delta;

        // ����Ƿ���ɱ���
        if (distanceTraveled >= totalDistance && !raceCompleted)
        {
            raceCompleted = true;
            CompleteRace();
        }

        // ��ʾ���Թ켣
        if (debugMode && showTrajectory)
        {
            DrawTrajectory();
        }
    }

    // �����������
    private void HandleDebugInput()
    {
        // �ֶ�����
        if (Input.GetKey(speedUpKey))
        {
            currentSpeed = Mathf.Min(currentSpeed + (baseForwardForce * Time.deltaTime * 2), maxSpeed);
            if (debugMode)
            {
                //Debug.Log($"�ֶ����٣���ǰ�ٶ�: {currentSpeed:F2}m/s");
            }
        }

        // �ֶ�����
        if (Input.GetKey(speedDownKey))
        {
            currentSpeed = Mathf.Max(currentSpeed - (baseForwardForce * Time.deltaTime * 2), minSpeed);
            if (debugMode)
            { 
            //Debug.Log($"�ֶ����٣���ǰ�ٶ�: {currentSpeed:F2}m/s");
            }
        }

        // ���ô�λ��
        if (Input.GetKeyDown(resetBoatKey))
        {
            ResetBoat();
        }
    }

    // ���ô�
    public void ResetBoat()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        currentSpeed = initialSpeed;
        distanceTraveled = 0f;
        raceCompleted = false;

        // ����ʤ������
        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (debugMode)
            Debug.Log("�����ô�λ�ú�״̬");
    }

    // ����Ԥ��켣
    private void DrawTrajectory()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentVel = velocity;

        for (int i = 0; i < trajectorySteps; i++)
        {
            Vector3 nextPos = currentPos + Vector3.forward * (currentSpeed * Time.deltaTime);

            // ���ƹ켣��
            Debug.DrawLine(currentPos, nextPos, trajectoryColor);

            // ����λ��
            currentPos = nextPos;
        }
    }

    // ���´���ҡ��
    private void UpdateBoatRocking()
    {
        // ������Ȼҡ��
        float naturalRocking = Mathf.Sin(Time.time * rockingSpeed) * rockingAmount;

        // ƽ�����ɵ�Ŀ��ҡ���Ƕ�
        currentRockAngle = Mathf.Lerp(currentRockAngle, naturalRocking + targetRockAngle, Time.deltaTime * 2f);

        // Ӧ��ҡ�� - ֻ��z����ҡ�������Ұڶ���
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, currentRockAngle);

        // �𽥼���Ŀ��ҡ���Ƕȣ��ӻ���Ӱ��ָ���
        targetRockAngle = Mathf.Lerp(targetRockAngle, 0, Time.deltaTime * 3f);
    }

    // ��Ӧ�����¼�
    void OnPaddleAction(bool isLeftPaddle, float strength)
    {
        // ������󻮽�ʱ��
        lastPaddleTime = Time.time;

        // ÿ�λ��������ٶ�
        float addedSpeed = baseForwardForce * strength;
        currentSpeed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);

        // ���һ��С�ļ�ʱλ�ƣ�ʹÿ�λ����������Ե�ǰ����
        transform.position += Vector3.forward * 0.1f;
        distanceTraveled += 0.1f;

        // Ӱ�촬��ҡ��
        if (enableRocking)
        {
            // ���ҽ�Ӱ�첻ͬ�����ҡ��
            float rockImpact = isLeftPaddle ? -paddleRockImpact : paddleRockImpact;
            targetRockAngle += rockImpact;
        }

        if (debugMode)
            Debug.Log($"�յ�{(isLeftPaddle ? "��" : "��")}���������ٶ����ӵ�: {currentSpeed:F2}m/s��ǿ��: {strength:F2}");
    }

    // ��Ӧͬ�������¼�
    void OnSyncPaddle(float strength)
    {
        // ������󻮽�ʱ��
        lastPaddleTime = Time.time;

        // ͬ�������ṩ�������
        float addedSpeed = baseForwardForce * strength * 1.5f;
        currentSpeed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);

        // ͬ�������ṩ����ļ�ʱλ��
        transform.position += Vector3.forward * 0.3f;
        distanceTraveled += 0.3f;

        // ͬ������ƽ�⴬�壬����ҡ��
        if (enableRocking)
        {
            targetRockAngle = 0;
            currentRockAngle *= 0.5f; // ���ټ��ٵ�ǰҡ��
        }

        if (debugMode)
            Debug.Log($"ͬ���������ٶ����ӵ�: {currentSpeed:F2}m/s��ǿ��: {strength:F2}");
    }

    // �����ٶȵķ���
    public void AddSpeed(float strength)
    {
        // ������󻮽�ʱ��
        lastPaddleTime = Time.time;

        float addedSpeed = baseForwardForce * strength;
        currentSpeed = Mathf.Min(currentSpeed + addedSpeed, maxSpeed);

        // ���һ��С�ļ�ʱλ��
        transform.position += Vector3.forward * 0.2f;
        distanceTraveled += 0.2f;

        if (debugMode)
            Debug.Log($"�ٶ����ӵ�: {currentSpeed:F2}m/s��ǿ��: {strength:F2}");
    }

    // �����ٶȵķ������������������ٶ�
    public void ReduceSpeed(float factor)
    {
        currentSpeed *= (1f - factor);

        // ȷ���ٶȲ���������ٶ�
        if (currentSpeed < minSpeed)
            currentSpeed = minSpeed;

        if (debugMode)
            Debug.Log($"�ٶȼ��ٵ�: {currentSpeed:F2}m/s������: {factor:F2}");
    }

    void CompleteRace()
    {
        //uiManager.HideAllMenus();
        // ��ʾʤ������
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
            Debug.Log("victory screen set true");
        }
            

        Debug.Log("������ɣ��г�: " + distanceTraveled.ToString("F1") + " / " + totalDistance.ToString("F1"));

        // ֪ͨ��Ϸ������
        if (gameManager != null)
        {
            gameManager.TogglePause();
            gameManager.OnLevelCompleted(scoreSystem.GetScore());
            Debug.Log("game manager set pause");
        }
    }

    // ��ȡ��ɰٷֱȣ�����UI��ʾ
    public float GetCompletionPercentage()
    {
        return distanceTraveled / totalDistance;
    }

    // ��ȡ��ǰ�ٶȣ�����UI������ϵͳ
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // ���õ�ǰ�ٶȣ����ڵ��ԣ�
    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);

        if (debugMode)
            Debug.Log($"�ֶ������ٶ�Ϊ: {currentSpeed:F2}m/s");
    }

    // ģ����ɱ��������ڵ��ԣ�
    public void SimulateRaceCompletion()
    {
        if (!raceCompleted)
        {
            distanceTraveled = totalDistance;
            raceCompleted = true;
            CompleteRace();

            if (debugMode)
                Debug.Log("ģ����ɱ���");
        }
    }
}