using System;

/// <summary>
/// 세 자리 플래그 각각이 RGB를 의미, ex) 110 -> RGx -> Yellow
/// </summary>
[Flags]
public enum PaintState
{
    Empty = 0,
    Red   = 1 << 2,
    Green = 1 << 1,
    Blue  = 1 << 0,

    Yellow  = Red | Green,
    Cyan    = Green | Blue,
    Magenta = Red | Blue,
    White   = Red | Green | Blue
}