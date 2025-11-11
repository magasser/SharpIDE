# Notes to Users

1. Significance of a dotnet/msbuild restore, relating to the Workspace. If you do a restore, and anything has changed, the project/solution has to be reloaded by the MSBuildProjectLoader, then into the Workspace
2. Do not use files without extensions - SharpIDE makes assumptions that "files" without extensions are files, not folders. Relates to IdeFileWatcher. This may be a poor assumption, and can be revisited.
