//  Copyright (C) 2011, Kenneth Skovhede

//  http://www.hexad.dk, opensource@hexad.dk
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections.Specialized;
using System.Web;

namespace Duplicati.Library.Utility
{
    /// <summary>
    /// Represents a relaxed parsing of a URL.
    /// The goal is to cover as many types of url's as possible,
    /// without being ambiguos.
    /// The major limitations is that an embedded username may not contain a :,
    /// and the password may not contain a @.
    /// </summary>
    public struct Uri
    {
        /// <summary>
        /// A very lax version of a URL parser
        /// </summary>
        private static System.Text.RegularExpressions.Regex URL_PARSER = new System.Text.RegularExpressions.Regex(@"(?<scheme>[^:]+)://(((?<username>[^\:]+)\:((?<password>[^@]*))\@))?((?<hostname>[^/\?\:]+)(\:(?<port>\d+))?(/(?<path>[^\?]*))|(?<path>([^\?]*)))?(\?(?<query>.+))?");

        /// <summary>
        /// The URL scheme, e.g. http
        /// </summary>
        public readonly string Scheme;
        /// <summary>
        /// The server name, e.g. www.example.com
        /// </summary>
        public readonly string Host;
        /// <summary>
        /// The server path, e.g. index.html
        /// </summary>
        public readonly string Path;
        /// <summary>
        /// The server port, e.g. 80, is -1 if using the default port
        /// </summary>
        public readonly int Port;
        /// <summary>
        /// The querystring, e.g. ?id=1
        /// </summary>
        public readonly string Query;
        /// <summary>
        /// The username, if any
        /// </summary>
        public readonly string Username;
        /// <summary>
        /// The password, if any
        /// </summary>
        public readonly string Password;
        
        /// <summary>
        /// The original URI.
        /// </summary>
        public readonly string OriginalUri;
        
        /// <summary>
        /// Cache for the query parameters.
        /// </summary>
        private NameValueCollection m_queryParams;
        
        /// <summary>
        /// Gets the paramters in the query string
        /// </summary>
        /// <value>The query parameters.</value>
        public NameValueCollection QueryParameters
        {
            get
            {
                if (m_queryParams == null)
                {
                    if (Query == null)
                        m_queryParams = new NameValueCollection();
                    else
                        m_queryParams = HttpUtility.ParseQueryString(Query);
                }
                
                return m_queryParams;
            }
        }
        
        /// <summary>
        /// Gets the host and path.
        /// </summary>
        /// <value>The host and path.</value>
        public string HostAndPath
        {
            get
            {
                if (string.IsNullOrEmpty(Path))
                    return Host;
                else if (string.IsNullOrEmpty(Host))
                    return Path;
                else
                    return Host + (Path == null ? "" : "/" + Path);
            }
        }
        
