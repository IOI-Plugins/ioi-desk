# IOI Desk

Canonical refactored sources are in src/. Read README.md and docs/VALIDATION.md before working.

- release.json is the product version and identity source; build tools generate platform metadata.
- Keep user data outside executable and bundle directories. Migrations copy supported data without deleting or overwriting user originals.
- macOS has one executable and one application delegate. WorkspaceController and DeskConnection are internal services, not separate IOI apps.
- Deskflow is an original third-party process, never renamed or represented as IOI-owned code. Preserve its notices and licensing.
- Production macOS builds require IOI_CODESIGN_IDENTITY. IOI_ALLOW_ADHOC=1 is for isolated tests only, never installation over the user's authorized app.
- Current installations outside this directory are legacy deployments, not alternative source trees. Do not overwrite them until the signed migration has been validated.
- Use tools/build-windows.ps1, tools/test-windows.ps1, tools/build-macos.sh and tools/check-source.py. Tests use isolated data directories.
- Never reset privacy databases, change certificate trust settings, or delete current user sessions as part of a build/update.
