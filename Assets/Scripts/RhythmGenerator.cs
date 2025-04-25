using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class RhythmGenerator : MonoBehaviour
{
    [System.Serializable]
    public class BeatEvent : UnityEvent<BeatInfo> { }

    public enum HandType { Left, Both, Right }
    public enum BeatType { Single, Double }

    [System.Serializable]
    public struct BeatInfo
    {
        public HandType hand;
        public BeatType type;
        public float hitTime; // 应该击打的时间点
        public float spawnTime; // 生成时间点
        public int beatID; // 唯一标识符
    }

    [Header("生成设置")]
    public float minInterval = 0.5f;
    public float maxInterval = 1.2f;
    public float leadTime = 2f; // 提前生成时间
    [Tooltip("是否随机生成双手鼓点")]
    public bool allowBothHands = true;
    [Tooltip("双拍概率")]
    [Range(0, 1)] public float doubleBeatProbability = 0.3f;

    [Header("节奏模式")]
    public bool usePattern = false;
    public List<BeatInfo> beatPattern = new List<BeatInfo>();
    private int patternIndex = 0;

    [Header("事件")]
    public BeatEvent OnBeatSpawned; // 鼓点生成时触发
    public UnityEvent OnGenerationStarted;
    public UnityEvent OnGenerationEnded;

    [Header("NPC设置")]
    // Move the Header attribute to the field declarations
    [SerializeField] private DrummerHandController drummerController;
    [SerializeField] private Animator[] npcAnimators;

    public enum GenerationMode
    {
        Random,     // 随机生成
        Pattern,    // 固定模式
        Alternating // 交替模式
    }
    
    [Header("模式设置")]
    [SerializeField] private GenerationMode generationMode = GenerationMode.Random;

    private bool isGenerating = false;
    private float nextBeatTime = 0f;
    private float endTime = 0f;
    private int nextBeatID = 0;

    private void Start()
    {
        if (drummerController == null)
        {
            drummerController = FindObjectOfType<DrummerHandController>();
        }
    }

    protected virtual void Update()
    {
        if (!isGenerating) return;

        if (Time.time >= nextBeatTime && Time.time < endTime)
        {
            GenerateNextBeat();
            ScheduleNextBeat();
        }
        else if (Time.time >= endTime)
        {
            StopGenerating();
        }
    }

    public void StartGenerating(float duration)
    {
        isGenerating = true;
        endTime = Time.time + duration;
        patternIndex = 0;
        ScheduleNextBeat();
        OnGenerationStarted.Invoke();
    }

    public void StopGenerating()
    {
        isGenerating = false;
        OnGenerationEnded.Invoke();
    }

    private void ScheduleNextBeat()
    {
        nextBeatTime = Time.time + Random.Range(minInterval, maxInterval);
    }

    private void GenerateNextBeat()
    {
        BeatInfo beatInfo = new BeatInfo();
        
        switch (generationMode)
        {
            case GenerationMode.Pattern:
                if (beatPattern.Count > 0)
                {
                    beatInfo = beatPattern[patternIndex];
                    patternIndex = (patternIndex + 1) % beatPattern.Count;
                }
                break;
                
            case GenerationMode.Alternating:
                beatInfo.hand = (patternIndex % 2 == 0) ? HandType.Left : HandType.Right;
                beatInfo.type = Random.value < doubleBeatProbability ? BeatType.Double : BeatType.Single;
                patternIndex++;
                break;
                
            default: // Random mode
                beatInfo.hand = GetRandomHandType();
                beatInfo.type = GetRandomBeatType();
                break;
        }

        beatInfo.hitTime = Time.time + leadTime;
        beatInfo.spawnTime = Time.time;
        beatInfo.beatID = nextBeatID++;

        // 处理鼓手动画
        if (drummerController != null)
        {
            StartCoroutine(HandleDrummerAnimation(beatInfo));
        }

        OnBeatSpawned.Invoke(beatInfo);
    }

    private HandType GetRandomHandType()
    {
        if (!allowBothHands)
        {
            return (HandType)Random.Range(0, 2); // 只生成左或右
        }

        // 30%概率生成双手，35%左，35%右
        float rand = Random.value;
        if (rand < 0.3f) return HandType.Both;
        return rand < 0.65f ? HandType.Left : HandType.Right;
    }

    private BeatType GetRandomBeatType()
    {
        return Random.value < doubleBeatProbability ? BeatType.Double : BeatType.Single;
    }

    private IEnumerator HandleDrummerAnimation(BeatInfo beatInfo)
    {
        // 根据手的类型触发准备动作
        switch (beatInfo.hand)
        {
            case HandType.Left:
                drummerController.PrepareLeft();
                break;
            case HandType.Right:
                drummerController.PrepareRight();
                break;
            case HandType.Both:
                drummerController.PrepareBoth();
                break;
        }

        // 等待准备时间
        yield return new WaitForSeconds(0.5f);

        // 播放击打动画
        drummerController.PlayBeatAnimation(beatInfo);
    }

    // 添加预设节奏模式
    public void SetBeatPattern(List<BeatInfo> pattern)
    {
        beatPattern = pattern;
        usePattern = pattern != null && pattern.Count > 0;
    }

    // 清除当前节奏模式
    public void ClearBeatPattern()
    {
        beatPattern.Clear();
        usePattern = false;
    }
}