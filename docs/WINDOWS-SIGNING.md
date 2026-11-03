# Code signing policy · Windows

Status: integration prepared; no Windows signing identity has been issued or configured. This page does not claim that SignPath has accepted IOI Desk or signed any release.

IOI Desk's executable and installer require Authenticode signatures. The Ed25519 signature on the update feed authenticates downloads to WinSparkle; it does not replace Authenticode or satisfy Windows App Control. Apple Developer ID certificates apply to macOS only.

## SignPath activation

Apply for the open-source program at https://signpath.org/apply. Eligibility and certificate issuance require SignPath Foundation approval. The code is MIT, and third-party code retains its original open-source licenses. No paid service is provisioned by this integration.

After approval, install the SignPath GitHub App for IOI-Plugins/ioi-desk, link the repository to a trusted GitHub build system and create two ZIP artifact configurations:

- `windows-app`: root `IOIDesk.exe`, signed as an IOI Desk PE file; enforce ProductName `IOI Desk` and version metadata.
- `windows-installer`: root `IOIDesk-<version>-windows-x64-setup.exe`, signed as the IOI Desk installer. Its payload contains the already signed application. Match the exact filename/version for the release.

Do not sign upstream binaries under the IOI Desk project identity. Preserve Deskflow and WinSparkle authorship and existing signatures. Their upstream Windows binaries currently include unsigned components; a restrictive App Control policy may also block them. Signing IOI Desk alone cannot promise compatibility with every policy. Obtain signed upstream builds or a signing arrangement explicitly covering redistribution before claiming full payload coverage. The NSIS-generated uninstaller is likewise not covered by the two signing requests.

Set these variables on the GitHub `release` environment:

- `SIGNPATH_ORGANIZATION_ID`: assigned organization UUID.
- `SIGNPATH_PROJECT_SLUG`: assigned project slug.
- `SIGNPATH_SIGNING_POLICY_SLUG`: approved production signing policy, not a test certificate.
- `WINDOWS_SIGNING_PUBLISHER`: exact Subject of the production code-signing certificate, verified through the provider. The free Foundation certificate may name SignPath Foundation as publisher.

Set the secret `SIGNPATH_API_TOKEN` on that same environment. It needs only permission to submit this project's production signing requests. Do not put a token, certificate private key or personal identity document in the repository or chat. Assign release approvers in the service as required by the provider; approval gates are not bypassed by CI.

## Release guarantees and limits

Only version tags run production signing. The Windows release job waits for the exact commit's CI and CodeQL gate, builds on a GitHub-hosted runner and submits artifact IDs from that build. The SignPath action is pinned to a full commit. The app is signed before packaging; the installer is signed before installer tests and upload. Both must have a Windows-trusted signature, the configured publisher and a timestamp. Missing credentials, refused approval, timeout, invalid signature or unexpected publisher stop Windows publication. There is no unsigned-release fallback.

Ordinary CI still builds unsigned test artifacts; these are not deployable production releases. Once activated, validate a production release on the affected Windows policy before installing it. A trusted signature does not guarantee immediate SmartScreen reputation or override a policy that rejects the publisher. Do not disable protection or install a self-signed root to work around a failed release.

Committers/reviewers: repository maintainers. Release approvers: https://github.com/orgs/IOI-Plugins/people?query=role%3Aowner, subject to the service's configured policy.

Privacy: local zone and diagnostic data remain on the user's machine. Network features requested by the user include device pairing/sharing and update checks; update downloads use GitHub and signing is performed by the CI provider and SignPath on build artifacts, never on user data. Third-party notices are in `third-party/`. Add the Foundation's required attribution only once the application is accepted; no sponsorship is asserted while pending.
