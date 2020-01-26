namespace wow.tools.api.Resolvers
{

    /*
    filedataid,build
    contenthash
    filename
    */

    public interface IFileResolving
    {
        public string buildConfig { get; }
    }

    public class FilenameFileResolving
    {
        string fileName;
    }
}