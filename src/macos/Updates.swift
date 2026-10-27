import AppKit
import Sparkle

final class DeskUpdates {
    private let controller: SPUStandardUpdaterController
    init() {
        controller=SPUStandardUpdaterController(startingUpdater:false,updaterDelegate:nil,userDriverDelegate:nil)
    }
    func start() {
        // CLI tests never instantiate this service. Respect Sparkle preferences.
        guard Bundle.main.object(forInfoDictionaryKey:"SUPublicEDKey") as? String != nil else{return}
        controller.startUpdater()
    }
    func addMenu(to menu:NSMenu) {
        let item=NSMenuItem(title:"Verificar atualizações…",action:#selector(SPUStandardUpdaterController.checkForUpdates(_:)),keyEquivalent:"")
        item.target=controller
        menu.addItem(item)
    }
    static func test() throws {
        let info=Bundle.main.infoDictionary ?? [:]
        guard let key=info["SUPublicEDKey"] as? String,Data(base64Encoded:key)?.count==32,
              let text=info["SUFeedURL"] as? String,let url=URL(string:text),url.scheme=="https",url.host=="github.com",
              info["SUVerifyUpdateBeforeExtraction"] as? Bool == true,
              info["SURequireSignedFeed"] as? Bool == true,
              NSClassFromString("SPUStandardUpdaterController") != nil else {
            throw NSError(domain:"IOIDesk.Updates",code:1,userInfo:[NSLocalizedDescriptionKey:"Invalid Sparkle trust configuration"])
        }
        print("PASS: original Sparkle framework loads; HTTPS feed and 32-byte public key configured; archive and feed signatures required")
    }
}
