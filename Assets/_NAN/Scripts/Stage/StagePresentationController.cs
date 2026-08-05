using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 스테이지 제목, 설명, 정답 격자와 정답 액자 배치를 표시한다.
/// </summary>
public sealed class StagePresentationController : MonoBehaviour
{
    [SerializeField] private GridView answerGridView;
    [SerializeField] private Transform answerPaintingFrame;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    /// <summary>
    /// 전달받은 스테이지 데이터를 화면의 설명 및 정답 영역에 표시한다.
    /// </summary>
    /// <param name="stageData">표시할 스테이지 데이터.</param>
    public void Show(StageData stageData)
    {
        if (stageData == null)
        {
            throw new ArgumentNullException(nameof(stageData));
        }

        titleText.text = stageData.Title;
        descriptionText.text = stageData.Description;

        GridState answerState = new GridState(stageData.Width, stageData.Height, stageData.WallPositions);
        answerGridView.CreateGrid(answerState, false);
        ResizeAnswerPaintingFrame(stageData.Width, stageData.Height);
        answerGridView.SetCellPaintStates(stageData.AnswerPaintStates);
    }

    private void ResizeAnswerPaintingFrame(int width, int height)
    {
        if (answerPaintingFrame == null || width != height)
        {
            return;
        }

        float scale = width switch
        {
            5 => 0.42f,
            6 => 0.5f,
            7 => 0.575f,
            _ => answerPaintingFrame.localScale.x
        };
        answerPaintingFrame.localScale = new Vector3(scale, scale, 1f);
    }
}
