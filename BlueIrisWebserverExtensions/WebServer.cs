using BPUtil;
using BPUtil.SimpleHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BlueIrisWebserverExtensions
{
	public class WebServer : HttpServer
	{
		public WebServer() : base(MainService.settings.http_port, MainService.settings.https_port)
		{
			pool = new SimpleThreadPool("Webserver Main Pool", 6, 256);
		}

		public override void handleGETRequest(HttpProcessor p)
		{
			p.ProxyTo("http://192.168.0.166:81" + p.request_url.PathAndQuery);
		}

		public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
		{
			p.ProxyTo("http://192.168.0.166:81" + p.request_url.PathAndQuery);
		}

		protected override void stopServer()
		{
		}
	}
}
