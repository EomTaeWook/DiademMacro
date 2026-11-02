using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Utils.Infrastructure;

namespace Utils.Platforms.MacOS
{
    public class MacOSNative
    {
        private const string Library = "DiademMac";
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CaptureCompletion(IntPtr context, IntPtr data, nint length, IntPtr error);
        private static readonly CaptureCompletion _captureCompletion = CaptureCompleted;

        [DllImport(Library, EntryPoint = "dm_input_permission", CallingConvention = CallingConvention.Cdecl)]
        public static extern int InputPermission(int request);
        [DllImport(Library, EntryPoint = "dm_capture_permission", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CapturePermission(int request);
        [DllImport(Library, EntryPoint = "dm_main_window", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint MainWindow(int processId);
        [DllImport(Library, EntryPoint = "dm_foreground_window", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ForegroundWindow();
        [DllImport(Library, EntryPoint = "dm_window_bounds", CallingConvention = CallingConvention.Cdecl)]
        private static extern int WindowBounds(uint windowId, out double left, out double top, out double right, out double bottom);
        [DllImport(Library, EntryPoint = "dm_window_visible", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WindowVisible(uint windowId);
        [DllImport(Library, EntryPoint = "dm_window_at_point", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint WindowAtPoint(double x, double y);
        [DllImport(Library, EntryPoint = "dm_window_title", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr WindowTitle(uint windowId);
        [DllImport(Library, EntryPoint = "dm_free", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Free(IntPtr data);
        [DllImport(Library, EntryPoint = "dm_activate_window", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ActivateWindow(uint windowId);
        [DllImport(Library, EntryPoint = "dm_monitors", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Monitors();
        [DllImport(Library, EntryPoint = "dm_cursor", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Cursor(out double x, out double y);
        [DllImport(Library, EntryPoint = "dm_mouse", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Mouse(int eventType, int button, double x, double y, long clickCount);
        [DllImport(Library, EntryPoint = "dm_key", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Key(ushort keyCode, int down, ulong modifierFlags);
        [DllImport(Library, EntryPoint = "dm_capture", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Capture(int isWindow, uint identifier, CaptureCompletion completion, IntPtr context);
        [DllImport(Library, EntryPoint = "dm_webview_create", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateWebView(IntPtr parent);
        [DllImport(Library, EntryPoint = "dm_webview_navigate", CallingConvention = CallingConvention.Cdecl)]
        public static extern void NavigateWebView(IntPtr view, [MarshalAs(UnmanagedType.LPUTF8Str)] string address);
        [DllImport(Library, EntryPoint = "dm_webview_destroy", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyWebView(IntPtr view);

        public static bool TryGetWindowBounds(IntPtr window, out IntRect bounds)
        {
            bounds = new IntRect();
            if (WindowBounds((uint)window.ToInt64(), out double left, out double top, out double right, out double bottom) == 0)
            {
                return false;
            }
            bounds = new IntRect { Left = (int)Math.Floor(left), Top = (int)Math.Floor(top), Right = (int)Math.Ceiling(right), Bottom = (int)Math.Ceiling(bottom) };
            return true;
        }

        public static string GetWindowTitle(uint window)
        {
            return ReadString(WindowTitle(window));
        }

        public static List<MonitorInfo> GetMonitors()
        {
            return JsonSerializer.Deserialize<List<MonitorInfo>>(ReadString(Monitors())) ?? new List<MonitorInfo>();
        }

        private static string ReadString(IntPtr value)
        {
            if (value == IntPtr.Zero)
            {
                throw new InvalidOperationException("The macOS native operation returned no result.");
            }
            try
            {
                return Marshal.PtrToStringUTF8(value);
            }
            finally
            {
                Free(value);
            }
        }

        public static Point GetCursorPosition()
        {
            if (Cursor(out double x, out double y) == 0)
            {
                throw new InvalidOperationException("The cursor position is unavailable.");
            }
            return new Point((int)Math.Round(x), (int)Math.Round(y));
        }

        public static Task<byte[]> CaptureAsync(bool isWindow, uint identifier, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource<byte[]> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            GCHandle lifetime = GCHandle.Alloc(completion);
            try
            {
                int windowFlag = 0;
                if (isWindow == true)
                {
                    windowFlag = 1;
                }
                Capture(windowFlag, identifier, _captureCompletion, GCHandle.ToIntPtr(lifetime));
            }
            catch
            {
                lifetime.Free();
                throw;
            }
            return AwaitCaptureAsync(completion, cancellationToken);
        }

        private static async Task<byte[]> AwaitCaptureAsync(TaskCompletionSource<byte[]> completion, CancellationToken cancellationToken)
        {
            try
            {
                return await completion.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                completion.TrySetCanceled();
                throw;
            }
        }

        private static void CaptureCompleted(IntPtr context, IntPtr data, nint length, IntPtr error)
        {
            GCHandle lifetime = GCHandle.FromIntPtr(context);
            TaskCompletionSource<byte[]> completion = (TaskCompletionSource<byte[]>)lifetime.Target;
            try
            {
                if (completion.Task.IsCompleted == true)
                {
                    return;
                }
                if (error != IntPtr.Zero)
                {
                    completion.TrySetException(new InvalidOperationException(Marshal.PtrToStringUTF8(error)));
                    return;
                }
                if (data == IntPtr.Zero)
                {
                    throw new InvalidOperationException("The screen capture returned no image.");
                }
                if (length <= 0)
                {
                    throw new InvalidOperationException("The screen capture returned an empty image.");
                }
                if (length > Array.MaxLength)
                {
                    throw new InvalidOperationException("The captured image exceeds the supported size.");
                }
                byte[] bytes = new byte[(int)length];
                Marshal.Copy(data, bytes, 0, bytes.Length);
                completion.TrySetResult(bytes);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            finally
            {
                lifetime.Free();
            }
        }
    }
}
