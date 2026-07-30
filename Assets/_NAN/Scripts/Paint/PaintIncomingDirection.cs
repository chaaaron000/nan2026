using System;

/// <summary>
/// 물감이 대상 셀로 들어온 방향을 비트 플래그로 표현한다.
/// 같은 최단 거리의 여러 경로가 한 셀에 도달하면 여러 값이 함께 저장된다.
/// </summary>
[Flags]
public enum PaintIncomingDirection
{
    /// <summary>확산 시작 셀이거나 들어온 방향이 없음을 나타낸다.</summary>
    None = 0,

    /// <summary>아래 셀에서 위로 이동해 대상 셀에 들어왔다.</summary>
    FromBelow = 1 << 0,

    /// <summary>왼쪽 셀에서 오른쪽으로 이동해 대상 셀에 들어왔다.</summary>
    FromLeft = 1 << 1,

    /// <summary>위 셀에서 아래로 이동해 대상 셀에 들어왔다.</summary>
    FromAbove = 1 << 2,

    /// <summary>오른쪽 셀에서 왼쪽으로 이동해 대상 셀에 들어왔다.</summary>
    FromRight = 1 << 3,
}
