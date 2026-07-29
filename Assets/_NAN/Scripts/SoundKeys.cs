/// <summary>
/// 프로젝트에서 사용하는 사운드 라이브러리 키를 중앙에서 관리한다.
/// </summary>
public static class SoundKeys
{
    /// <summary>물감통을 선택할 때 재생할 효과음 키를 반환한다.</summary>
    public const string PaintBucketSelect = "paint_bucket_select";

    /// <summary>물감통 사용에 성공했을 때 재생할 효과음 키를 반환한다.</summary>
    public const string PaintBucketUse = "paint_bucket_use";

    /// <summary>전체 되돌리기 버튼을 눌렀을 때 재생할 효과음 키를 반환한다.</summary>
    public const string Erase = "erase";

    /// <summary>마지막 명령을 되돌렸을 때 재생할 효과음 키를 반환한다.</summary>
    public const string Undo = "undo";

    /// <summary>스테이지 화면에서 재생할 배경음 키를 반환한다.</summary>
    public const string StageBgm = "stage_bgm";
}
