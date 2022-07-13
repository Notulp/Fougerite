using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Stream = System.IO.Stream;

namespace Fougerite
{
    /// <summary>
    /// This class helps plugins to use simple web requests.
    /// Some Certifications are not supported in Mono when using TLS requests.
    /// </summary>
    public class Web
    {
        private static Web _webInstance;
        
        /// <summary>
        /// SSL Protocols.
        /// </summary>
        [Flags]
        public enum MySecurityProtocolType
        {
            //
            // Summary:
            //     Specifies the Secure Socket Layer (SSL) 3.0 security protocol.
            Ssl3 = 48,

            //
            // Summary:
            //     Specifies the Transport Layer Security (TLS) 1.0 security protocol.
            Tls = 192,

            //
            // Summary:
            //     Specifies the Transport Layer Security (TLS) 1.1 security protocol.
            // I'll leave It here, but the upgraded dlls we are using are supporting 1.0 max If i have seen right.
            Tls11 = 768,

            //
            // Summary:
            //     Specifies the Transport Layer Security (TLS) 1.2 security protocol.
            // Unsupported in Mono. https://forum.unity.com/threads/unity-2017-1-tls-1-2-still-not-working-with-net-4-6.487415/
            [Obsolete("Doesn't work in this version of Mono. See https://forum.unity.com/threads/unity-2017-1-tls-1-2-still-not-working-with-net-4-6.487415/", false)]
            Tls12 = 3072
        }

