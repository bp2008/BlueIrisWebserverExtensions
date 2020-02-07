using BPUtil;

namespace BlueIrisWebserverExtensions
{
	public class Settings : SerializableObjectBase
	{
		public int http_port = 80;
		public int https_port = 443;
	}
}