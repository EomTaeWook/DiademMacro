import AppKit
import ApplicationServices
import CoreGraphics
import Foundation
import ScreenCaptureKit
import WebKit

private func onMain<Result>(_ action: @MainActor () -> Result) -> Result {
    if Thread.isMainThread { return MainActor.assumeIsolated { action() } }
    return DispatchQueue.main.sync { MainActor.assumeIsolated { action() } }
}

private func windowInformation(_ window: CGWindowID) -> [String: Any]? {
    if window == kCGNullWindowID { return nil }
    let entries = CGWindowListCopyWindowInfo(.optionIncludingWindow, window) as? [[String: Any]]
    return entries?.first
}

private func windowBounds(_ entry: [String: Any]) -> CGRect? {
    guard let values = entry[kCGWindowBounds as String] as? NSDictionary else { return nil }
    return CGRect(dictionaryRepresentation: values as CFDictionary)
}

private func onScreenWindows() -> [[String: Any]] {
    let entries = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    return entries.filter { entry in
        guard let bounds = windowBounds(entry), bounds.width > 1, bounds.height > 1 else { return false }
        return (entry[kCGWindowAlpha as String] as? Double ?? 1) > 0
    }
}

private func visibleWindows() -> [[String: Any]] {
    return onScreenWindows().filter { ($0[kCGWindowLayer as String] as? Int) == 0 }
}

private func displayScale(at point: CGPoint) -> CGFloat {
    var display = CGDirectDisplayID(0)
    var count: UInt32 = 0
    if CGGetDisplaysWithPoint(point, 1, &display, &count) != .success { return 1 }
    if count == 0 { return 1 }
    let width = CGDisplayBounds(display).width
    if width <= 0 { return 1 }
    return CGFloat(CGDisplayPixelsWide(display)) / width
}

@_cdecl("dm_input_permission")
public func inputPermission(_ request: Int32) -> Int32 {
    let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: request != 0] as CFDictionary
    return AXIsProcessTrustedWithOptions(options) ? 1 : 0
}

@_cdecl("dm_capture_permission")
public func capturePermission(_ request: Int32) -> Int32 {
    if request != 0 { return CGRequestScreenCaptureAccess() ? 1 : 0 }
    return CGPreflightScreenCaptureAccess() ? 1 : 0
}

@_cdecl("dm_main_window")
public func mainWindow(_ process: Int32) -> UInt32 {
    let entry = visibleWindows().first { ($0[kCGWindowOwnerPID as String] as? Int32) == process }
    return entry?[kCGWindowNumber as String] as? UInt32 ?? 0
}

@_cdecl("dm_foreground_window")
public func foregroundWindow() -> UInt32 {
    return onMain {
        guard let process = NSWorkspace.shared.frontmostApplication?.processIdentifier else { return 0 }
        return mainWindow(process)
    }
}

@_cdecl("dm_window_bounds")
public func boundsForWindow(_ window: UInt32, _ left: UnsafeMutablePointer<Double>, _ top: UnsafeMutablePointer<Double>, _ right: UnsafeMutablePointer<Double>, _ bottom: UnsafeMutablePointer<Double>) -> Int32 {
    guard let entry = windowInformation(window), let bounds = windowBounds(entry) else { return 0 }
    left.pointee = bounds.minX
    top.pointee = bounds.minY
    right.pointee = bounds.maxX
    bottom.pointee = bounds.maxY
    return 1
}

@_cdecl("dm_window_visible")
public func windowVisible(_ window: UInt32) -> Int32 {
    guard let entry = windowInformation(window) else { return 0 }
    return (entry[kCGWindowIsOnscreen as String] as? Bool ?? false) ? 1 : 0
}

@_cdecl("dm_window_at_point")
public func windowAtPoint(_ x: Double, _ y: Double) -> UInt32 {
    for entry in onScreenWindows() {
        if let bounds = windowBounds(entry), bounds.contains(CGPoint(x: x, y: y)) {
            return entry[kCGWindowNumber as String] as? UInt32 ?? 0
        }
    }
    return 0
}

