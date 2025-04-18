using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RhythmGenerator : MonoBehaviour
{
    [System.Serializable]
    public class BeatEvent : UnityEvent<BeatInfo> { }

    public enum HandType { Left, Both, Right }
    public enum BeatType { Single, Double } // 保留原有的单/双类型

    public struct BeatInfo
    {
        public HandType hand;
        public BeatType type;
        public float hitTime; // 应该击打的时间点
        public float spawnTime; // 生成时间点
    }

    [Header("生成设置")]
    public float minInterval = 0.5f;
    public float maxInterval = 1.2f;
    public float leadTime = 2f; // 提前生成时间
    [Tooltip("是否随机生成双手鼓点")]
    public bool allowBothHands = true;

    [Header("事件")]
    public BeatEvent OnBeatSpawned; // 鼓点生成时触发
    public UnityEvent OnGenerationStarted;
    public UnityEvent OnGenerationEnded;

    private bool isGenerating = false;
    private float nextBeatTime = 0f;
    private float endTime = 0f;

    void Update()
    {
        if (!isGenerating) return;

        if (Time.time >= nextBeatTime && Time.time < endTime)
        {
            SpawnRandomBeat();
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

    private void SpawnRandomBeat()
    {
        BeatInfo newBeat = new BeatInfo
        {
            hand = GetRandomHandType(),
            type = (BeatType)Random.Range(0, 2), // 保留单/双类型
            hitTime = Time.time + leadTime,
            spawnTime = Time.time
        };

        OnBeatSpawned.Invoke(newBeat);
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
}