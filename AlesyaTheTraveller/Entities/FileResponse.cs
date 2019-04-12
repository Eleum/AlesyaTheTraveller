using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class FileResponse : HttpResponseMessage
    {
        public FileResponse(byte[] fileContent, string mediaType, string fileName)
        {
            StatusCode = System.Net.HttpStatusCode.OK;
            Content = new StreamContent(new MemoryStream(fileContent));
            Content.Headers.ContentLength = fileContent.Length;
            Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
            Content.Headers.ContentDisposition = 
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = fileName };
            // allowing to actually read the content of this header
            Content.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
        }
    }
}