@_cdecl("dm_window_title")
public func titleForWindow(_ window: UInt32) -> UnsafeMutablePointer<CChar>? {
    guard let entry = windowInformation(window) else { return strdup("") }
    return strdup(entry[kCGWindowName as String] as? String ?? entry[kCGWindowOwnerName as String] as? String ?? "")
}

@_cdecl("dm_free")
public func freeMemory(_ pointer: UnsafeMutableRawPointer?) {
    free(pointer)
}

@_cdecl("dm_activate_window")
public func activateWindow(_ window: UInt32) -> Int32 {
    guard AXIsProcessTrusted(), let entry = windowInformation(window),
          let process = entry[kCGWindowOwnerPID as String] as? Int32 else { return 0 }
    let title = entry[kCGWindowName as String] as? String ?? ""
    return onMain {
        guard let application = NSRunningApplication(processIdentifier: process) else { return 0 }
        let activated = application.activate(options: [.activateIgnoringOtherApps])
        let accessibilityApplication = AXUIElementCreateApplication(process)
        var values: CFTypeRef?
        if AXUIElementCopyAttributeValue(accessibilityApplication, kAXWindowsAttribute as CFString, &values) == .success,
           let windows = values as? [AXUIElement] {
            for accessibilityWindow in windows {
                var candidateTitle: CFTypeRef?
                AXUIElementCopyAttributeValue(accessibilityWindow, kAXTitleAttribute as CFString, &candidateTitle)
                if !title.isEmpty, candidateTitle as? String == title {
                    AXUIElementPerformAction(accessibilityWindow, kAXRaiseAction as CFString)
                    break
                }
            }
        }
        return activated ? 1 : 0
    }
}

@_cdecl("dm_monitors")
public func monitors() -> UnsafeMutablePointer<CChar>? {
    return onMain {
        let descriptions: [[String: Any]] = NSScreen.screens.enumerated().compactMap { index, screen in
            guard let number = screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber else { return nil }
            let display = CGDirectDisplayID(number.uint32Value)
            let bounds = CGDisplayBounds(display)
            let dpi = Int((screen.backingScaleFactor * 96).rounded())
            return ["Index": index, "Rect": ["Left": Int(bounds.minX), "Top": Int(bounds.minY), "Right": Int(bounds.maxX), "Bottom": Int(bounds.maxY)], "Dpi": ["X": dpi, "Y": dpi], "DeviceName": String(display), "FriendlyName": screen.localizedName, "IsOn": true]
        }
        guard let data = try? JSONSerialization.data(withJSONObject: descriptions), let text = String(data: data, encoding: .utf8) else { return nil }
        return strdup(text)
    }
}

@_cdecl("dm_cursor")
public func cursorPosition(_ x: UnsafeMutablePointer<Double>, _ y: UnsafeMutablePointer<Double>) -> Int32 {
    guard let event = CGEvent(source: nil) else { return 0 }
    x.pointee = event.location.x
    y.pointee = event.location.y
    return 1
}

@_cdecl("dm_mouse")
public func mouseEvent(_ eventType: Int32, _ button: Int32, _ x: Double, _ y: Double, _ clickCount: Int64) -> Int32 {
    if eventType != 2, eventType != 4 {
        if !AXIsProcessTrusted() { return 0 }
    }
    guard let type = CGEventType(rawValue: UInt32(eventType)), let mouseButton = CGMouseButton(rawValue: UInt32(button)),
          let event = CGEvent(mouseEventSource: nil, mouseType: type, mouseCursorPosition: CGPoint(x: x, y: y), mouseButton: mouseButton) else { return 0 }
    switch type {
    case .mouseMoved, .leftMouseDragged, .rightMouseDragged:
        if CGWarpMouseCursorPosition(CGPoint(x: x, y: y)) != .success { return 0 }
    default:
        break
    }
    event.setIntegerValueField(.mouseEventClickState, value: clickCount)
    event.post(tap: .cghidEventTap)
    return 1
}

