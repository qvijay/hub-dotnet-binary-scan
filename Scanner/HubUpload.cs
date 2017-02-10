﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Text;
using System.Threading.Tasks;

namespace Blackduck.Hub
{
	class HubUpload
	{
		private const string UPLOAD_URI = "api/v1/scans/upload";
		private const string AUTH_URI = "j_spring_security_check";

		public static void UploadScan(string baseUrl, string username, string password, ScannerJsonBuilder scanResult)
		{
			IEnumerable<string> authCookies = authenticate(baseUrl, username, password);

			string requestUrl = $"{(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/")}{UPLOAD_URI}";
			HttpWebRequest uploadRequest = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
			uploadRequest.Method = "POST";


			foreach (string authCookie in authCookies)
			{
				uploadRequest.Headers.Add(HttpRequestHeader.Cookie, authCookie);
			}

			uploadRequest.ContentType = "multipart/form-data; boundary=----SCAN_UPLOADrfoy1iy3n41oui3dyn14oius4hn";

				
			using (var outWriter = new StreamWriter(uploadRequest.GetRequestStream()))
			{
				scanResult.Write(outWriter);
			}

			HttpWebResponse response = (HttpWebResponse)uploadRequest.GetResponse();
			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new Exception($"Unable to upload to hub. Status code: {response.StatusCode}");
			}

		}

		/// <summary>
		/// Authenticates with the hub and returns the cookies to set.
		/// </summary>
		/// <param name="baseUrl"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		private static IEnumerable<string> authenticate(string baseUrl, string username, string password)
		{
			string requestUrl = $"{(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/")}{AUTH_URI}";
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";


			StringBuilder formData = new StringBuilder();
			formData.Append("j_username=" + HttpUtility.UrlEncode(username));
			formData.Append("&j_password="+HttpUtility.UrlEncode(password));
			byte[] requestData = Encoding.ASCII.GetBytes(formData.ToString());

			using (var stream = request.GetRequestStream())
				stream.Write(requestData, 0, requestData.Length);

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.OK)
			{
				throw new Exception("Unable to authenticate into " + baseUrl);
			}

			string[] cookies = response.Headers.GetValues("Set-Cookie");
			var cleansedCookies = new List<string>(cookies.Length);
			foreach (string cookie in cookies.Where(c => !string.IsNullOrEmpty(c)))
			{
				string cleansedCookie = string.Join(";", cookie.Split(';').Where(c => !c.StartsWith("Path=")));
				cleansedCookies.Add(cleansedCookie);
			}
			return cleansedCookies;
		}
	}
}
