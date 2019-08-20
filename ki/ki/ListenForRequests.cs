using System.Net;

namespace ki 
{
    public class HandleRequests{
        public HandleRequests(string prefix)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();
             HttpListenerContext context = listener.GetContext();
             HttpListenerRequest request = context.Request;
             System.Console.WriteLine(GetRequestPostData(request));
        // Obtain a response object.
    HttpListenerResponse response = context.Response;
    // Construct a response.
    string responseString = "<HTML><BODY> Hello world!</BODY></HTML>";
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
    // Get a response stream and write the response to it.
    response.ContentLength64 = buffer.Length;
    System.IO.Stream output = response.OutputStream;
    output.Write(buffer,0,buffer.Length);
    // You must close the output stream.
    output.Close();
    listener.Stop();
        }
private string GetRequestPostData(HttpListenerRequest request)
{
  if (!request.HasEntityBody)
  {
    return null;
  }
  using (System.IO.Stream body = request.InputStream) // here we have data
  {
    using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
    {
      return reader.ReadToEnd();
    }
  }
}
    }
    

}