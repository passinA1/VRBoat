using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

public class RhythmJudgement : MonoBehaviour
{
    [System.Serializable]
    public class JudgementEvent : UnityEvent<JudgementResult> { }

    public enum Judgement { Perfect, Great, Good, Miss }

    [System.Serializable]
    public struct JudgementResult
    {
        public Judgement judgement;
        public RhythmGenerator.HandType handType;
        public RhythmGenerator.BeatType beatType;
        public float timeDeviation;
        public int beatID;
    }

    [Header("判定设置")]
    public float perfectThreshold = 0.05f;  // 完美判定阈值(秒)
    public float greatThreshold = 0.1f;     // 优秀判定阈值(秒)
    public float goodThreshold = 0.2f;      // 良好判定阈值(秒)
    public float inputBufferTime = 0.2f;    // 输入缓冲时间(秒)

    [Header("事件")]
    public JudgementEvent OnJudgementMade;  // 判定结果事件
    public UnityEvent OnComboBreak;        // 连击中断事件

    private Dictionary<int, RhythmGenerator.BeatInfo> activeBeats = new Dictionary<int, RhythmGenerator.BeatInfo>();
    private int currentCombo = 0;

    public void RegisterInput(RhythmGenerator.HandType inputHand, RhythmGenerator.BeatType inputType)
    {
        float currentTime = Time.time;
        int bestMatchID = -1;
        float bestMatchDeviation = float.MaxValue;
        Judgement bestJudgement = Judgement.Miss;

        // 查找所有活跃鼓点中最佳匹配
        foreach (var beatPair in activeBeats)
        {
            var beat = beatPair.Value;
            float timeDeviation = Mathf.Abs(currentTime - beat.hitTime);

            // 检查手型和类型是否匹配
            if ((beat.hand == inputHand || beat.hand == RhythmGenerator.HandType.Both) &&
                beat.type == inputType)
            {
                Judgement currentJudgement = CalculateJudgement(timeDeviation);

                // 找到偏差最小且判定最好的输入
                if (currentJudgement != Judgement.Miss &&
                   (timeDeviation < bestMatchDeviation ||
                    (timeDeviation == bestMatchDeviation && currentJudgement < bestJudgement)))
                {
                    bestMatchID = beatPair.Key;
                    bestMatchDeviation = timeDeviation;
                    bestJudgement = currentJudgement;
                }
            }
        }

        // 处理最佳匹配
        if (bestMatchID != -1)
        {
            var beat = activeBeats[bestMatchID];
            SendJudgement(bestJudgement, beat, bestMatchDeviation);
            activeBeats.Remove(bestMatchID);

            // 更新连击
            if (bestJudgement != Judgement.Miss)
            {
                currentCombo++;
            }
            else
            {
                BreakCombo();
            }
        }
        else
        {
            // 没有匹配的鼓点，判定为Miss
            BreakCombo();
        }
    }

    public void RegisterBeat(RhythmGenerator.BeatInfo beat)
    {
        // 添加新鼓点到活跃列表
        if (!activeBeats.ContainsKey(beat.beatID))
        {
            activeBeats.Add(beat.beatID, beat);

            // 设置超时检查
            StartCoroutine(CheckBeatMiss(beat.beatID, beat.hitTime + goodThreshold));
        }
    }

    private IEnumerator CheckBeatMiss(int beatID, float missTime)
    {
        yield return new WaitUntil(() => Time.time >= missTime || !activeBeats.ContainsKey(beatID));

        if (activeBeats.ContainsKey(beatID))
        {
            // 超时未击中
            var beat = activeBeats[beatID];
            SendJudgement(Judgement.Miss, beat, goodThreshold);
            activeBeats.Remove(beatID);
            BreakCombo();
        }
    }

    private Judgement CalculateJudgement(float timeDeviation)
    {
        if (timeDeviation <= perfectThreshold)
            return Judgement.Perfect;
        if (timeDeviation <= greatThreshold)
            return Judgement.Great;
        if (timeDeviation <= goodThreshold)
            return Judgement.Good;
        return Judgement.Miss;
    }

    private void SendJudgement(Judgement judgement, RhythmGenerator.BeatInfo beat, float timeDeviation)
    {
        var result = new JudgementResult
        {
            judgement = judgement,
            handType = beat.hand,
            beatType = beat.type,
            timeDeviation = timeDeviation,
            beatID = beat.beatID
        };

        OnJudgementMade.Invoke(result);

        // 调试输出
        string handText = beat.hand switch
        {
            RhythmGenerator.HandType.Left => "左手",
            RhythmGenerator.HandType.Both => "双手",
            _ => "右手"
        };
        Debug.Log($"{handText} {(beat.type == RhythmGenerator.BeatType.Single ? "单" : "双")}击: {judgement} (偏差: {timeDeviation:F3}s)");
    }

    private void BreakCombo()
    {
        if (currentCombo > 0)
        {
            currentCombo = 0;
            OnComboBreak.Invoke();
        }
    }

    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    public void ClearActiveBeats()
    {
        activeBeats.Clear();
    }
}