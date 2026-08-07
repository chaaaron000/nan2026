using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nan.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// 화면 전환 연출과 단일 씬의 비동기 로드를 전역에서 관리합니다.
/// </summary>
public sealed class SceneTransitionManager : LazyPersistentSingleton<SceneTransitionManager>
{
    private const string TransitionUIResourcePath = "SceneTransitionUI";

    private UISceneTransition transitionUI;
    private bool isTransitioning;
    private bool waitsForSceneReady;
    private bool sceneReadyReceived;
    private string targetSceneName;

    /// <summary>
    /// 현재 씬 전환이 진행 중인지 반환합니다.
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// 지정한 씬을 비동기로 로드하고 로드 완료 직후 화면을 다시 표시합니다.
    /// </summary>
    /// <param name="sceneName">Build Settings에 등록된 대상 씬 이름입니다.</param>
    /// <returns>전환 요청을 시작했으면 true, 요청을 거절했으면 false입니다.</returns>
    public bool LoadScene(string sceneName)
    {
        return TryStartTransition(sceneName, false);
    }

    /// <summary>
    /// 지정한 씬을 비동기로 로드하고 준비 완료 신호가 올 때까지 화면을 가립니다.
    /// </summary>
    /// <param name="sceneName">Build Settings에 등록된 대상 씬 이름입니다.</param>
    /// <returns>전환 요청을 시작했으면 true, 요청을 거절했으면 false입니다.</returns>
    public bool LoadSceneAndWaitForReady(string sceneName)
    {
        return TryStartTransition(sceneName, true);
    }

    /// <summary>
    /// 현재 로드 대상 씬의 콘텐츠 준비가 완료되었음을 알립니다.
    /// </summary>
    public void NotifySceneReady()
    {
        if (!isTransitioning || !waitsForSceneReady || sceneReadyReceived)
        {
            return;
        }

        Scene activeScene = UnitySceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, targetSceneName, StringComparison.Ordinal))
        {
            return;
        }

        sceneReadyReceived = true;
    }

    private bool TryStartTransition(string sceneName, bool waitForSceneReady)
    {
        if (isTransitioning)
        {
            DebugConsole.LogWarning(
                $"[SceneTransitionManager] Scene transition is already in progress: {targetSceneName}",
                this
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            DebugConsole.LogError("[SceneTransitionManager] Scene name is empty.", this);
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            DebugConsole.LogError(
                $"[SceneTransitionManager] Scene is not available in Build Settings: {sceneName}",
                this
            );
            return false;
        }

        if (!TryCreateTransitionUI())
        {
            return false;
        }

        isTransitioning = true;
        waitsForSceneReady = waitForSceneReady;
        sceneReadyReceived = false;
        targetSceneName = sceneName;

        SoundManager.Instance?.PlaySfx(SoundKeys.SceneTransition);
        RunTransitionAsync(sceneName).Forget();
        return true;
    }

    private bool TryCreateTransitionUI()
    {
        if (transitionUI != null)
        {
            return true;
        }

        UISceneTransition transitionPrefab = Resources.Load<UISceneTransition>(TransitionUIResourcePath);
        if (transitionPrefab == null)
        {
            DebugConsole.LogError(
                $"[SceneTransitionManager] Transition UI was not found in Resources: {TransitionUIResourcePath}",
                this
            );
            return false;
        }

        transitionUI = Instantiate(transitionPrefab, transform);
        transitionUI.name = transitionPrefab.name;
        transitionUI.InitializeHidden();
        return true;
    }

    private async UniTaskVoid RunTransitionAsync(string sceneName)
    {
        CancellationToken cancellationToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await transitionUI.CoverAsync(cancellationToken);

            AsyncOperation loadOperation = UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                throw new InvalidOperationException($"Unity could not start loading scene: {sceneName}");
            }

            await loadOperation.ToUniTask(cancellationToken: cancellationToken);

            if (waitsForSceneReady)
            {
                await UniTask.WaitUntil(() => sceneReadyReceived, cancellationToken: cancellationToken);
            }

            // 새 씬의 Canvas와 레이아웃이 한 프레임 렌더링 준비를 마친 뒤 화면을 연다.
            await UniTask.NextFrame(cancellationToken: cancellationToken);
            await transitionUI.RevealAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 매니저가 파괴되는 애플리케이션 종료 과정에서는 복구 연출을 실행하지 않습니다.
        }
        catch (Exception exception)
        {
            DebugConsole.LogException(exception, this);

            if (transitionUI != null)
            {
                await transitionUI.RevealAsync(CancellationToken.None);
            }
        }
        finally
        {
            isTransitioning = false;
            waitsForSceneReady = false;
            sceneReadyReceived = false;
            targetSceneName = null;
        }
    }
}
