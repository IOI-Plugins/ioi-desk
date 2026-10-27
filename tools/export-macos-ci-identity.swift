// Export only the explicitly selected identity, encrypted for GitHub Secrets.
// Run locally in the owner's logged-in macOS session. Never print key material.
import Foundation
import Security
import CryptoKit

guard CommandLine.arguments.count==4 else { fatalError("Usage: export-identity SHA1 password-file output.p12") }
let wanted=CommandLine.arguments[1].uppercased()
let password=try String(contentsOfFile:CommandLine.arguments[2],encoding:.utf8).trimmingCharacters(in:.whitespacesAndNewlines) as CFString
guard wanted.count==40,CFStringGetLength(password)>=24 else { fatalError("Invalid export selection or encryption password") }
var found:CFTypeRef?
let query:[String:Any]=[kSecClass as String:kSecClassIdentity,kSecReturnRef as String:true,kSecMatchLimit as String:kSecMatchLimitAll]
guard SecItemCopyMatching(query as CFDictionary,&found)==errSecSuccess,let identities=found as? [SecIdentity] else { fatalError("No signing identities available") }
var selected:SecIdentity?
for identity in identities {
    var certificate:SecCertificate?
    guard SecIdentityCopyCertificate(identity,&certificate)==errSecSuccess,let certificate=certificate else { continue }
    let fingerprint=Insecure.SHA1.hash(data:SecCertificateCopyData(certificate) as Data).map{String(format:"%02X",$0)}.joined()
    if fingerprint==wanted { selected=identity;break }
}
guard let identity=selected else { fatalError("Selected signing identity was not found") }
var parameters=SecItemImportExportKeyParameters()
parameters.version=UInt32(SEC_KEY_IMPORT_EXPORT_PARAMS_VERSION)
parameters.passphrase=Unmanaged.passUnretained(password)
var exported:CFData?
let result=SecItemExport(identity,.formatPKCS12,[],&parameters,&exported)
guard result==errSecSuccess,let exported=exported else { fatalError("Selected identity export failed: \(result)") }
let output=URL(fileURLWithPath:CommandLine.arguments[3])
try (exported as Data).write(to:output,options:.atomic)
try FileManager.default.setAttributes([.posixPermissions:0o600],ofItemAtPath:output.path)
print("Selected Developer ID identity exported as encrypted PKCS12; no other identities exported.")
