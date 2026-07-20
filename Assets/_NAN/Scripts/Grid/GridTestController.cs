using UnityEngine;

/// <summary>
/// 임시 UI 버튼으로 5x5 격자 생성을 확인하기 위한 테스트 컴포넌트.
/// </summary>
public sealed class GridTestController : MonoBehaviour
{
    private const int TestWidth = 5;
    private const int TestHeight = 5;

    [SerializeField]
    private GridView gridView;

    private GridState gridState;

    /// <summary>
    /// UI Button의 OnClick 이벤트에 연결한다.
    /// </summary>
    public void CreateTestGrid()
    {
        gridState = new GridState(TestWidth, TestHeight);
        gridView.CreateGrid(gridState);
    }
}
