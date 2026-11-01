using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure.Controller;
using Macro.Infrastructure.Manager;
using Macro.Infrastructure.Services;
using Macro.Models;
using MCP.Core.Models.Server;
using MCP.Core.Models.Tools;
using MCP.Core.Tools;
using MCP.Core.Transport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.Infrastructure;
using DrawingRectangle = System.Drawing.Rectangle;
using ProcessInfo = Macro.Models.ProcessInfo;

namespace Macro.Infrastructure.Mcp
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class MacroMcpService
    {
        private readonly EventEditingService _events;
        private readonly ScreenCaptureManager _capture;
        private readonly BatchModeController _search;
        private readonly Config _config;
        private readonly IClassicDesktopStyleApplicationLifetime _desktop;
        private McpHttpServer _server;

        public MacroMcpService(EventEditingService eventEditingService, ScreenCaptureManager screenCaptureManager, BatchModeController imageSearchController, Config config, IClassicDesktopStyleApplicationLifetime desktop)
        {
            _events = eventEditingService;
            _capture = screenCaptureManager;
            _search = imageSearchController;
            _config = config;
            _desktop = desktop;
        }

        public async Task StartAsync()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (Array.IndexOf(arguments, "--mcp") < 0)
            {
                return;
            }
            int port = 8192;
            foreach (string argument in arguments)
            {
                if (argument.StartsWith("--mcp-port=", StringComparison.Ordinal) == false)
                {
                    continue;
                }
                if (int.TryParse(argument.Substring("--mcp-port=".Length), out port) == false)
                {
                    throw new ArgumentException("The MCP port must be an integer.");
                }
                if (port < 1024)
                {
                    throw new ArgumentException("The MCP port must be between 1024 and 65535.");
                }
                if (port > 65535)
                {
                    throw new ArgumentException("The MCP port must be between 1024 and 65535.");
                }
            }
            _server = new McpHttpServer(new McpServerOptions
            {
                Endpoint = new Uri("http://127.0.0.1:" + port + "/mcp"),
                Name = "Diadem Macro",
                Version = VersionNote.CurrentVersion.ToVersionString(),
                AccessToken = Environment.GetEnvironmentVariable("DIADEM_MCP_TOKEN") ?? string.Empty,
                Instructions = "Configure Diadem events using the tools. Read the event tree and target window before editing. Parent-child events form sequential AND conditions in the parent's process window; siblings are independent. A child's stored process name does not switch target windows. Use physical_click for Unity. Coordinates are capture-image pixels, not desktop screen coordinates. Edits are saved automatically; inspect saved=false errors. No tool starts macro execution or sends input. The user starts execution in the app."
            }, CreateTools(), exception => LogHelper.Error(exception));
            try
            {
                await _server.StartAsync();
                LogHelper.Info("Diadem MCP listening at " + _server.Endpoint.AbsoluteUri);
            }
            catch
            {
                await StopAsync();
                throw;
            }
        }

        public async Task StopAsync()
        {
            McpHttpServer server = _server;
            _server = null;
            if (server != null)
            {
                await server.DisposeAsync();
            }
        }

        private IReadOnlyList<McpTool> CreateTools()
        {
            return new McpTool[]
            {
                new(new McpToolDefinition("diadem_get_status", "Read macro execution state, event count and editing availability.") { ReadOnly = true, Idempotent = true, OpenWorld = false }, GetStatusAsync),
                new(new McpToolDefinition("diadem_list_processes", "List running windowed processes with their actual process_id, name and title. Does not select or activate a window.") { ReadOnly = true, Idempotent = true, OpenWorld = false }, ListProcessesAsync),
                new(new McpToolDefinition("diadem_list_events", "Read the complete event tree and current settings. Existing event_id is the app's event number. Parent checks are sequential AND; siblings are independent.") { ReadOnly = true, Idempotent = true, OpenWorld = false }, ListEventsAsync),
                new(new McpToolDefinition("diadem_get_event_image", "Read one registered template as a PNG image. Requires an idle app.", ObjectSchema(new JsonObject { ["event_id"] = IntegerSchema(1, long.MaxValue) }, "event_id")) { ReadOnly = true, Idempotent = true, OpenWorld = false }, GetEventImageAsync),
                new(new McpToolDefinition("diadem_capture_window", "Capture a target window as PNG without clicking, activating or moving it. Use the returned capture-pixel coordinates to choose an image region. Requires an idle app.", ProcessSchema()) { ReadOnly = true, OpenWorld = false }, CaptureWindowAsync),
                new(new McpToolDefinition("diadem_create_event", "Create and save an event for process_id. Supply either an existing local image_path or capture_region from diadem_capture_window. Optional parent_id nests under that event; 0 means root. settings use the existing app model. Success means actual save completion.", ObjectSchema(new JsonObject { ["process_id"] = IntegerSchema(1, int.MaxValue), ["image_path"] = StringSchema(), ["capture_region"] = RectangleSchema(), ["parent_id"] = IntegerSchema(0, long.MaxValue), ["settings"] = SettingsSchema() }, "process_id")) { OpenWorld = false }, CreateEventAsync),
                new(new McpToolDefinition("diadem_update_event", "Update and save the supplied settings of one event; omitted settings are preserved. Optional image_path replaces the template, process_id retargets the process. Does not change children or event numbers. Requires no open dialogs or running macro.", ObjectSchema(new JsonObject { ["event_id"] = IntegerSchema(1, long.MaxValue), ["settings"] = SettingsSchema(), ["image_path"] = StringSchema(), ["process_id"] = IntegerSchema(1, int.MaxValue) }, "event_id", "settings")) { Idempotent = true, OpenWorld = false }, UpdateEventAsync),
                new(new McpToolDefinition("diadem_delete_event", "Delete the specified event AND all its children from the configuration and save. Existing image files are not deleted. Requires an idle app.", ObjectSchema(new JsonObject { ["event_id"] = IntegerSchema(1, long.MaxValue) }, "event_id")) { Destructive = true, OpenWorld = false }, DeleteEventAsync),
                new(new McpToolDefinition("diadem_move_event", "Move and save an event under parent_id, keeping its children and ID. parent_id=0 moves to the root. position is the zero-based final sibling position; -1 appends. Self and descendant cycles are rejected.", ObjectSchema(new JsonObject { ["event_id"] = IntegerSchema(1, long.MaxValue), ["parent_id"] = IntegerSchema(0, long.MaxValue), ["position"] = IntegerSchema(-1, int.MaxValue) }, "event_id", "parent_id")) { OpenWorld = false }, MoveEventAsync),
                new(new McpToolDefinition("diadem_save_events", "Save the current shared UI configuration using the existing SavePath/save.dll and image directory. Reports actual save success.") { Idempotent = true, OpenWorld = false }, SaveEventsAsync),
                new(new McpToolDefinition("diadem_search_event", "Run one capture and image search using the event's existing ROI and similarity settings. Returns similarity and location only. Never clicks, types, runs child events or starts the macro.", ObjectSchema(new JsonObject { ["event_id"] = IntegerSchema(1, long.MaxValue), ["process_id"] = IntegerSchema(1, int.MaxValue) }, "event_id", "process_id")) { ReadOnly = true, OpenWorld = false }, SearchEventAsync)
            };
        }

        private MainWindow GetWindow()
        {
            if (_desktop.MainWindow is MainWindow window)
            {
                return window;
            }
            throw new InvalidOperationException("The main window is unavailable.");
        }

        private async Task<McpToolResult> OnUiAsync(Func<Task<McpToolResult>> action, bool requireIdle, CancellationToken cancellationToken)
        {
            TaskCompletionSource<McpToolResult> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (cancellationToken.IsCancellationRequested == true)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }
                bool accepted = UIManager.Instance.EnqueueButtonAction(async () =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        GetWindow().EnsureMcpAvailable(requireIdle);
                        completion.TrySetResult(await action());
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested == true)
                    {
                        completion.TrySetCanceled(cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        LogHelper.Error(exception);
                        completion.TrySetResult(new McpToolResult(exception.Message, true));
                    }
                });
                if (accepted == false)
                {
                    completion.TrySetResult(new McpToolResult("The application is closing and cannot accept MCP work.", true));
                }
            });
            return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        private Task<McpToolResult> GetStatusAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() =>
            {
                MainWindow window = GetWindow();
                bool canEdit = window.IsMacroRunning == false;
                if (UIManager.Instance.HasActivePopup == true)
                {
                    canEdit = false;
                }
                if (ApplicationManager.Instance.HasActiveInputOverlay == true)
                {
                    canEdit = false;
                }
                if (window.ApplicationUpdateInProgress == true)
                {
                    canEdit = false;
                }
                return Task.FromResult(new McpToolResult(new JsonObject { ["version"] = VersionNote.CurrentVersion.ToVersionString(), ["running"] = window.IsMacroRunning, ["can_edit"] = canEdit, ["root_event_count"] = _events.EventItems.Count, ["execution_tools_available"] = false }));
            }, false, cancellationToken);
        }

        private Task<McpToolResult> ListProcessesAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(async () =>
            {
                JsonArray processes = await Task.Run(() =>
                {
                    JsonArray items = new();
                    foreach (Process process in Process.GetProcesses())
                    {
                        using (process)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            try
                            {
                                if (process.MainWindowHandle == IntPtr.Zero)
                                {
                                    continue;
                                }
                                items.Add(new JsonObject { ["process_id"] = process.Id, ["name"] = process.ProcessName, ["title"] = process.MainWindowTitle });
                            }
                            catch (InvalidOperationException)
                            {
                            }
                            catch (System.ComponentModel.Win32Exception)
                            {
                            }
                        }
                    }
                    return items;
                }, cancellationToken);
                return new McpToolResult(new JsonObject { ["processes"] = processes });
            }, false, cancellationToken);
        }

        private Task<McpToolResult> ListEventsAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() =>
            {
                JsonArray events = new();
                foreach (EventInfoModel eventInfo in _events.EventItems)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    events.Add(DescribeEvent(eventInfo));
                }
                return Task.FromResult(new McpToolResult(new JsonObject { ["events"] = events }));
            }, false, cancellationToken);
        }

        private Task<McpToolResult> GetEventImageAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() =>
            {
                EventInfoModel eventInfo = _events.Find(arguments["event_id"].GetValue<ulong>());
                return Task.FromResult(ImageResult(eventInfo.Image, new JsonObject { ["event_id"] = eventInfo.ItemIndex }));
            }, true, cancellationToken);
        }

        private Task<McpToolResult> CaptureWindowAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(async () =>
            {
                using Process process = OpenProcess(arguments["process_id"].GetValue<int>());
                using Bitmap capture = await CaptureAsync(process, cancellationToken);
                return ImageResult(capture, new JsonObject { ["process_id"] = process.Id, ["coordinate_space"] = "capture_pixels" });
            }, true, cancellationToken);
        }

        private Task<McpToolResult> CreateEventAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(async () =>
            {
                ulong parentId = 0;
                if (arguments["parent_id"] != null)
                {
                    parentId = arguments["parent_id"].GetValue<ulong>();
                    if (parentId != 0)
                    {
                        _events.Find(parentId);
                    }
                }
                bool hasImagePath = arguments["image_path"] != null;
                bool hasCaptureRegion = arguments["capture_region"] != null;
                if (hasImagePath == hasCaptureRegion)
                {
                    throw new ArgumentException("Supply exactly one of image_path and capture_region.");
                }
                EventInfoModel eventInfo = new();
                bool inserted = false;
                try
                {
                    using Process process = OpenProcess(arguments["process_id"].GetValue<int>());
                    SetProcess(eventInfo, process);
                    if (hasImagePath == true)
                    {
                        using Bitmap image = new(Path.GetFullPath(arguments["image_path"].GetValue<string>()));
                        eventInfo.Image = new Bitmap(image);
                    }
                    else
                    {
                        using Bitmap capture = await CaptureAsync(process, cancellationToken);
                        DrawingRectangle region = ReadRectangle(arguments["capture_region"].AsObject());
                        if (region.Right > capture.Width)
                        {
                            throw new ArgumentException("The capture region exceeds the image width.");
                        }
                        if (region.Bottom > capture.Height)
                        {
                            throw new ArgumentException("The capture region exceeds the image height.");
                        }
                        eventInfo.Image = capture.Clone(region, PixelFormat.Format32bppArgb);
                    }
                    ApplySettings(eventInfo, arguments["settings"] as JsonObject);
                    cancellationToken.ThrowIfCancellationRequested();
                    _events.Add(eventInfo, parentId);
                    inserted = true;
                    return SaveResult(new JsonObject { ["event"] = DescribeEvent(eventInfo) });
                }
                finally
                {
                    if (inserted == false)
                    {
                        eventInfo.Dispose();
                    }
                }
            }, true, cancellationToken);
        }

        private Task<McpToolResult> UpdateEventAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() =>
            {
                EventInfoModel eventInfo = _events.Find(arguments["event_id"].GetValue<ulong>());
                using EventInfoModel replacement = new(eventInfo);
                if (arguments["image_path"] != null)
                {
                    using Bitmap image = new(Path.GetFullPath(arguments["image_path"].GetValue<string>()));
                    Bitmap previousImage = replacement.Image;
                    replacement.Image = new Bitmap(image);
                    previousImage?.Dispose();
                }
                if (arguments["process_id"] != null)
                {
                    using Process process = OpenProcess(arguments["process_id"].GetValue<int>());
                    SetProcess(replacement, process);
                }
                ApplySettings(replacement, arguments["settings"].AsObject());
                cancellationToken.ThrowIfCancellationRequested();
                _events.Update(eventInfo, replacement);
                return Task.FromResult(SaveResult(new JsonObject { ["event"] = DescribeEvent(eventInfo) }));
            }, true, cancellationToken);
        }

        private Task<McpToolResult> DeleteEventAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() =>
            {
                ulong eventId = arguments["event_id"].GetValue<ulong>();
                _events.Remove(_events.Find(eventId));
                return Task.FromResult(SaveResult(new JsonObject { ["deleted_event_id"] = eventId }));
            }, true, cancellationToken);
        }

        private Task<McpToolResult> MoveEventAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() =>
            {
                EventInfoModel eventInfo = _events.Find(arguments["event_id"].GetValue<ulong>());
                ulong parentId = arguments["parent_id"].GetValue<ulong>();
                int position = -1;
                if (arguments["position"] != null)
                {
                    position = arguments["position"].GetValue<int>();
                }
                _events.Move(eventInfo, parentId, position);
                return Task.FromResult(SaveResult(new JsonObject { ["event_id"] = eventInfo.ItemIndex, ["parent_id"] = parentId }));
            }, true, cancellationToken);
        }

        private Task<McpToolResult> SaveEventsAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(() => Task.FromResult(CreateSaveResult(new JsonObject())), true, cancellationToken);
        }

        private McpToolResult SaveResult(JsonObject result)
        {
            GetWindow().RefreshAfterEventChange();
            return CreateSaveResult(result);
        }

        private McpToolResult CreateSaveResult(JsonObject result)
        {
            bool saved = _events.Save();
            result["applied"] = true;
            result["saved"] = saved;
            if (saved == false)
            {
                result["error"] = "The in-memory configuration changed, but saving failed. Retry diadem_save_events; do not repeat creation or deletion.";
                return new McpToolResult(result.ToJsonString(), true);
            }
            return new McpToolResult(result);
        }

        private Task<McpToolResult> SearchEventAsync(JsonObject arguments, CancellationToken cancellationToken)
        {
            return OnUiAsync(async () =>
            {
                using EventInfoModel eventInfo = new(_events.Find(arguments["event_id"].GetValue<ulong>()));
                using Process process = OpenProcess(arguments["process_id"].GetValue<int>());
                using Bitmap capture = await CaptureAsync(process, cancellationToken);
                Tuple<int, Point2D> match = await Task.Run(() => _search.CalculateSimilarityAndLocation(eventInfo.Image, capture, eventInfo, cancellationToken), cancellationToken);
                int threshold = _config.Similarity;
                if (eventInfo.Similarity > 0)
                {
                    threshold = eventInfo.Similarity;
                }
                return new McpToolResult(new JsonObject { ["event_id"] = eventInfo.ItemIndex, ["similarity"] = match.Item1, ["threshold"] = threshold, ["found"] = match.Item1 >= threshold, ["location"] = PointNode(match.Item2), ["input_sent"] = false });
            }, true, cancellationToken);
        }

        private async Task<Bitmap> CaptureAsync(Process process, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                if (_capture.CaptureProcessWindow(process, cancellationToken, out Bitmap image) == false)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new InvalidOperationException("The target window could not be captured.");
                }
                return image;
            }, cancellationToken);
        }

        private static Process OpenProcess(int processId)
        {
            Process process = Process.GetProcessById(processId);
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    throw new ArgumentException("The target process has no main window.");
                }
                return process;
            }
            catch
            {
                process.Dispose();
                throw;
            }
        }

        private void SetProcess(EventInfoModel eventInfo, Process process)
        {
            IntRect bounds = new();
            if (NativeHelper.GetWindowRect(process.MainWindowHandle, ref bounds) == IntPtr.Zero)
            {
                throw new InvalidOperationException("The target window position is unavailable.");
            }
            eventInfo.ProcessInfo = new ProcessInfo { ProcessName = process.ProcessName, Position = bounds };
            foreach (MonitorInfo monitor in _capture.GetMonitorInfo())
            {
                if (monitor.Rect.Left >= bounds.Right)
                {
                    continue;
                }
                if (monitor.Rect.Right <= bounds.Left)
                {
                    continue;
                }
                if (monitor.Rect.Top >= bounds.Bottom)
                {
                    continue;
                }
                if (monitor.Rect.Bottom <= bounds.Top)
                {
                    continue;
                }
                eventInfo.MonitorInfo = monitor;
                break;
            }
        }

        private static McpToolResult ImageResult(Bitmap image, JsonObject metadata)
        {
            if (image == null)
            {
                throw new InvalidOperationException("The event has no registered image.");
            }
            using MemoryStream stream = new();
            image.Save(stream, ImageFormat.Png);
            metadata["width"] = image.Width;
            metadata["height"] = image.Height;
            metadata["mime_type"] = "image/png";
            return new McpToolResult(metadata, stream.ToArray());
        }

        private static JsonObject PointNode(Point2D point)
        {
            return new JsonObject { ["x"] = point.X, ["y"] = point.Y };
        }

        private JsonObject DescribeEvent(EventInfoModel eventInfo)
        {
            JsonArray children = new();
            foreach (EventInfoModel child in eventInfo.SubEventItems)
            {
                children.Add(DescribeEvent(child));
            }
            JsonArray mousePoints = new();
            foreach (Point2D point in eventInfo.MouseEventInfo.MousePoints)
            {
                mousePoints.Add(PointNode(point));
            }
            JsonObject region = null;
            if (eventInfo.RoiDataInfo.IsExists() == true)
            {
                IntRect rectangle = _capture.AdjustRectForDPI(eventInfo.RoiDataInfo.RoiRect, eventInfo.RoiDataInfo.MonitorInfo);
                region = new JsonObject { ["left"] = rectangle.Left, ["top"] = rectangle.Top, ["width"] = rectangle.Width, ["height"] = rectangle.Height };
            }
            return new JsonObject
            {
                ["event_id"] = eventInfo.ItemIndex,
                ["process_name"] = eventInfo.ProcessInfo.ProcessName,
                ["event_type"] = eventInfo.EventType.ToString(),
                ["enabled"] = eventInfo.IsChecked,
                ["has_image"] = eventInfo.Image != null,
                ["physical_click"] = eventInfo.HardClick,
                ["keyboard_command"] = eventInfo.KeyboardCmd,
                ["after_delay_ms"] = eventInfo.AfterDelay,
                ["similarity"] = eventInfo.Similarity,
                ["repeat_type"] = eventInfo.RepeatInfo.RepeatType.ToString(),
                ["repeat_count"] = eventInfo.RepeatInfo.Count,
                ["next_event_id"] = eventInfo.EventToNext,
                ["same_image_drag"] = eventInfo.SameImageDrag,
                ["max_drag_count"] = eventInfo.MaxDragCount,
                ["relative_position"] = PointNode(eventInfo.PositionRelativeToImage),
                ["mouse"] = new JsonObject { ["type"] = eventInfo.MouseEventInfo.MouseEventType.ToString(), ["point"] = PointNode(eventInfo.MouseEventInfo.MousePoint), ["points"] = mousePoints },
                ["roi"] = region,
                ["status"] = eventInfo.Status,
                ["children"] = children
            };
        }

        private void ApplySettings(EventInfoModel eventInfo, JsonObject settings)
        {
            if (settings == null)
            {
                settings = new JsonObject();
            }
            if (settings["event_type"] != null)
            {
                eventInfo.EventType = Enum.Parse<EventType>(settings["event_type"].GetValue<string>());
            }
            if (settings["enabled"] != null)
            {
                eventInfo.IsChecked = settings["enabled"].GetValue<bool>();
            }
            if (settings["physical_click"] != null)
            {
                eventInfo.HardClick = settings["physical_click"].GetValue<bool>();
            }
            if (settings["keyboard_command"] != null)
            {
                eventInfo.KeyboardCmd = settings["keyboard_command"].GetValue<string>();
            }
            if (settings["after_delay_ms"] != null)
            {
                eventInfo.AfterDelay = settings["after_delay_ms"].GetValue<int>();
            }
            if (settings["similarity"] != null)
            {
                eventInfo.Similarity = settings["similarity"].GetValue<int>();
            }
            if (settings["repeat_type"] != null)
            {
                eventInfo.RepeatInfo.RepeatType = Enum.Parse<RepeatType>(settings["repeat_type"].GetValue<string>());
            }
            if (settings["repeat_count"] != null)
            {
                eventInfo.RepeatInfo.Count = settings["repeat_count"].GetValue<ushort>();
            }
            if (settings["next_event_id"] != null)
            {
                ulong nextEventId = settings["next_event_id"].GetValue<ulong>();
                if (nextEventId != 0)
                {
                    _events.Find(nextEventId);
                }
                eventInfo.EventToNext = nextEventId;
            }
            if (settings["same_image_drag"] != null)
            {
                eventInfo.SameImageDrag = settings["same_image_drag"].GetValue<bool>();
            }
            if (settings["max_drag_count"] != null)
            {
                eventInfo.MaxDragCount = settings["max_drag_count"].GetValue<int>();
            }
            if (settings["relative_position"] is JsonObject relativePosition)
            {
                eventInfo.PositionRelativeToImage = ReadPoint(relativePosition);
            }
            if (settings["mouse"] is JsonObject mouse)
            {
                if (mouse["type"] != null)
                {
                    eventInfo.MouseEventInfo.MouseEventType = Enum.Parse<MouseEventType>(mouse["type"].GetValue<string>());
                }
                if (mouse["point"] is JsonObject mousePoint)
                {
                    eventInfo.MouseEventInfo.MousePoint = ReadPoint(mousePoint);
                }
                if (mouse["points"] is JsonArray mousePoints)
                {
                    List<Point2D> points = new();
                    foreach (JsonNode point in mousePoints)
                    {
                        points.Add(ReadPoint(point.AsObject()));
                    }
                    eventInfo.MouseEventInfo.MousePoints = points;
                }
            }
            if (settings["clear_roi"] != null)
            {
                if (settings["clear_roi"].GetValue<bool>() == true)
                {
                    if (settings["roi"] != null)
                    {
                        throw new ArgumentException("Supply roi or clear_roi=true, not both.");
                    }
                    eventInfo.RoiDataInfo = new RoiModel();
                }
            }
            if (settings["roi"] is JsonObject roi)
            {
                DrawingRectangle rectangle = ReadRectangle(roi);
                eventInfo.RoiDataInfo = new RoiModel
                {
                    RoiRect = new Utils.Infrastructure.Rectangle { Left = rectangle.Left, Top = rectangle.Top, Right = rectangle.Right, Bottom = rectangle.Bottom },
                    MonitorInfo = new MonitorInfo { Dpi = NativeHelper.GetSystemDPI() }
                };
            }
            if (eventInfo.Image == null)
            {
                throw new ArgumentException("Every event requires a template image, including keyboard and mouse events.");
            }
            if (eventInfo.HardClick == true)
            {
                if (eventInfo.SameImageDrag == true)
                {
                    throw new ArgumentException("Physical click cannot be combined with same-image dragging.");
                }
                if (eventInfo.EventType == EventType.Keyboard)
                {
                    throw new ArgumentException("Physical click is not used by keyboard events.");
                }
            }
            if (eventInfo.EventType == EventType.Keyboard)
            {
                if (string.IsNullOrWhiteSpace(eventInfo.KeyboardCmd) == true)
                {
                    throw new ArgumentException("A keyboard event requires keyboard_command.");
                }
            }
            if (eventInfo.SameImageDrag == true)
            {
                if (eventInfo.EventType != EventType.Image)
                {
                    throw new ArgumentException("Same-image dragging requires an Image event.");
                }
                if (eventInfo.MaxDragCount < 1)
                {
                    throw new ArgumentException("Same-image dragging requires max_drag_count greater than zero.");
                }
            }
        }

        private static Point2D ReadPoint(JsonObject point)
        {
            return new Point2D(point["x"].GetValue<int>(), point["y"].GetValue<int>());
        }

        private static JsonObject PointSchema()
        {
            return ObjectSchema(new JsonObject { ["x"] = IntegerSchema(int.MinValue, int.MaxValue), ["y"] = IntegerSchema(int.MinValue, int.MaxValue) }, "x", "y");
        }

        private static JsonObject SettingsSchema()
        {
            return ObjectSchema(new JsonObject
            {
                ["event_type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("Image", "Mouse", "Keyboard", "RelativeToImage") },
                ["enabled"] = new JsonObject { ["type"] = "boolean" },
                ["physical_click"] = new JsonObject { ["type"] = "boolean" },
                ["keyboard_command"] = new JsonObject { ["type"] = "string", ["maxLength"] = 4096 },
                ["after_delay_ms"] = IntegerSchema(0, int.MaxValue),
                ["similarity"] = IntegerSchema(0, 100),
                ["repeat_type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("Count", "RepeatOnChildEvent", "StopOnParentImage") },
                ["repeat_count"] = IntegerSchema(0, ushort.MaxValue),
                ["next_event_id"] = IntegerSchema(0, long.MaxValue),
                ["same_image_drag"] = new JsonObject { ["type"] = "boolean" },
                ["max_drag_count"] = IntegerSchema(0, int.MaxValue),
                ["relative_position"] = PointSchema(),
                ["mouse"] = ObjectSchema(new JsonObject { ["type"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("None", "LeftClick", "RightClick", "Drag", "Wheel") }, ["point"] = PointSchema(), ["points"] = new JsonObject { ["type"] = "array", ["items"] = PointSchema(), ["maxItems"] = 10000 } }),
                ["roi"] = RectangleSchema(),
                ["clear_roi"] = new JsonObject { ["type"] = "boolean" }
            });
        }

        private static JsonObject ObjectSchema(JsonObject properties, params string[] requiredNames)
        {
            JsonArray required = new();
            foreach (string name in requiredNames)
            {
                required.Add(name);
            }
            return new JsonObject { ["type"] = "object", ["properties"] = properties, ["required"] = required, ["additionalProperties"] = false };
        }

        private static JsonObject IntegerSchema(long minimum, long maximum)
        {
            return new JsonObject { ["type"] = "integer", ["minimum"] = minimum, ["maximum"] = maximum };
        }

        private static JsonObject StringSchema()
        {
            return new JsonObject { ["type"] = "string", ["minLength"] = 1 };
        }

        private static JsonObject ProcessSchema()
        {
            return ObjectSchema(new JsonObject { ["process_id"] = IntegerSchema(1, int.MaxValue) }, "process_id");
        }

        private static JsonObject RectangleSchema()
        {
            return ObjectSchema(new JsonObject { ["left"] = IntegerSchema(0, int.MaxValue), ["top"] = IntegerSchema(0, int.MaxValue), ["width"] = IntegerSchema(1, int.MaxValue), ["height"] = IntegerSchema(1, int.MaxValue) }, "left", "top", "width", "height");
        }

        private static DrawingRectangle ReadRectangle(JsonObject rectangle)
        {
            int left = rectangle["left"].GetValue<int>();
            int top = rectangle["top"].GetValue<int>();
            int width = rectangle["width"].GetValue<int>();
            int height = rectangle["height"].GetValue<int>();
            if ((long)left + width > int.MaxValue)
            {
                throw new ArgumentException("The rectangle exceeds the supported horizontal coordinates.");
            }
            if ((long)top + height > int.MaxValue)
            {
                throw new ArgumentException("The rectangle exceeds the supported vertical coordinates.");
            }
            return new DrawingRectangle(left, top, width, height);
        }
    }
}