@_cdecl("dm_key")
public func keyEvent(_ key: UInt16, _ down: Int32, _ modifierFlags: UInt64) -> Int32 {
    if down != 0, !AXIsProcessTrusted() { return 0 }
    guard let event = CGEvent(keyboardEventSource: nil, virtualKey: CGKeyCode(key), keyDown: down != 0) else { return 0 }
    event.flags = CGEventFlags(rawValue: modifierFlags)
    event.post(tap: .cghidEventTap)
    return 1
}

public typealias CaptureCompletion = @convention(c) (UnsafeMutableRawPointer?, UnsafeRawPointer?, Int, UnsafePointer<CChar>?) -> Void

@_cdecl("dm_capture")
public func capture(_ isWindow: Int32, _ identifier: UInt32, _ completion: @escaping CaptureCompletion, _ context: UnsafeMutableRawPointer?) {
    Task {
        do {
            if !CGPreflightScreenCaptureAccess() { throw NSError(domain: "DiademMacro", code: 1, userInfo: [NSLocalizedDescriptionKey: "Allow screen recording for Diadem Macro in System Settings."]) }
            let content = try await SCShareableContent.excludingDesktopWindows(true, onScreenWindowsOnly: true)
            let filter: SCContentFilter
            let configuration = SCStreamConfiguration()
            configuration.showsCursor = false
            if isWindow != 0 {
                guard let window = content.windows.first(where: { $0.windowID == identifier }) else { throw NSError(domain: "DiademMacro", code: 2, userInfo: [NSLocalizedDescriptionKey: "The target window is unavailable or minimized."]) }
                filter = SCContentFilter(desktopIndependentWindow: window)
                let scale = displayScale(at: CGPoint(x: window.frame.midX, y: window.frame.midY))
                configuration.width = max(1, Int((window.frame.width * scale).rounded()))
                configuration.height = max(1, Int((window.frame.height * scale).rounded()))
                configuration.ignoreShadowsSingleWindow = true
            } else {
                guard let display = content.displays.first(where: { $0.displayID == identifier }) else { throw NSError(domain: "DiademMacro", code: 3, userInfo: [NSLocalizedDescriptionKey: "The target display is unavailable."]) }
                filter = SCContentFilter(display: display, excludingWindows: [])
                configuration.width = CGDisplayPixelsWide(identifier)
                configuration.height = CGDisplayPixelsHigh(identifier)
            }
            let image = try await SCScreenshotManager.captureImage(contentFilter: filter, configuration: configuration)
            let representation = NSBitmapImageRep(cgImage: image)
            guard let data = representation.representation(using: .png, properties: [:]) else { throw NSError(domain: "DiademMacro", code: 4, userInfo: [NSLocalizedDescriptionKey: "The captured image could not be encoded."]) }
            data.withUnsafeBytes { bytes in completion(context, bytes.baseAddress, data.count, nil) }
        } catch {
            error.localizedDescription.withCString { message in completion(context, nil, 0, message) }
        }
    }
}

@_cdecl("dm_webview_create")
public func createWebView(_ parentHandle: UnsafeMutableRawPointer) -> UnsafeMutableRawPointer {
    return onMain {
        let parent = Unmanaged<NSView>.fromOpaque(parentHandle).takeUnretainedValue()
        let webView = WKWebView(frame: parent.bounds)
        webView.autoresizingMask = [.width, .height]
        parent.addSubview(webView)
        return Unmanaged.passRetained(webView).toOpaque()
    }
}

@_cdecl("dm_webview_navigate")
public func navigateWebView(_ handle: UnsafeMutableRawPointer, _ address: UnsafePointer<CChar>) {
    let text = String(cString: address)
    onMain {
        guard let url = URL(string: text) else { return }
        let webView = Unmanaged<WKWebView>.fromOpaque(handle).takeUnretainedValue()
        webView.load(URLRequest(url: url))
    }
}

@_cdecl("dm_webview_destroy")
public func destroyWebView(_ handle: UnsafeMutableRawPointer) {
    onMain {
        let webView = Unmanaged<WKWebView>.fromOpaque(handle).takeRetainedValue()
        webView.stopLoading()
        webView.removeFromSuperview()
    }
}