        /// <summary>
        /// Gets the path and query.
        /// </summary>
        /// <value>The path and query.</value>
        public string PathAndQuery
        {
            get
            {
                return (Path ?? "") + (Query == null ? "" : "?" + Query);            
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Duplicati.Library.Utility.Utility.Uri"/> struct.
        /// </summary>
        /// <param name="url">The URL to parse</param>
        public Uri(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException("url");
            
            m_queryParams = null;
            this.OriginalUri = url;

            var m = URL_PARSER.Match(url);
            if (!m.Success || m.Length != url.Length)
            {
                if (url.IndexOfAny(System.IO.Path.GetInvalidPathChars()) < 0)
                    try 
                    {
                        var fp = System.IO.Path.GetFullPath(url);
                        this.Scheme = "file";
                        this.Host = null;
                        this.Path = fp;
                        this.Port = -1;
                        this.Query = null;
                        this.Username = null;
                        this.Password = null;
                        return;
                    }
                    catch
                    {
                    }
                throw new ArgumentException(string.Format(Strings.Uri.UriParseError, url), url);
            }
                
            this.Scheme = m.Groups["scheme"].Value;
            this.Host = m.Groups["hostname"].Value;
            this.Path = m.Groups["path"].Success ? m.Groups["path"].Value : null;
            this.Query = m.Groups["query"].Success ? m.Groups["query"].Value : null;
            this.Username = m.Groups["username"].Success ? HttpUtility.UrlDecode(m.Groups["username"].Value) : null;
            this.Password = m.Groups["password"].Success ? HttpUtility.UrlDecode(m.Groups["password"].Value) : null;
            if (m.Groups["port"].Success)
                this.Port = int.Parse(m.Groups["port"].Value);
            else
                this.Port = -1;
        }
        
        /// <summary>
        /// Constructs a free-form URI from components
        /// </summary>
        /// <param name="scheme">The url scheme, e.g. http</param>
        /// <param name="host">The hostname, e.g. www.example.com</param>
        /// <param name="path">The path, e.g. index.html</param>
        /// <param name="query">The query string, e.g. id=1</param>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <param name="port">The port</param>
        public Uri(string scheme, string host, string path = null, string query = null, string username = null, string password = null, int port = -1)
        {
            m_queryParams = null;
            Scheme = scheme;
            Host = host;
            Path = path;
            Query = query;
            Username = username;
            Password = password;
            Port = port;
            OriginalUri = AsString(scheme, host, path, query, username, password, port);
        }
        
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents the current <see cref="Duplicati.Library.Utility.Uri"/>.
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Duplicati.Library.Utility.Uri"/>.</returns>
        public override string ToString ()
        {
            return AsString(Scheme, Host, Path, Query, Username, Password, Port);
        }

        /// <summary>
        /// Throws an exception if the host name is missing.
        /// </summary>
        public void RequireHost()
        {
            if (string.IsNullOrEmpty(Host))
                throw new ArgumentException(string.Format(Strings.Uri.NoHostname, OriginalUri));
        }
        
        /// <summary>
        /// Constructs an url-like string from components.
        /// </summary>
        /// <returns>An url-like string</returns>
        /// <param name="scheme">The url scheme, e.g. http</param>
        /// <param name="host">The hostname, e.g. www.example.com</param>
        /// <param name="path">The path, e.g. index.html</param>
        /// <param name="query">The query string, e.g. id=1</param>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <param name="port">The port</param>
        private static string AsString(string scheme, string host, string path, string query, string username, string password, int port)
        {
            var s = scheme + "://";
            if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            {
                
                s += HttpUtility.UrlEncode(username ?? "");
                s += ":";
                s += HttpUtility.UrlEncode(password ?? "");
                s += "@";
            }
            
            if (!string.IsNullOrEmpty(host))
            {
                s += host;
                if (port != -1)
                    s += ":" + port.ToString();
            }
            
            if (!string.IsNullOrEmpty(path))
            {
                if (!string.IsNullOrEmpty(host))
                    s += "/";
                s += path;
            }
            if (!string.IsNullOrEmpty(query))
                s += "?" + query;

            return s;             
        }
        
        /// <summary>
        /// Creates a new instance with another scheme
        /// </summary>
        /// <returns>A new instance</returns>
        /// <param name="scheme">The new scheme to use</param>
        public Uri SetScheme(string scheme)
        {
            return new Uri(scheme, Host, Path, Query, Username, Password, Port);
        }
        
        /// <summary>
        /// Creates a new instance with another host
        /// </summary>
        /// <returns>A new instance</returns>
        /// <param name="host">The new hostname to use</param>
        public Uri SetHost(string host)
        {
            return new Uri(Scheme, host, Path, Query, Username, Password, Port);
        }
        
        /// <summary>
        /// Creates a new instance with another path
        /// </summary>
        /// <returns>A new instance</returns>
        /// <param name="path">The new path to use</param>
        public Uri SetPath(string path)
        {
            return new Uri(Scheme, Host, path, Query, Username, Password, Port);
        }
        
        /// <summary>
        /// Creates a new instance with another query
        /// </summary>
        /// <returns>A new instance</returns>
        /// <param name="query">The new query to use</param>
        public Uri SetQuery(string query)
        {
            return new Uri(Scheme, Host, Path, query, Username, Password, Port);
        }

        /// <summary>
        /// Creates a new instance with other credentials
        /// </summary>
        /// <returns>A new instance</returns>
        /// <param name="username">The new username to use</param>
        /// <param name="password">The new password to use</param>
        public Uri SetCredentials(string username, string password)
        {
            return new Uri(Scheme, Host, Path, Query, username, password, Port);
        }
        
        /// <summary>
        /// Creates a new instance with another port
        /// </summary>
        /// <returns>A new instance</returns>
        /// <param name="port">The new port to use</param>
        public Uri SetPort(int port)
        {
            return new Uri(Scheme, Host, Path, Query, Username, Password, port);
        }
        
        /// <summary>
        /// Initializes the <see cref="Duplicati.Library.Utility.Uri"/> struct.
        /// </summary>
        static Uri()
        {
            //Mono bug-fix
            try { HttpUtility.UrlEncode("test@web.de"); }
            catch {}
        }
    }
}

