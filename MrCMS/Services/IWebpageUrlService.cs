using MrCMS.Entities.Documents.Web;

namespace MrCMS.Services
{
    public interface IWebpageUrlService
    {
        string Suggest(string pageName, Webpage parent, string documentType, bool useHierarchy = false);
    }
}