        private Web()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)(MySecurityProtocolType.Tls11 | MySecurityProtocolType.Tls | MySecurityProtocolType.Ssl3);
            ServicePointManager.ServerCertificateValidationCallback = AcceptAllCertifications;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 200;
        }

        /// <summary>
        /// Returns the instance of the Web class.
        /// </summary>
        /// <returns></returns>
        public static Web GetInstance()
        {
            if (_webInstance == null)
            {
                _webInstance = new Web();
            }

            return _webInstance;
        }

        /// <summary>
        /// Does a GET request to the specified URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [Obsolete("Use CreateAsyncHTTPRequest instead.", false)]
        public string GET(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        /// <summary>
        /// Does a post request to the specified URL with the data.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="contentType">For JSON format specify 'application/json'</param>
        /// <returns></returns>
        [Obsolete("Use CreateAsyncHTTPRequest instead.", false)]
        public string POST(string url, string data, string contentType = "application/x-www-form-urlencoded")
        {
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = contentType;
                byte[] bytes = client.UploadData(url, "POST", Encoding.UTF8.GetBytes(data));
                return Encoding.UTF8.GetString(bytes);
            }
        }

        /// <summary>
        /// Does a GET request to the specified URL, and accepts all SSL certificates.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [Obsolete("Use CreateAsyncHTTPRequest instead.", false)]
        public string GETWithSSL(string url)
        {
            return GET(url);
        }

        /// <summary>
        /// Does a post request to the specified URL with the data, and accepts all SSL certificates.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [Obsolete("Use CreateAsyncHTTPRequest instead.", false)]
        public string POSTWithSSL(string url, string data)
        {
            return POST(url, data);
        }


        /// <summary>
        /// Creates an Async request for the specified URL, and headers.
        /// The result will be passed to the specified callback's parameter.
        /// This is the recommended way to do requests.
        /// Python example:
        /// clr.AddReferenceByPartialName('System.Core')
        /// import System
        /// import json
        /// from System import Action
        /// Web.CreateAsyncHTTPRequest('url', Action[int, str](self.webCallback), 'POST', json.dumps({'name':'test'}))
        /// 
        /// WARNING: This is an async call. The callback will be on a subthread. If you have something that you need to run
        /// on the main thread, because It's thread sensitive then call Loom's QueueOnMainThread function.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="callback"></param>
        /// <param name="method"></param>
        /// <param name="inputBody"></param>
        /// <param name="additionalHeaders"></param>
        /// <param name="contentType">For JSON format specify 'application/json'</param>
        /// <param name="timeout"></param>
        /// <param name="allowDecompression"></param>
        public void CreateAsyncHTTPRequest(string url, Action<int, string> callback, string method = "GET",
            string inputBody = null,
            Dictionary<string, string> additionalHeaders = null, 
            string contentType = "application/x-www-form-urlencoded",
            float timeout = 0f,
            bool allowDecompression = false)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = method;
            request.Credentials = CredentialCache.DefaultCredentials;
            request.KeepAlive = false;
            if (timeout > 0f)
            {
                request.Timeout = (int) Math.Round(timeout);
            }
            request.AutomaticDecompression = allowDecompression ? DecompressionMethods.GZip | DecompressionMethods.Deflate 
                : DecompressionMethods.None;
            request.ServicePoint.MaxIdleTime = request.Timeout;
            request.ServicePoint.Expect100Continue = ServicePointManager.Expect100Continue;
            request.ServicePoint.ConnectionLimit = ServicePointManager.DefaultConnectionLimit;
            request.UserAgent = $"Fougerite Mod (v{Bootstrap.Version}; https://fougerite.com)";
            request.ContentType = contentType;

            byte[] input = new byte[0];
            if (!string.IsNullOrEmpty(inputBody))
            {
                input = Encoding.UTF8.GetBytes(inputBody);
                request.ContentLength = input.Length;
            }

            if (additionalHeaders != null)
            {
                foreach (var x in additionalHeaders.Keys)
                {
                    request.SetRawHeader(x, additionalHeaders[x]);
                }
            }
            
            // Are we posting anything?
            if (input.Length > 0)
            {
                request.BeginGetRequestStream(result =>
                {
                    try
                    {
                        // Write request body
                        using (Stream stream = request.EndGetRequestStream(result))
                        {
                            stream.Write(input, 0, input.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("[CreateAsyncHTTPRequest Failed] Error: " + ex);
                    }
                }, null);
            }

            DoWithResponse(request, response =>
            {
                string body = "Failed";
                int responseCode = 200;
                try
                {
                    Stream stream = response.GetResponseStream();
                    if (stream != null)
                    {
                        body = new StreamReader(stream).ReadToEnd();
                    }

                    responseCode = (int)response.StatusCode;
                }
                catch (WebException ex)
                {
                    // Grab the response directly from the exception instead.
                    if (ex.Response is HttpWebResponse httpWebResponse)
                    {
                        try
                        {
                            // Try to read
                            Stream stream = httpWebResponse.GetResponseStream();
                            if (stream != null)
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    body = reader.ReadToEnd();
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Ignore
                        }
                        
                        responseCode = (int) httpWebResponse.StatusCode;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("[CreateAsyncHTTPRequest Failed] Error: " + ex);
                }

                try
                {
                    callback(responseCode, body);
                }
                catch (Exception ex)
                {
                    Logger.LogError("[CreateAsyncHTTPRequest Callback] Error: " + ex);
                }
            });
        }

        /// <summary>
        /// This handles the Async webrequests of the CreateAsyncHTTPRequest method.
        /// You can use this if you are creating your own HttpWebRequest instance.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseAction"></param>
        public void DoWithResponse(HttpWebRequest request, Action<HttpWebResponse> responseAction)
        {
            Action wrapperAction = () =>
            {
                request.BeginGetResponse(iar =>
                {
                    var response = (HttpWebResponse)((HttpWebRequest)iar.AsyncState).EndGetResponse(iar);
                    responseAction(response);
                }, request);
            };
            wrapperAction.BeginInvoke(iar =>
            {
                var action = (Action)iar.AsyncState;
                action.EndInvoke(iar);
            }, wrapperAction);
        }
        
        /// <summary>
        /// We do not care what the SSL cert gives, we accept any type literally.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslpolicyerrors"></param>
        /// <returns></returns>
        private bool AcceptAllCertifications(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }
    }

    // https://stackoverflow.com/questions/239725/cannot-set-some-http-headers-when-using-system-net-webrequest
    public static class HttpWebRequestExtensions
    {
        private static readonly string[] RestrictedHeaders = {
            "Accept",
            "Connection",
            "Content-Length",
            "Content-Type",
            "Date",
            "Expect",
            "Host",
            "If-Modified-Since",
            "Keep-Alive",
            "Proxy-Connection",
            "Range",
            "Referer",
            "Transfer-Encoding",
            "User-Agent"
        };

        private static readonly Dictionary<string, PropertyInfo> HeaderProperties =
            new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        static HttpWebRequestExtensions()
        {
            Type type = typeof(HttpWebRequest);
            foreach (string header in RestrictedHeaders)
            {
                string propertyName = header.Replace("-", "");
                PropertyInfo headerProperty = type.GetProperty(propertyName);
                HeaderProperties[header] = headerProperty;
            }
        }

        public static void SetRawHeader(this HttpWebRequest request, string name, string value)
        {
            if (HeaderProperties.ContainsKey(name))
            {
                PropertyInfo property = HeaderProperties[name];
                if (property.PropertyType == typeof(DateTime))
                    property.SetValue(request, DateTime.Parse(value), null);
                else if (property.PropertyType == typeof(bool))
                    property.SetValue(request, Boolean.Parse(value), null);
                else if (property.PropertyType == typeof(long))
                    property.SetValue(request, Int64.Parse(value), null);
                else
                    property.SetValue(request, value, null);
            }
            else
            {
                request.Headers[name] = value;
            }
        }
    }
}