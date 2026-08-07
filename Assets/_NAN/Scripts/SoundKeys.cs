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

    /// <summary>UI 버튼에 마우스 포인터가 들어왔을 때 재생할 효과음 키를 반환한다.</summary>
    public const string UiButtonHover = "ui_button_hover";

    /// <summary>UI 버튼을 클릭했을 때 재생할 효과음 키를 반환한다.</summary>
    public const string UiButtonClick = "ui_button_click";

    /// <summary>씬 전환 연출이 시작될 때 재생할 효과음 키를 반환한다.</summary>
    public const string SceneTransition = "scene_transition";

    /// <summary>스테이지 클리어 연출이 시작될 때 재생할 효과음 키를 반환한다.</summary>
    public const string StageClear = "stage_clear";

    /// <summary>스테이지 화면에서 재생할 배경음 키를 반환한다.</summary>
    public const string StageBgm = "stage_bgm";
    
    /// <summary>타이틀 화면에서 재생할 배경음 키를 반환한다.</summary>
    public const string TitleBgm = "title_bgm";
}
