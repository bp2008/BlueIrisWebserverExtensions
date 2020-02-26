using BPUtil;
using BPUtil.SimpleHttp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BlueIrisWebserverExtensions
{
	public class WebServer : HttpServer
	{
		HttpClient httpClient = new HttpClient();
		protected ServerNameCertificateSelector serverNameCertificateSelector;
		public WebServer() : base(MainService.settings.http_port, MainService.settings.https_port, new ServerNameCertificateSelector(), IPAddress.Any)
		{
			// Set up the httpClient we will use for proxying requests
			WebRequestHandler webRequestHandler = new WebRequestHandler();
			webRequestHandler.AllowAutoRedirect = false; // Do not hide redirects
			webRequestHandler.AllowPipelining = false; // We would not want to pipeline a stream by accident, as it would delay other requests indefinitely.
			webRequestHandler.AutomaticDecompression = DecompressionMethods.None; // Use Blue Iris's compression for efficiency.
			httpClient = new HttpClient(webRequestHandler);

			pool = new SimpleThreadPool("Webserver Main Pool", 6, 256);
			if (!Debugger.IsAttached)
				WindowsFirewallHelper.AuthorizeSelf(Globals.AssemblyName);
			serverNameCertificateSelector = certificateSelector as ServerNameCertificateSelector;
			serverNameCertificateSelector.SetCertificate(null, HttpServer.GetSelfSignedCertificate());
		}

		public override void handleGETRequest(HttpProcessor p)
		{
			//if (p.requestedPage == "")
			//{
			//	p.writeSuccess("text/html; charset=UTF-8");
			//}
			ProxyRequestToBlueIris(p);
		}


		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			//p.ProxyTo("http://192.168.0.166:81" + p.request_url.PathAndQuery);
			ProxyRequestToBlueIris(p);
		}

		protected override void stopServer()
		{
		}

		private void ProxyRequestToBlueIris(HttpProcessor p)
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://192.168.0.166:81" + p.request_url.PathAndQuery);
			request.Version = new Version(1, 1);
			request.Headers.ConnectionClose = !p.keepAliveRequested;

			if (p.PostBodyStream != null)
			{
				StreamContent postContent = new StreamContent(p.PostBodyStream);
				if (MediaTypeHeaderValue.TryParse(p.GetHeaderValue("Content-Type"), out MediaTypeHeaderValue parsedContentType))
					postContent.Headers.ContentType = parsedContentType;
				postContent.Headers.ContentLength = p.PostBodyStream.Length;
				request.Method = HttpMethod.Post;
				request.Content = postContent;
			}

			AddProxyableRequestHeaders(p, request.Headers);
			Task<HttpResponseMessage> responseTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
			responseTask.Wait();
			if (responseTask.Exception != null)
			{
				p.writeFullResponseUTF8("An error occurred proxying this request. " + responseTask.Exception.ToString(), "text/plain; charset=UTF-8", "500 Internal Server Error");
				return;
			}

			HttpResponseMessage response = responseTask.Result;
			long contentLength = response.Content.Headers.GetLongValue("Content-Length", -1);
			Task<Stream> streamTask = response.Content.ReadAsStreamAsync();
			streamTask.Wait();
			if (streamTask.Exception != null)
			{
				p.writeFullResponseUTF8("An error occurred proxying this request. " + streamTask.Exception.ToString(), "text/plain; charset=UTF-8", "500 Internal Server Error");
				return;
			}

			List<KeyValuePair<string, string>> responseHeaders = GetProxyableHeaders(response);
			responseHeaders.Add(new KeyValuePair<string, string>("KeepAliveRequestCount", p.keepAliveRequestCount.ToString()));
			p.writeSuccess(response.Content.Headers.GetFirstValue("Content-Type"), contentLength, (int)response.StatusCode + " " + response.StatusCode.ToString(), responseHeaders, contentLength > -1 && p.keepAliveRequested);
			p.outputStream.Flush();
			Stream proxyResponseStream = streamTask.Result;
			proxyResponseStream.CopyTo(p.tcpStream);
		}

		#region Proxy Headers
		private static HashSet<string> dropProxyRequestHeaders = new HashSet<string>()
		{
			"host",
			"connection",
			"content-length",
			"content-type",
			"x-forwarded-for",
			"x-real-ip"
		};
		/// <summary>
		/// Copies headers from the HttpProcessor to the HttpRequestHeaders, leaving out headers defined in <see cref="dropProxyRequestHeaders"/>.
		/// </summary>
		/// <param name="p"></param>
		/// <param name="headers"></param>
		private void AddProxyableRequestHeaders(HttpProcessor p, HttpRequestHeaders headers)
		{
			foreach (KeyValuePair<string, string> header in p.httpHeadersRaw)
				if (!dropProxyRequestHeaders.Contains(header.Key.ToLower()))
				{
					if (!headers.TryAddWithoutValidation(header.Key, header.Value))
						throw new Exception("Header \"" + header.Key + ": " + header.Value + "\" could not be added to HttpRequestHeaders.");
				}
		}
		private static HashSet<string> dropProxyResponseHeaders = new HashSet<string>()
		{
			"content-type",
			"content-length",
			"transfer-encoding",
			"connection"
		};
		/// <summary>
		/// Copies headers from the HttpResponseMessage to a new list, leaving out headers defined in <see cref="dropProxyResponseHeaders"/>.
		/// </summary>
		/// <param name="proxyResponse"></param>
		/// <returns></returns>
		private List<KeyValuePair<string, string>> GetProxyableHeaders(HttpResponseMessage proxyResponse)
		{
			List<KeyValuePair<string, string>> proxyable = new List<KeyValuePair<string, string>>();
			foreach (KeyValuePair<string, IEnumerable<string>> header in proxyResponse.Headers)
			{
				if (!dropProxyResponseHeaders.Contains(header.Key.ToLower()))
				{
					foreach (string value in header.Value)
					{
						proxyable.Add(new KeyValuePair<string, string>(header.Key, value));
					}
				}
			}
			foreach (KeyValuePair<string, IEnumerable<string>> header in proxyResponse.Content.Headers)
			{
				if (!dropProxyResponseHeaders.Contains(header.Key.ToLower()))
				{
					foreach (string value in header.Value)
					{
						proxyable.Add(new KeyValuePair<string, string>(header.Key, value));
					}
				}
			}
			return proxyable;
		}
		#endregion
	}
}
