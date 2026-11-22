// Shared by the two opt-in persistence test assemblies, never production code.
internal static class PersistenceTestPaths
{
    internal static string TemporaryRoot()
    {
        // macOS exposes its normal temporary directory through /var -> /private/var.
        // Resolve the existing fixture root before creating owned directories; the
        // repository must continue rejecting symlink/reparse ancestors unchanged.
        var full = Path.GetFullPath(Path.GetTempPath());
        var root = Path.GetPathRoot(full)!;
        var current = root;
        foreach (var part in full.Substring(root.Length).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            var next = new DirectoryInfo(Path.Combine(current, part));
            current = next.LinkTarget == null ? next.FullName : next.ResolveLinkTarget(true)!.FullName;
        }
        return current;
    }
}
