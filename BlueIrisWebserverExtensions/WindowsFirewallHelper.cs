using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using NetFwTypeLib;

namespace BlueIrisWebserverExtensions
{
	/// <summary>
	/// To use this class, add the COM Library "NetFwTypeLib" to the project references.
	/// </summary>
	public static class WindowsFirewallHelper
	{
		/// <summary>
		/// Authorizes this program to receive incoming communications through Windows Firewall. If this fails (typically due to insufficient permission), the exception is returned.
		/// </summary>
		/// <param name="appName">The title of the firewall rule/program.</param>
		/// <param name="remoteAddresses">Comma separated list of remote addresses to authorize, or "*" for all.</param>
		/// <returns></returns>
		public static Exception AuthorizeSelf(string appName, string remoteAddresses = "*")
		{
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
			return AuthorizeProgram(appName, assembly.Location, remoteAddresses);
		}

		/// <summary>
		/// Authorizes a program to receive incoming communications through Windows Firewall. If this fails (typically due to insufficient permission), the exception is returned.
		/// </summary>
		/// <param name="title">The title of the firewall rule/program.</param>
		/// <param name="path">Path to the executable that should be authorized.</param>
		/// <param name="remoteAddresses">Comma separated list of remote addresses to authorize, or "*" for all.</param>
		/// <param name="protocol">Internet protocol version.</param>
		/// <param name="networkProfiles">Internet protocol version.</param>
		/// <returns></returns>
		public static Exception AuthorizeProgram(string title
			, string path
			, string remoteAddresses = "*"
			, NET_FW_IP_PROTOCOL_ protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY
			, NET_FW_PROFILE_TYPE2_ networkProfiles = NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN | NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE)
		{
			try
			{
				INetFwPolicy2 fwPolicy2 = Create<INetFwPolicy2>("HNetCfg.FwPolicy2");

				foreach (INetFwRule existingRule in fwPolicy2.Rules)
				{
					if (path.Equals(existingRule.ApplicationName, StringComparison.OrdinalIgnoreCase))
					{
						if (existingRule.Protocol == (int)protocol
							&& existingRule.Profiles == (int)networkProfiles
							&& existingRule.Action == NET_FW_ACTION_.NET_FW_ACTION_ALLOW
							&& existingRule.Enabled)
						{
							if (existingRule.RemoteAddresses != remoteAddresses)
								existingRule.RemoteAddresses = remoteAddresses;
							return null;
						}
					}
				}

				INetFwRule rule = Create<INetFwRule>("HNetCfg.FwRule");
				rule.Name = title;
				rule.Description = "Allow incoming traffic to " + title;
				rule.ApplicationName = path;
				rule.Protocol = (int)protocol;
				rule.Enabled = true;
				rule.Grouping = title;
				rule.RemoteAddresses = remoteAddresses;
				rule.Profiles = (int)networkProfiles;
				rule.Action = NET_FW_ACTION_.NET_FW_ACTION_ALLOW;
				fwPolicy2.Rules.Add(rule);
			}
			catch (Exception ex)
			{
				return ex;
			}
			return null;
		}
		/// <summary>
		/// Creates an instance of the type with the specified "ProgID".
		/// </summary>
		/// <typeparam name="T">Type to return. Must be compatible with [progId].</typeparam>
		/// <param name="progId">ProgID string.</param>
		/// <returns></returns>
		private static T Create<T>(string progId)
		{
			Type type = Type.GetTypeFromProgID(progId);
			T obj = (T)Activator.CreateInstance(type);
			return obj;
		}
	}
}
