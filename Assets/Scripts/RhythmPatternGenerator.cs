using UnityEngine;
using System.Collections;

public class RhythmPatternGenerator : MonoBehaviour
{
    [System.Serializable]
    public enum DrumPattern
    {
        SingleLeft,      // 单击左手
        SingleRight,     // 单击右手
        SingleBoth,      // 单击双手
        DoubleLeft,      // 双击左手
        DoubleRight,    // 双击右手
        DoubleBoth,     // 双击双手
        Alternating     // 交替左右手
    }

    [Header("节奏生成设置")]
    public float minInterval = 1.5f;    // 最小间隔时间
    public float maxInterval = 3.0f;    // 最大间隔时间
    public bool isGenerating = false;   // 是否正在生成节奏

    [Header("引用")]
    public RhythmGameManager gameManager;
    public DrummerHandController drummerController; // 鼓手控制器
    public Animator[] npcAnimators;

    private RhythmGenerator rhythmGenerator;

    private void Start()
    {
        // 自动查找鼓手控制器（如果未指定）
        if (drummerController == null)
        {
            drummerController = FindObjectOfType<DrummerHandController>();
        }
        
        StartCoroutine(GenerateRhythmPattern());
    }

    private IEnumerator GenerateRhythmPattern()
    {
        isGenerating = true;
        
        while (isGenerating)
        {
            // 随机生成一种鼓点类型
            DrumPattern pattern = (DrumPattern)Random.Range(0, System.Enum.GetValues(typeof(DrumPattern)).Length);
            
            // 生成节奏信息
            RhythmGenerator.BeatInfo beatInfo = new RhythmGenerator.BeatInfo
            {
                hand = GetHandTypeFromPattern(pattern),
                type = GetBeatTypeFromPattern(pattern)
            };

            // 触发预备动画
            if (drummerController != null)
            {
                // 提前0.5秒做准备动作
                switch (beatInfo.hand)
                {
                    case RhythmGenerator.HandType.Left:
                        drummerController.PrepareLeft();
                        break;
                    case RhythmGenerator.HandType.Right:
                        drummerController.PrepareRight();
                        break;
                    case RhythmGenerator.HandType.Both:
                        drummerController.PrepareBoth();
                        break;
                }
                
                // NPC也做出预备动作
                foreach (var npcAnimator in npcAnimators)
                {
                    if (npcAnimator != null)
                    {
                        npcAnimator.SetTrigger("Prepare");
                    }
                }
                
                // 等待准备动作完成
                yield return new WaitForSeconds(0.5f);
                
                // 播放击打动画
                drummerController.PlayBeatAnimation(beatInfo);
            }

            // 通知游戏管理器生成新的节拍
            if (gameManager != null)
            {
                gameManager.HandleBeatSpawned(beatInfo);
            }

            // 随机等待时间
            float interval = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    // 从模式获取手部类型
    private RhythmGenerator.HandType GetHandTypeFromPattern(DrumPattern pattern)
    {
        switch (pattern)
        {
            case DrumPattern.SingleLeft:
            case DrumPattern.DoubleLeft:
                return RhythmGenerator.HandType.Left;
            case DrumPattern.SingleRight:
            case DrumPattern.DoubleRight:
                return RhythmGenerator.HandType.Right;
            case DrumPattern.SingleBoth:
            case DrumPattern.DoubleBoth:
                return RhythmGenerator.HandType.Both;
            case DrumPattern.Alternating:
                // 交替使用左右手
                return Random.value < 0.5f ? RhythmGenerator.HandType.Left : RhythmGenerator.HandType.Right;
            default:
                return RhythmGenerator.HandType.Right;
        }
    }

    // 从模式获取节拍类型
    private RhythmGenerator.BeatType GetBeatTypeFromPattern(DrumPattern pattern)
    {
        switch (pattern)
        {
            case DrumPattern.DoubleLeft:
            case DrumPattern.DoubleRight:
            case DrumPattern.DoubleBoth:
                return RhythmGenerator.BeatType.Double;
            default:
                return RhythmGenerator.BeatType.Single;
        }
    }
    
    // 停止生成
    public void StopGeneration()
    {
        isGenerating = false;
    }
    
    // 开始生成
    public void StartGeneration()
    {
        if (!isGenerating)
        {
            isGenerating = true;
            StartCoroutine(GenerateRhythmPattern());
        }
    }
}