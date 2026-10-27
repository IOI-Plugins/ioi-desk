// Import a single encrypted identity into the disposable runner keychain.
import Foundation
import Security

guard CommandLine.arguments.count==3,let password=ProcessInfo.processInfo.environment["MACOS_CERTIFICATE_PASSWORD"] else { fatalError("Missing signing import inputs") }
var keychain:SecKeychain?
guard SecKeychainOpen(CommandLine.arguments[2],&keychain)==errSecSuccess,let keychain=keychain else { fatalError("Runner keychain unavailable") }
let data=try Data(contentsOf:URL(fileURLWithPath:CommandLine.arguments[1]))
let options:[String:Any]=[kSecImportExportPassphrase as String:password,kSecImportExportKeychain as String:keychain]
var imported:CFArray?
let result=SecPKCS12Import(data as CFData,options as CFDictionary,&imported)
guard result==errSecSuccess else { fatalError("Signing identity import failed: \(result)") }
print("Signing identity imported into disposable keychain.")
