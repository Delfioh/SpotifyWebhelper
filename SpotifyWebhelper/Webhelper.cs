using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace SpotifyWebhelper
{

    //Very very lazy code loosely based on node-spotify-webhelper, feel free to blame me

    class Webhelper
    {
        int local_port = 4370;
        string return_on = "login,logout,play,pause,error,ap";
        string return_after = "1";
        Parameter origin_header = new Parameter("Origin", "https://open.spotify.com");
        ExpandoObjectConverter converter;
        string oauthToken;
        string csrfToken;

        bool _isInitialised = false;

        public bool isInitialised
        {
            get
            {
                return _isInitialised;
            }
        }

        public Webhelper()
        {
            Init();
        }


        public void Init()
        {
            converter = new ExpandoObjectConverter();
            _isInitialised = false;
            oauthToken = getOauthToken();
            csrfToken = getCsrfToken();
            _isInitialised = (bool)(oauthToken != null && csrfToken != null);
        }

        bool PropertyExists(ExpandoObject settings, string name)
        {
            return ((IDictionary<String, object>)settings).ContainsKey(name);
        }

        string generateRandomString(int length)
        {
            Random rnd = new Random();
            string text = "";

            for (int i = 0; i < length; i++)
            {
                text += (char)rnd.Next((int)'a', (int)'z');
            }

            return text;
        }

        string generateRandomLocalHostName()
        {
            return generateRandomString(10) + ".spotilocal.com";
        }

        ExpandoObject getJson(string url, List<Parameter> parameters, Parameter headers)
        {
            if (parameters != null && parameters.Count > 0)
            {
                url += "?";
                for (int i = 0; i < parameters.Count; i++)
                {
                    url += parameters[i].name + "=" + parameters[i].value;
                    if (i < parameters.Count) url += "&";
                }
            }

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            httpWebRequest.Timeout = 4000; //4 seconds should be enough for detecting timeout

            if (headers != null) httpWebRequest.Headers.Add(headers.name + ":" + headers.value);

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                string result = streamReader.ReadToEnd();
                return JsonConvert.DeserializeObject<ExpandoObject>(result, converter);
            }

        }


        string generateSpotifyUrl(string url)
        {
            return String.Format("https://{0}:{1}{2}", generateRandomLocalHostName(), local_port, url);
        }

        public ExpandoObject getVersion()
        {
            string url = generateSpotifyUrl("/service/version.json");

            List<Parameter> parameters = new List<Parameter>();
            parameters.Add(new Parameter("service", "remote"));

            return getJson(url, parameters, origin_header);
        }

        string getOauthToken()
        {
            dynamic token = getJson("https://open.spotify.com/token", null, null);

            if (PropertyExists(token, "t"))
            {
                return token.t;
            }
            else
            {
                return null;
            }
        }

        
        string getCsrfToken()
        {
            string url = generateSpotifyUrl("/simplecsrf/token.json");
            dynamic token = getJson(url, null, origin_header);

            if (PropertyExists(token, "token"))
            {
                return token.token;
            }
            else
            {
                return null;
            }
        }

        ExpandoObject spotifyJsonRequest(string spotifyRelativeUrl, List<Parameter> additionalParams)
        {
            List<Parameter> parameters = new List<Parameter>();
            parameters.Add(new Parameter("oauth", oauthToken));
            parameters.Add(new Parameter("csrf", csrfToken));
            parameters.AddRange(additionalParams);

            string url = generateSpotifyUrl(spotifyRelativeUrl);
            return getJson(url, parameters, origin_header);
        }

        public ExpandoObject getStatus()
        {
            List<Parameter> parameters = new List<Parameter>();

            parameters.Add(new Parameter("returnafter", return_after));
            parameters.Add(new Parameter("returnon", return_on));

            return spotifyJsonRequest("/remote/status.json", parameters);
        }

    }
}
