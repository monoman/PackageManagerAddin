namespace NuGetConsole {
    /// <summary>
    /// Simple (line, lastWord) based tab expansion interface. A host can implement
    /// this interface and reuse CommandExpansion/CommandExpansionProvider.
    /// </summary>
    public interface ITabExpansion {
        string[] GetExpansions(string line, string lastWord);
    }
}
