import AppKit
import ApplicationServices

func rectText(_ rect: CGRect?) -> String { rect.map { "x=\($0.minX) y=\($0.minY) width=\($0.width) height=\($0.height)" } ?? "unavailable" }

// macOS may inset browser frames slightly to keep their titlebar accessible.
// Accept only small inward adjustments; never accept a minimum size spilling
// into another zone, or a different window occupying a small part of the zone.
func fitsZone(_ actual: CGRect, _ target: CGRect) -> Bool {
    guard actual.width > 0, actual.height > 0 else { return false }
    return actual.minX >= target.minX-4 && actual.minY >= target.minY-4
        && actual.maxX <= target.maxX+4 && actual.maxY <= target.maxY+4
        && actual.width >= target.width-24 && actual.height >= target.height-24
}

// Original Camadas implementation. Deskflow runs as an independent executable.
struct Cell: Codable {
    var id: String
    var name: String
    var display: UInt32
    var x: Double; var y: Double; var w: Double; var h: Double
    func rect(_ area: CGRect) -> CGRect {
        CGRect(x: area.minX + x * area.width, y: area.maxY - (y + h) * area.height,
               width: w * area.width, height: h * area.height).insetBy(dx: 3, dy: 3)
    }
}
struct SavedWindow: Codable { var pid: Int32; var bundle: String; var title: String }
struct Profile: Codable { var version = 1; var cells: [Cell]; var assignments: [String: SavedWindow] = [:] }
struct WindowRef {
    let element: AXUIElement
    let app: NSRunningApplication
    let title: String
    var saved: SavedWindow { SavedWindow(pid: app.processIdentifier, bundle: app.bundleIdentifier ?? "", title: title) }
}
func attribute(_ element: AXUIElement, _ key: String) -> CFTypeRef? {
    var value: CFTypeRef?; AXUIElementCopyAttributeValue(element, key as CFString, &value); return value
}
func windowRect(_ element: AXUIElement) -> CGRect? {
    guard let p = attribute(element, kAXPositionAttribute), let s = attribute(element, kAXSizeAttribute),
          CFGetTypeID(p) == AXValueGetTypeID(), CFGetTypeID(s) == AXValueGetTypeID() else { return nil }
    var point = CGPoint.zero; var size = CGSize.zero
    guard AXValueGetValue(p as! AXValue, .cgPoint, &point), AXValueGetValue(s as! AXValue, .cgSize, &size) else { return nil }
    return CGRect(origin: point, size: size)
}
// Chromium can acknowledge AXSize without changing the window. Its scripting
// interface supplies a separate, verifiable route; identify by bounds, never by
// the front window (which can change while the zone menu is open).
func browserBoundsFallback(_ element: AXUIElement, _ target: CGRect, errors: inout [String:Int32]) -> Bool {
    var pid:pid_t=0
    guard AXUIElementGetPid(element,&pid) == .success,
          let bundle=NSRunningApplication(processIdentifier:pid)?.bundleIdentifier,
          ["com.google.Chrome","com.brave.Browser","com.microsoft.edgemac"].contains(bundle),
          let old=windowRect(element) else{return false}
    let axTitle=(attribute(element,kAXTitleAttribute) as? String ?? "").replacingOccurrences(of:"\\",with:"\\\\").replacingOccurrences(of:"\"",with:"\\\"").replacingOccurrences(of:"\n",with:" ").replacingOccurrences(of:"\r",with:" ")
    let values=[old.minX,old.minY,old.maxX,old.maxY,target.minX,target.minY,target.maxX,target.maxY].map{String(Int($0.rounded()))}
    let source="""
    on near(a, b)
      return a > (b - 6) and a < (b + 6)
    end near
    with timeout of 3 seconds
      tell application id "\(bundle)"
        set candidates to {}
        set titledCandidates to {}
        repeat with candidate in windows
          set b to bounds of candidate
          set candidateTitle to title of candidate
          if candidateTitle is not "" and "\(axTitle)" contains candidateTitle then set end of titledCandidates to candidate
          if (my near((item 1 of b), \(values[0]))) and (my near((item 2 of b), \(values[1]))) and (my near((item 3 of b), \(values[2]))) and (my near((item 4 of b), \(values[3]))) then set end of candidates to candidate
        end repeat
        if (count titledCandidates) is 1 then set candidates to titledCandidates
        if (count candidates) is not 1 then return false
        set chosen to item 1 of candidates
        set bounds of chosen to {\(values[4]),\(values[5]),\(values[6]),\(values[7])}
        set b to bounds of chosen
        return (my near((item 1 of b), \(values[4]))) and (my near((item 2 of b), \(values[5]))) and (my near((item 3 of b), \(values[6]))) and (my near((item 4 of b), \(values[7])))
      end tell
    end timeout
    """
    var failure:NSDictionary?
    let result=NSAppleScript(source:source)?.executeAndReturnError(&failure)
    errors["browserAutomation"]=Int32((failure?[NSAppleScript.errorNumber] as? NSNumber)?.intValue ?? 0)
    errors["browserBoundsVerified"]=(result?.booleanValue == true) ? 1:0
    return result?.booleanValue == true
}
@discardableResult func move(_ element: AXUIElement, _ rect: CGRect, errors: inout [String: Int32]) -> Bool {
    var p = rect.origin; var s = rect.size
    let size = AXValueCreate(.cgSize, &s)!; let point = AXValueCreate(.cgPoint, &p)!
    let a = AXUIElementSetAttributeValue(element, kAXSizeAttribute as CFString, size)
    let b = AXUIElementSetAttributeValue(element, kAXPositionAttribute as CFString, point)
    // Some apps recalculate their limits after moving between displays.
    let retry = AXUIElementSetAttributeValue(element, kAXSizeAttribute as CFString, size)
    errors = ["size": a.rawValue, "position": b.rawValue, "sizeRetry": retry.rawValue]
    // Chromium may apply the second size update using a cached old origin.
    // Put position last, and allow its separate process a bounded settling interval.
    var positionResult=b
    for attempt in 0..<4 {
        positionResult=AXUIElementSetAttributeValue(element,kAXPositionAttribute as CFString,point)
        errors["positionRetry\(attempt)"]=positionResult.rawValue
        Thread.sleep(forTimeInterval:0.025)
        let settledSize=AXUIElementSetAttributeValue(element,kAXSizeAttribute as CFString,size)
        errors["settledSize\(attempt)"]=settledSize.rawValue
        positionResult=AXUIElementSetAttributeValue(element,kAXPositionAttribute as CFString,point)
        Thread.sleep(forTimeInterval:0.025)
        if let actual=windowRect(element), abs(actual.minX-rect.minX)<6 && abs(actual.minY-rect.minY)<6 && abs(actual.width-rect.width)<6 && abs(actual.height-rect.height)<6 {break}
    }
    if let actual=windowRect(element), abs(actual.minX-rect.minX)<6 && abs(actual.minY-rect.minY)<6 && abs(actual.width-rect.width)<6 && abs(actual.height-rect.height)<6 {return true}
    if browserBoundsFallback(element,rect,errors:&errors) {return true}
    return (a == .success || retry == .success) && positionResult == .success
}
@discardableResult func move(_ element: AXUIElement, _ rect: CGRect) -> Bool { var errors:[String:Int32]=[:]; return move(element,rect,errors:&errors) }
func cocoaToAX(_ rect: CGRect) -> CGRect {
    let top = NSScreen.screens.first?.frame.maxY ?? 0
    return CGRect(x: rect.minX, y: top - rect.maxY, width: rect.width, height: rect.height)
}
func screenID(_ screen: NSScreen) -> UInt32 {
    (screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber)?.uint32Value ?? 0
}
func defaultCells(_ ids: [UInt32]) -> [Cell] {
    guard let first = ids.first else { return [] }
    var cells = [Cell(id: "reading", name: "Leitura", display: first, x: 0, y: 0, w: 1, h: 1)]
    if ids.count > 1 {
        let names = ["Estudos", "Dashboard", "Servidores", "Acompanhamento"]
        for i in 0..<4 { cells.append(Cell(id: "support-\(i)", name: names[i], display: ids[1], x: Double(i % 2) / 2, y: Double(i / 2) / 2, w: 0.5, h: 0.5)) }
    }
    return cells
}
