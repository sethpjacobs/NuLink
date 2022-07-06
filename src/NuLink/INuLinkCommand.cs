namespace NuLink
{
    public interface INuLinkCommand
    {
        int Execute(NuLinkCommandOptions options);
    }
}
