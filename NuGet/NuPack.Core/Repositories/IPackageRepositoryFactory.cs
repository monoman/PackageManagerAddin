namespace NuGet {
    public interface IPackageRepositoryFactory {
        IPackageRepository CreateRepository(string source);
    }
}
