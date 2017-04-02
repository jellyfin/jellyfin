namespace SharpCifs.Util.Sharpen
{
    public interface IFilenameFilter
	{
		bool Accept (FilePath dir, string name);
	}
}
