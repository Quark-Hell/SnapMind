using SnapMind.Shared;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace SnapMind.Screenshoter;

internal class Program
{
    private static readonly string _baseAddress = "http://localhost:5132";
    private static System.Drawing.Rectangle _region = new Rectangle(x: 100, y: 100, width: 800, height: 600);
    private static readonly string _aiPostRequestAddress = "/api/ai/generate";

    static async Task PreloadModel()
    {
        using var http = new HttpClient
        {
            BaseAddress = new Uri(_baseAddress),
            Timeout = TimeSpan.FromMinutes(5)
        };

        var request = new
        {
            model = "qwen3.5:4b",
            prompt = "Привет! Готов к работе?"
        };

        HttpResponseMessage? response = null;

        int retry_count = 5;
        for (int i = 0; i < retry_count; i++)
        {
            try
            {
                response = await http.PostAsJsonAsync(_aiPostRequestAddress, request);

                if (response.IsSuccessStatusCode)
                    break;

                Console.WriteLine($"AIService not available. Retry count {i}");
            }
            catch
            {

            }

            await Task.Delay(5000);
        }

        if (response == null || !response.IsSuccessStatusCode)
            throw new Exception("AIService not available");

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
    }

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Загрузка AI модели. Пожалуйста ожидайте...");
        await PreloadModel();

        using var http = new HttpClient
        {
            BaseAddress = new Uri(_baseAddress)
        };

        using var hook = new KeyboardHook(
            onScreenshot: () =>
            {
                Console.WriteLine("Select region!");
                _region = RegionSelectorForm.SelectRegion();

                Console.WriteLine("Snapshot!");
                Console.WriteLine("Отправляем запрос AI модели. Пожалуйста ожидайте...");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        var base64 = ScreenCapture.CaptureToFileAndBase64(_region, "screen.png");

                        var request = new
                        {
                            model = "qwen3.5:4b",
                            prompt = "Что на изображении? Ответь кратко. Если ты видишь тест с вопросами на изображении, то укажи правильные варианты",
                            imageBase64 = base64
                        };

                        var response = await http.PostAsJsonAsync(_aiPostRequestAddress, request);

                        response.EnsureSuccessStatusCode();

                        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>();

                        Console.WriteLine("AI: " + result?.Response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("AI request failed: " + ex.Message);
                    }
                });
            }
        );


        hook.Start();
        Console.WriteLine("Hook started. Press ESC to exit.");

        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG msg, IntPtr hWnd, uint min, uint max);
    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG msg);
}

// ──────────────────────── Hook ───────────────────────────────────

public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int VK_SNAPSHOT = (int)ConsoleKey.F12;
    private const int VK_ESCAPE = (int)ConsoleKey.F1;

    private readonly Action _onTriggerScreenshot;


    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _disposed;

    public KeyboardHook(Action onScreenshot)
    {
        _onTriggerScreenshot = onScreenshot;
        _proc = HookCallback;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var process = Process.GetCurrentProcess();
        var module = process.MainModule
            ?? throw new InvalidOperationException("Cannot get main module.");

        _hookId = SetWindowsHookEx(
            WH_KEYBOARD_LL, _proc,
            GetModuleHandle(module.ModuleName!), 0);

        if (_hookId == IntPtr.Zero)
            throw new InvalidOperationException(
                $"SetWindowsHookEx failed: {Marshal.GetLastWin32Error()}");
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vk = Marshal.ReadInt32(lParam);

            if (vk == VK_SNAPSHOT)
                _onTriggerScreenshot?.Invoke();

            if (vk == VK_ESCAPE)
                PostQuitMessage(0);
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        _disposed = true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);
}

[StructLayout(LayoutKind.Sequential)]
public struct MSG
{
    public IntPtr hWnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public POINT pt;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT { public int X, Y; }
