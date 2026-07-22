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

    /// <summary>
    /// 현재 되돌릴 수 있는 커맨드가 있는지 나타내는 프로퍼티
    /// </summary>
    public bool CanUndo => undoStack.Count > 0;

    /// <summary>
    /// 커맨드를 실행하고 성공한 커맨드만 이력에 기록한다.
    /// </summary>
    public bool Execute(ICommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
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
        UndoLast();
    }

    /// <summary>
    /// 가장 최근에 성공한 커맨드를 되돌린다.
    /// </summary>
    private bool UndoLast()
    {
        if (undoStack.Count == 0)
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