using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어 이후의 이펙트, 클리어 UI, 다음 스테이지 및 스테이지 선택 이동을 관리한다.
/// </summary>
public sealed class StageClearController : MonoBehaviour
{
    [Header("클리어 UI")]
    [SerializeField] private RectTransform clearUiRoot;
    [SerializeField] private CanvasGroup clearUiCanvasGroup;
    [SerializeField] private Button stageSelectButton;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private StageCatalog stageCatalog;
    [SerializeField] private StageSessionController stageSessionController;

    [Header("클리어 이펙트")]
    [SerializeField] private GameObject clearEffectPrefab;
    [SerializeField] private SpriteRenderer gridEaselRenderer;
    [SerializeField, Min(0.01f)] private float clearEffectReferenceDiameter = 10f;
    [SerializeField] private string clearEffectSortingLayer = "GridBackground";
    [SerializeField] private int clearEffectSortingOrder = -4;
    [SerializeField, Min(0f)] private float clearEffectLifetime = 6f;
    [SerializeField, Min(1f)] private float smallParticleSizeMultiplier = 2.5f;
    [SerializeField, Min(1f)] private float particleLifetimeMultiplier = 2f;

    private GameObject activeClearEffect;

    private void Awake()
    {
        Nan.UI.UIButtonSound.Attach(stageSelectButton);
        Nan.UI.UIButtonSound.Attach(nextStageButton);
        Hide();
    }

    /// <summary>
    /// 지정한 스테이지의 클리어 연출을 재생하고 이동 UI를 표시한다.
    /// </summary>
    /// <param name="clearedStage">방금 클리어한 스테이지 데이터.</param>
    public void Show(StageData clearedStage)
    {
        nextStageButton.gameObject.SetActive(TryGetNextStage(clearedStage, out _));
        clearUiCanvasGroup.alpha = 1f;
        clearUiRoot.gameObject.SetActive(true);
        SoundManager.Instance?.PlaySfx(SoundKeys.StageClear);
        PlayClearEffect();
    }

    /// <summary>
    /// 클리어 UI와 재생 중인 클리어 이펙트를 숨긴다.
    /// </summary>
    public void Hide()
    {
        if (clearUiRoot != null)
        {
            clearUiRoot.gameObject.SetActive(false);
        }

        if (activeClearEffect != null)
        {
            Destroy(activeClearEffect);
            activeClearEffect = null;
        }
    }

    private bool TryGetNextStage(StageData currentStage, out StageData nextStage)
    {
        nextStage = null;
        if (stageCatalog == null)
        {
            return false;
        }

        for (int index = 0; index < stageCatalog.Count - 1; index++)
        {
            if (stageCatalog.GetStage(index) != currentStage)
            {
                continue;
            }

            nextStage = stageCatalog.GetStage(index + 1);
            return nextStage != null;
        }

        return false;
    }

    /// <summary>
    /// 현재 스테이지의 다음 스테이지를 같은 씬에서 불러오고 클리어 UI를 닫는다.
    /// </summary>
    public void LoadNextStage()
    {
        if (stageSessionController == null || !TryGetNextStage(stageSessionController.CurrentStage, out StageData nextStage))
        {
            return;
        }

        Hide();
        stageSessionController.LoadStage(nextStage);
    }

    /// <summary>
    /// 타이틀 씬으로 이동한 뒤 스테이지 선택 화면이 열리도록 요청한다.
    /// </summary>
    public void ReturnToStageSelection()
    {
        if (SceneTransitionManager.Instance.LoadScene("Title"))
        {
            StageRunContext.Instance.RequestReturnToStageSelection();
        }
    }

    private void PlayClearEffect()
    {
        if (clearEffectPrefab == null || gridEaselRenderer == null)
        {
            return;
        }

        if (activeClearEffect != null)
        {
            Destroy(activeClearEffect);
        }

        activeClearEffect = Instantiate(clearEffectPrefab, gridEaselRenderer.transform.position, Quaternion.identity);
        float scale = gridEaselRenderer.bounds.size.x / clearEffectReferenceDiameter;
        activeClearEffect.transform.localScale = Vector3.one * scale;

        foreach (Renderer renderer in activeClearEffect.GetComponentsInChildren<Renderer>())
        {
            renderer.sortingLayerName = clearEffectSortingLayer;
            renderer.sortingOrder = clearEffectSortingOrder;
        }

        foreach (ParticleSystem particleSystem in activeClearEffect.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.startLifetimeMultiplier *= particleLifetimeMultiplier;
            if (particleSystem.gameObject.name == "particle")
            {
                main.startSizeMultiplier *= smallParticleSizeMultiplier;
            }
        }

        Destroy(activeClearEffect, clearEffectLifetime);
    }

    private void OnDestroy()
    {
        Hide();
    }
}
