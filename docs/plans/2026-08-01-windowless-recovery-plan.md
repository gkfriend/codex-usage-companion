# Windowless Recovery Implementation Plan

## Goal

Release version 0.3.5 with a consoleless scheduled recovery check every three minutes.

## Tasks

1. Extend recovery tests to require a Windows Script Host wrapper, a `wscript.exe` task action, and a `PT3M` repetition interval.
2. Run the focused test and confirm it fails against the current implementation.
3. Add the wrapper and update installation, uninstallation, and package scripts.
4. Update product version assertions and all English, Traditional Chinese, and Simplified Chinese documentation.
5. Run focused tests, the full test suite, release build, and package verification.
6. Install the release candidate locally and verify the task action, interval, process count, and absence of visible console windows.
7. Commit, push, open and merge a pull request, tag version 0.3.5, and verify the GitHub release assets.
8. Synchronize the locally enabled plugin with the released version and perform a final recovery check.
