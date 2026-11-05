using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Делает нативный title bar окна Windows тёмным (DWMWA_USE_IMMERSIVE_DARK_MODE).
/// Только в билде: в редакторе выходим, чтобы не перекрашивать окно Unity.
/// Применяем в первые кадры — окно к Start уже существует, но повторяем на случай
/// пересоздания при смене разрешения/фуллскрина.
/// </summary>
public class DarkTitleBar : MonoBehaviour
{
#if UNITY_STANDALONE_WIN
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private const int USE_IMMERSIVE_DARK_MODE = 20;   // Win10 20H1+ / Win11
    private const int USE_IMMERSIVE_DARK_MODE_OLD = 19; // ранние Win10 (fallback)

    void Start() => StartCoroutine(ApplySoon());

    private IEnumerator ApplySoon()
    {
        for (int i = 0; i < 10; i++)   // ~10 кадров страховки
        {
            Apply();
            yield return null;
        }
    }

    private void Apply()
    {
        IntPtr hwnd = GetActiveWindow();
        if (hwnd == IntPtr.Zero) return;
        int on = 1;
        // 20 -> если среда не знает атрибут (вернёт не-0), пробуем 19.
        if (DwmSetWindowAttribute(hwnd, USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, USE_IMMERSIVE_DARK_MODE_OLD, ref on, sizeof(int));
    }
#else
    void Start() { /* не-Windows билд: title bar не трогаем */ }
#endif
}