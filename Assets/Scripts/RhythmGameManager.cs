using UnityEngine;
using UnityEngine.Events;

public class RhythmGameManager : MonoBehaviour
{
    [System.Serializable]
    public class ScoreEvent : UnityEvent<int> { }

    [Header("得分设置")]
    public int perfectScore = 100;
    public int greatScore = 80;
    public int goodScore = 50;
    
    [Header("连击加成")]
    public float comboMultiplier = 0.1f; // 每10连击增加10%分数
    
    [Header("引用")]
    public RhythmJudgement judgement;
    public RhythmGenerator generator;
    public DrummerHandController drummerController; // Replace drummerAnimator with drummerController
    public Animator[] npcAnimators;
    
    [Header("事件")]
    public ScoreEvent OnScoreChanged;
    
    private int currentScore = 0;
    
    void Start()
    {
        if (judgement != null)
        {
            judgement.OnJudgementMade.AddListener(HandleJudgement);
        }
        
        if (generator != null)
        {
            generator.OnBeatSpawned.AddListener(HandleBeatSpawned);
        }
    }
    
    private void HandleJudgement(RhythmJudgement.JudgementResult result)
    {
        // 计算基础分数
        int baseScore = result.judgement switch
        {
            RhythmJudgement.Judgement.Perfect => perfectScore,
            RhythmJudgement.Judgement.Great => greatScore,
            RhythmJudgement.Judgement.Good => goodScore,
            _ => 0
        };
        
        // 计算连击加成
        float comboBonus = 1f + (judgement.GetCurrentCombo() * comboMultiplier);
        int finalScore = Mathf.RoundToInt(baseScore * comboBonus);
        
        // 更新总分
        if (finalScore > 0)
        {
            currentScore += finalScore;
            OnScoreChanged.Invoke(currentScore);
        }
        
        // 触发鼓手动作
        if (drummerController != null)
        {
            drummerController.PlayBeatAnimation(new RhythmGenerator.BeatInfo
            {
                hand = result.handType,
                type = result.beatType,
                hitTime = Time.time,
                spawnTime = Time.time,
                beatID = -1 // 使用-1表示这是判定触发的动画
            });
        }
        
        // 根据判定结果触发NPC反应
        if (npcAnimators != null && result.judgement != RhythmJudgement.Judgement.Miss)
        {
            foreach (var npcAnimator in npcAnimators)
            {
                if (npcAnimator != null)
                {
                    // 完美判定时NPC做出更热烈的反应
                    string reactionTrigger = (result.judgement == RhythmJudgement.Judgement.Perfect) 
                        ? "ExcitedReaction" 
                        : "NormalReaction";
                    npcAnimator.SetTrigger(reactionTrigger);
                }
            }
        }
    }
    
    public void HandleBeatSpawned(RhythmGenerator.BeatInfo beat)
    {
        // Update to use drummerController instead of drummerAnimator
        if (drummerController != null)
        {
            switch (beat.hand)
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
        }
    }
    
    // 删除不再需要的GetAnimationTrigger和GetPrepareTrigger方法，因为这些逻辑已经移到DrummerHandController中
    
    private string GetAnimationTrigger(RhythmGenerator.HandType hand, RhythmGenerator.BeatType type)
    {
        string prefix = type == RhythmGenerator.BeatType.Single ? "Single" : "Double";
        return hand switch
        {
            RhythmGenerator.HandType.Left => prefix + "LeftHit",
            RhythmGenerator.HandType.Right => prefix + "RightHit",
            RhythmGenerator.HandType.Both => prefix + "BothHit",
            _ => "SingleRightHit" // 默认动作
        };
    }
    
    private string GetPrepareTrigger(RhythmGenerator.HandType hand)
    {
        return hand switch
        {
            RhythmGenerator.HandType.Left => "PrepareLeft",
            RhythmGenerator.HandType.Right => "PrepareRight",
            RhythmGenerator.HandType.Both => "PrepareBoth",
            _ => "PrepareRight" // 默认准备动作
        };
    }
    
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged.Invoke(currentScore);
    }
}