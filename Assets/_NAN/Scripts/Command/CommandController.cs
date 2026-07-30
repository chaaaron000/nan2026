using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 플레이 커맨드를 실행하고 되돌리기 이력을 관리한다.
/// </summary>
public sealed class CommandController : MonoBehaviour
{
    // 실행되는 커맨드가 쌓일 스택
    private readonly Stack<ICommand> undoStack = new();

    private bool inputEnabled = true;

    /// <summary>
    /// 현재 되돌릴 수 있는 커맨드가 있는지 나타내는 프로퍼티
    /// </summary>
    public bool CanUndo => undoStack.Count > 0;

    /// <summary>새 커맨드 실행과 Undo/Clear UI 입력의 허용 여부를 설정한다.</summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    /// <summary>
    /// 커맨드를 실행하고 성공한 커맨드만 이력에 기록한다.
    /// </summary>
    public bool Execute(ICommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (!inputEnabled)
        {
            return false;
        }

        // 커맨드 실행 및 실패시 false 반환하고 return
        if (!command.Execute())
        {
            return false;
        }

        undoStack.Push(command);
        return true;
    }
    
    /// <summary>
    /// UI 버튼 입력을 받아 가장 최근 커맨드를 되돌린다.
    /// </summary>
    public void HandleUndoButtonClicked()
    {
        if (!inputEnabled || !CanUndo)
        {
            return;
        }

        // 복원 대상이 있다는 것이 확인되었으므로
        // 대량의 화면 복원 작업보다 먼저 즉시 피드백을 제공한다.
        SoundManager.Instance?.PlaySfx(
            SoundKeys.Undo);
        UndoLast();
    }

    /// <summary>
    /// 가장 최근에 성공한 커맨드를 되돌린다.
    /// </summary>
    private bool UndoLast()
    {
        if (!inputEnabled || undoStack.Count == 0)
        {
            return false;
        }

        ICommand command = undoStack.Pop();
        command.Undo();

        return true;
    }
    
    /// <summary>
    /// UI 버튼 입력을 받아 커맨드를 모두 되돌린다.
    /// </summary>
    public void HandleClearButtonClicked()
    {
        if (!inputEnabled || undoStack.Count == 0)
        {
            return;
        }

        // 전체 되돌리기 대상이 있다는 것이 확인되었으므로
        // 반복되는 Undo 처리보다 먼저 버튼 피드백을 재생한다.
        SoundManager.Instance?.PlaySfx(
            SoundKeys.Erase);
        UndoAll();
    }

    /// <summary>
    /// 실행된 모든 커맨드를 최근 순서부터 되돌린다.
    /// </summary>
    private void UndoAll()
    {
        while (undoStack.Count > 0)
        {
            undoStack.Pop().Undo();
        }
    }

    /// <summary>
    /// 현재 커맨드 이력만 제거한다.
    /// 게임 상태는 변경하지 않는다.
    /// </summary>
    public void ClearHistory()
    {
        undoStack.Clear();
    }
}
