using System.Runtime.InteropServices;
using System.Windows;
using Reticle.Engine;
using static Reticle.Platform.NativeMethods;

namespace Reticle.Platform;

/// <summary>Reads the physical cursor position and injects mouse input with <c>SendInput</c>.</summary>
internal sealed class Win32Mouse : IMouse
{
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    public Point GetCursorPosition()
    {
        GetPhysicalCursorPos(out POINT p);
        return new Point(p.X, p.Y);
    }

    public void Click(MouseButton button) => Send(Down(button), Up(button));

    public void DoubleClick(MouseButton button) => Send(Down(button), Up(button), Down(button), Up(button));

    public void Press(MouseButton button) => Send(Down(button));

    public void Release(MouseButton button) => Send(Up(button));

    /// <summary>Injected input is subject to button swapping, so the logical primary is the physical right when swapped.</summary>
    private static bool IsPhysicalLeft(MouseButton button) =>
        (button == MouseButton.Left) != (GetSystemMetrics(SM_SWAPBUTTON) != 0);

    private static uint Down(MouseButton button) => IsPhysicalLeft(button) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN;

    private static uint Up(MouseButton button) => IsPhysicalLeft(button) ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_RIGHTUP;

    private static void Send(params uint[] flags)
    {
        var inputs = new INPUT[flags.Length];
        for (int i = 0; i < flags.Length; i++)
        {
            inputs[i] = new INPUT { type = INPUT_MOUSE, mi = new MOUSEINPUT { dwFlags = flags[i] } };
        }
        if (SendInput((uint)inputs.Length, inputs, InputSize) != inputs.Length)
        {
            Log.Warn($"SendInput injected fewer events than requested (error {Marshal.GetLastPInvokeError()}).");
        }
    }
}
