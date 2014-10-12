/* This file is part of OpenSubtitles Handler
   A library that handle OpenSubtitles.org XML-RPC methods.

   Copyright © Ala Ibrahim Hadid 2013

   This program is free software: you can redistribute it and/or modify
   it under the terms of the GNU General Public License as published by
   the Free Software Foundation, either version 3 of the License, or
   (at your option) any later version.

   This program is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenSubtitlesHandler.Console;
using XmlRpcHandler;

namespace OpenSubtitlesHandler
{
    /// <summary>
    /// The core of the OpenSubtitles Handler library. All members are static.
    /// </summary>
    public sealed class OpenSubtitles
    {
        private static string XML_PRC_USERAGENT = "";
        // This is session id after log in, important value and MUST be set by LogIn before any other call.
        private static string TOKEN = "";

        /// <summary>
        /// Set the useragent value. This must be called before doing anything else.
        /// </summary>
        /// <param name="agent">The useragent value</param>
        public static void SetUserAgent(string agent)
        {
            XML_PRC_USERAGENT = agent;
        }

        /*Session handling*/
        /// <summary>
        /// Send a LogIn request, this must be called before anything else in this class.
        /// </summary>
        /// <param name="userName">The user name of OpenSubtitles</param>
        /// <param name="password">The password</param>
        /// <param name="language">The language, usally en</param>
        /// <returns>Status of the login operation</returns>
        public static IMethodResponse LogIn(string userName, string password, string language)
        {
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(userName));
            parms.Add(new XmlRpcValueBasic(password));
            parms.Add(new XmlRpcValueBasic(language));
            parms.Add(new XmlRpcValueBasic(XML_PRC_USERAGENT));
            XmlRpcMethodCall call = new XmlRpcMethodCall("LogIn", parms);
            OSHConsole.WriteLine("Sending LogIn request to the server ...", DebugCode.Good);

            //File.WriteAllText(".\\request.txt", Encoding.UTF8.GetString(XmlRpcGenerator.Generate(call)));
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));

            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. We expect Struct here.
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        MethodResponseLogIn re = new MethodResponseLogIn("Success", "Log in successful.");
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "token": re.Token = TOKEN = MEMBER.Data.Data.ToString(); OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": re.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "status": re.Status = MEMBER.Data.Data.ToString(); OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        return re;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "Log in failed !");
        }

        public static async Task<IMethodResponse> LogInAsync(string userName, string password, string language, CancellationToken cancellationToken)
        {
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(userName));
            parms.Add(new XmlRpcValueBasic(password));
            parms.Add(new XmlRpcValueBasic(language));
            parms.Add(new XmlRpcValueBasic(XML_PRC_USERAGENT));
            XmlRpcMethodCall call = new XmlRpcMethodCall("LogIn", parms);
            OSHConsole.WriteLine("Sending LogIn request to the server ...", DebugCode.Good);

            //File.WriteAllText(".\\request.txt", Encoding.UTF8.GetString(XmlRpcGenerator.Generate(call)));
            // Send the request to the server
            var stream = await Utilities.SendRequestAsync(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT, cancellationToken)
                .ConfigureAwait(false);

            string response = Utilities.GetStreamString(stream);

            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. We expect Struct here.
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        MethodResponseLogIn re = new MethodResponseLogIn("Success", "Log in successful.");
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "token": 
                                    re.Token = TOKEN = MEMBER.Data.Data.ToString(); 
                                    OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); 
                                    break;
                                case "seconds":
                                    re.Seconds = double.Parse(MEMBER.Data.Data.ToString(), CultureInfo.InvariantCulture);
                                    OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); 
                                    break;
                                case "status": 
                                    re.Status = MEMBER.Data.Data.ToString(); 
                                    OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); 
                                    break;
                            }
                        }
                        return re;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "Log in failed !");
        }
        
        /// <summary>
        /// Log out from the server. Call this to terminate the session.
        /// </summary>
        /// <returns></returns>
        public static IMethodResponse LogOut()
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            XmlRpcMethodCall call = new XmlRpcMethodCall("LogOut", parms);

            OSHConsole.WriteLine("Sending LogOut request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. We expect Struct here.
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct strct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        OSHConsole.WriteLine("STATUS=" + ((XmlRpcValueBasic)strct.Members[0].Data).Data.ToString());
                        OSHConsole.WriteLine("SECONDS=" + ((XmlRpcValueBasic)strct.Members[1].Data).Data.ToString());
                        MethodResponseLogIn re = new MethodResponseLogIn("Success", "Log out successful.");
                        re.Status = ((XmlRpcValueBasic)strct.Members[0].Data).Data.ToString();
                        re.Seconds = (double)((XmlRpcValueBasic)strct.Members[1].Data).Data;
                        return re;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "Log out failed !");
        }
        /// <summary>
        /// keep-alive user's session, verify token/session validity 
        /// </summary>
        /// <returns>Status of the call operation</returns>
        public static IMethodResponse NoOperation()
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            parms.Add(new XmlRpcValueBasic(XML_PRC_USERAGENT, XmlRpcBasicValueType.String));
            XmlRpcMethodCall call = new XmlRpcMethodCall("NoOperation", parms);

            OSHConsole.WriteLine("Sending NoOperation request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. We expect Struct here.
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        MethodResponseNoOperation R = new MethodResponseNoOperation();
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(MEMBER.Name + "= " + MEMBER.Data.Data); break;
                                case "download_limits":
                                    XmlRpcValueStruct dlStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember dlmember in dlStruct.Members)
                                    {
                                        OSHConsole.WriteLine(" >" + dlmember.Name + "= " + dlmember.Data.Data.ToString());
                                        switch (dlmember.Name)
                                        {
                                            case "global_wrh_download_limit": R.global_wrh_download_limit = dlmember.Data.Data.ToString(); break;
                                            case "client_ip": R.client_ip = dlmember.Data.Data.ToString(); break;
                                            case "limit_check_by": R.limit_check_by = dlmember.Data.Data.ToString(); break;
                                            case "client_24h_download_count": R.client_24h_download_count = dlmember.Data.Data.ToString(); break;
                                            case "client_downlaod_quota": R.client_downlaod_quota = dlmember.Data.Data.ToString(); break;
                                            case "client_24h_download_limit": R.client_24h_download_limit = dlmember.Data.Data.ToString(); break;
                                        }
                                    }
                                    break;
                            }
                        }
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "NoOperation call failed !");
        }
        /*Search and download*/
        /// <summary>
        /// Search for subtitle files matching your videos using either video file hashes or IMDb IDs. 
        /// </summary>
        /// <param name="parameters">List of search subtitle parameters which each one represents 'struct parameter' as descriped at http://trac.opensubtitles.org/projects/opensubtitles/wiki/XmlRpcSearchSubtitles </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseSubtitleSearch'</returns>
        public static IMethodResponse SearchSubtitles(SubtitleSearchParameters[] parameters)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            if (parameters == null)
            {
                OSHConsole.UpdateLine("No subtitle search parameter passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle search parameter passed"); ;
            }
            if (parameters.Length == 0)
            {
                OSHConsole.UpdateLine("No subtitle search parameter passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle search parameter passed"); ;
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Add subtitle search parameters. Each one will be like 'array' of structs.
            XmlRpcValueArray array = new XmlRpcValueArray();
            foreach (SubtitleSearchParameters param in parameters)
            {
                XmlRpcValueStruct strct = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                // sublanguageid member
                XmlRpcStructMember member = new XmlRpcStructMember("sublanguageid",
                    new XmlRpcValueBasic(param.SubLangaugeID, XmlRpcBasicValueType.String));
                strct.Members.Add(member);
                // moviehash member
                if (param.MovieHash.Length > 0 && param.MovieByteSize > 0) {
                    member = new XmlRpcStructMember("moviehash",
                        new XmlRpcValueBasic(param.MovieHash, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                    // moviehash member
                    member = new XmlRpcStructMember("moviebytesize",
                        new XmlRpcValueBasic(param.MovieByteSize, XmlRpcBasicValueType.Int));
                    strct.Members.Add(member);
                }
                if (param.Query.Length > 0) {
                    member = new XmlRpcStructMember("query",
                        new XmlRpcValueBasic(param.Query, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                }

                 if (param.Episode.Length > 0 && param.Season.Length>0) {
                     member = new XmlRpcStructMember("season",
                         new XmlRpcValueBasic(param.Season, XmlRpcBasicValueType.String));
                     strct.Members.Add(member);
                     member = new XmlRpcStructMember("episode",
                       new XmlRpcValueBasic(param.Episode, XmlRpcBasicValueType.String));
                     strct.Members.Add(member);
                }

                // imdbid member
                if (param.IMDbID.Length > 0)
                {
                    member = new XmlRpcStructMember("imdbid",
                        new XmlRpcValueBasic(param.IMDbID, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                }
                // Add the struct to the array
                array.Values.Add(strct);
            }
            // Add the array to the parameters
            parms.Add(array);
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("SearchSubtitles", parms);
            OSHConsole.WriteLine("Sending SearchSubtitles request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));

            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        // We expect Struct of 3 members:
                        //* the first is status
                        //* the second is [array of structs, each one includes subtitle file].
                        //* the third is [double basic value] represent seconds token by server.
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseSubtitleSearch R = new MethodResponseSubtitleSearch();
                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                if (MEMBER.Data is XmlRpcValueArray)
                                {
                                    OSHConsole.WriteLine("Search results: ");

                                    XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue subStruct in rarray.Values)
                                    {
                                        if (subStruct == null) continue;
                                        if (!(subStruct is XmlRpcValueStruct)) continue;

                                        SubtitleSearchResult result = new SubtitleSearchResult();
                                        foreach (XmlRpcStructMember submember in ((XmlRpcValueStruct)subStruct).Members)
                                        {
                                            // To avoid errors of arranged info or missing ones, let's do it with switch..
                                            switch (submember.Name)
                                            {
                                                case "IDMovie": result.IDMovie = submember.Data.Data.ToString(); break;
                                                case "IDMovieImdb": result.IDMovieImdb = submember.Data.Data.ToString(); break;
                                                case "IDSubMovieFile": result.IDSubMovieFile = submember.Data.Data.ToString(); break;
                                                case "IDSubtitle": result.IDSubtitle = submember.Data.Data.ToString(); break;
                                                case "IDSubtitleFile": result.IDSubtitleFile = submember.Data.Data.ToString(); break;
                                                case "ISO639": result.ISO639 = submember.Data.Data.ToString(); break;
                                                case "LanguageName": result.LanguageName = submember.Data.Data.ToString(); break;
                                                case "MovieByteSize": result.MovieByteSize = submember.Data.Data.ToString(); break;
                                                case "MovieHash": result.MovieHash = submember.Data.Data.ToString(); break;
                                                case "MovieImdbRating": result.MovieImdbRating = submember.Data.Data.ToString(); break;
                                                case "MovieName": result.MovieName = submember.Data.Data.ToString(); break;
                                                case "MovieNameEng": result.MovieNameEng = submember.Data.Data.ToString(); break;
                                                case "MovieReleaseName": result.MovieReleaseName = submember.Data.Data.ToString(); break;
                                                case "MovieTimeMS": result.MovieTimeMS = submember.Data.Data.ToString(); break;
                                                case "MovieYear": result.MovieYear = submember.Data.Data.ToString(); break;
                                                case "SubActualCD": result.SubActualCD = submember.Data.Data.ToString(); break;
                                                case "SubAddDate": result.SubAddDate = submember.Data.Data.ToString(); break;
                                                case "SubAuthorComment": result.SubAuthorComment = submember.Data.Data.ToString(); break;
                                                case "SubBad": result.SubBad = submember.Data.Data.ToString(); break;
                                                case "SubDownloadLink": result.SubDownloadLink = submember.Data.Data.ToString(); break;
                                                case "SubDownloadsCnt": result.SubDownloadsCnt = submember.Data.Data.ToString(); break;
                                                case "SeriesEpisode": result.SeriesEpisode = submember.Data.Data.ToString(); break;
                                                case "SeriesSeason": result.SeriesSeason = submember.Data.Data.ToString(); break;
                                                case "SubFileName": result.SubFileName = submember.Data.Data.ToString(); break;
                                                case "SubFormat": result.SubFormat = submember.Data.Data.ToString(); break;
                                                case "SubHash": result.SubHash = submember.Data.Data.ToString(); break;
                                                case "SubLanguageID": result.SubLanguageID = submember.Data.Data.ToString(); break;
                                                case "SubRating": result.SubRating = submember.Data.Data.ToString(); break;
                                                case "SubSize": result.SubSize = submember.Data.Data.ToString(); break;
                                                case "SubSumCD": result.SubSumCD = submember.Data.Data.ToString(); break;
                                                case "UserID": result.UserID = submember.Data.Data.ToString(); break;
                                                case "UserNickName": result.UserNickName = submember.Data.Data.ToString(); break;
                                                case "ZipDownloadLink": result.ZipDownloadLink = submember.Data.Data.ToString(); break;
                                            }
                                        }
                                        R.Results.Add(result);
                                        OSHConsole.WriteLine(">" + result.ToString());
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "Search Subtitles call failed !");
        }

        public static async Task<IMethodResponse> SearchSubtitlesAsync(SubtitleSearchParameters[] parameters, CancellationToken cancellationToken)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            if (parameters == null)
            {
                OSHConsole.UpdateLine("No subtitle search parameter passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle search parameter passed"); ;
            }
            if (parameters.Length == 0)
            {
                OSHConsole.UpdateLine("No subtitle search parameter passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle search parameter passed"); ;
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Add subtitle search parameters. Each one will be like 'array' of structs.
            XmlRpcValueArray array = new XmlRpcValueArray();
            foreach (SubtitleSearchParameters param in parameters)
            {
                XmlRpcValueStruct strct = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                // sublanguageid member
                XmlRpcStructMember member = new XmlRpcStructMember("sublanguageid",
                    new XmlRpcValueBasic(param.SubLangaugeID, XmlRpcBasicValueType.String));
                strct.Members.Add(member);
                // moviehash member
                if (param.MovieHash.Length > 0 && param.MovieByteSize > 0)
                {
                    member = new XmlRpcStructMember("moviehash",
                        new XmlRpcValueBasic(param.MovieHash, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                    // moviehash member
                    member = new XmlRpcStructMember("moviebytesize",
                        new XmlRpcValueBasic(param.MovieByteSize, XmlRpcBasicValueType.Int));
                    strct.Members.Add(member);
                }
                if (param.Query.Length > 0)
                {
                    member = new XmlRpcStructMember("query",
                        new XmlRpcValueBasic(param.Query, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                }

                if (param.Episode.Length > 0 && param.Season.Length > 0)
                {
                    member = new XmlRpcStructMember("season",
                        new XmlRpcValueBasic(param.Season, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                    member = new XmlRpcStructMember("episode",
                      new XmlRpcValueBasic(param.Episode, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                }

                // imdbid member
                if (param.IMDbID.Length > 0)
                {
                    member = new XmlRpcStructMember("imdbid",
                        new XmlRpcValueBasic(param.IMDbID, XmlRpcBasicValueType.String));
                    strct.Members.Add(member);
                }
                // Add the struct to the array
                array.Values.Add(strct);
            }
            // Add the array to the parameters
            parms.Add(array);
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("SearchSubtitles", parms);
            OSHConsole.WriteLine("Sending SearchSubtitles request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(await Utilities.SendRequestAsync(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT, cancellationToken).ConfigureAwait(false));

            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        // We expect Struct of 3 members:
                        //* the first is status
                        //* the second is [array of structs, each one includes subtitle file].
                        //* the third is [double basic value] represent seconds token by server.
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseSubtitleSearch R = new MethodResponseSubtitleSearch();
                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                if (MEMBER.Data is XmlRpcValueArray)
                                {
                                    OSHConsole.WriteLine("Search results: ");

                                    XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue subStruct in rarray.Values)
                                    {
                                        if (subStruct == null) continue;
                                        if (!(subStruct is XmlRpcValueStruct)) continue;

                                        SubtitleSearchResult result = new SubtitleSearchResult();
                                        foreach (XmlRpcStructMember submember in ((XmlRpcValueStruct)subStruct).Members)
                                        {
                                            // To avoid errors of arranged info or missing ones, let's do it with switch..
                                            switch (submember.Name)
                                            {
                                                case "IDMovie": result.IDMovie = submember.Data.Data.ToString(); break;
                                                case "IDMovieImdb": result.IDMovieImdb = submember.Data.Data.ToString(); break;
                                                case "IDSubMovieFile": result.IDSubMovieFile = submember.Data.Data.ToString(); break;
                                                case "IDSubtitle": result.IDSubtitle = submember.Data.Data.ToString(); break;
                                                case "IDSubtitleFile": result.IDSubtitleFile = submember.Data.Data.ToString(); break;
                                                case "ISO639": result.ISO639 = submember.Data.Data.ToString(); break;
                                                case "LanguageName": result.LanguageName = submember.Data.Data.ToString(); break;
                                                case "MovieByteSize": result.MovieByteSize = submember.Data.Data.ToString(); break;
                                                case "MovieHash": result.MovieHash = submember.Data.Data.ToString(); break;
                                                case "MovieImdbRating": result.MovieImdbRating = submember.Data.Data.ToString(); break;
                                                case "MovieName": result.MovieName = submember.Data.Data.ToString(); break;
                                                case "MovieNameEng": result.MovieNameEng = submember.Data.Data.ToString(); break;
                                                case "MovieReleaseName": result.MovieReleaseName = submember.Data.Data.ToString(); break;
                                                case "MovieTimeMS": result.MovieTimeMS = submember.Data.Data.ToString(); break;
                                                case "MovieYear": result.MovieYear = submember.Data.Data.ToString(); break;
                                                case "SubActualCD": result.SubActualCD = submember.Data.Data.ToString(); break;
                                                case "SubAddDate": result.SubAddDate = submember.Data.Data.ToString(); break;
                                                case "SubAuthorComment": result.SubAuthorComment = submember.Data.Data.ToString(); break;
                                                case "SubBad": result.SubBad = submember.Data.Data.ToString(); break;
                                                case "SubDownloadLink": result.SubDownloadLink = submember.Data.Data.ToString(); break;
                                                case "SubDownloadsCnt": result.SubDownloadsCnt = submember.Data.Data.ToString(); break;
                                                case "SeriesEpisode": result.SeriesEpisode = submember.Data.Data.ToString(); break;
                                                case "SeriesSeason": result.SeriesSeason = submember.Data.Data.ToString(); break;
                                                case "SubFileName": result.SubFileName = submember.Data.Data.ToString(); break;
                                                case "SubFormat": result.SubFormat = submember.Data.Data.ToString(); break;
                                                case "SubHash": result.SubHash = submember.Data.Data.ToString(); break;
                                                case "SubLanguageID": result.SubLanguageID = submember.Data.Data.ToString(); break;
                                                case "SubRating": result.SubRating = submember.Data.Data.ToString(); break;
                                                case "SubSize": result.SubSize = submember.Data.Data.ToString(); break;
                                                case "SubSumCD": result.SubSumCD = submember.Data.Data.ToString(); break;
                                                case "UserID": result.UserID = submember.Data.Data.ToString(); break;
                                                case "UserNickName": result.UserNickName = submember.Data.Data.ToString(); break;
                                                case "ZipDownloadLink": result.ZipDownloadLink = submember.Data.Data.ToString(); break;
                                            }
                                        }
                                        R.Results.Add(result);
                                        OSHConsole.WriteLine(">" + result.ToString());
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "Search Subtitles call failed !");
        }
        
        /// <summary>
        /// Download subtitle file(s)
        /// </summary>
        /// <param name="subIDS">The subtitle IDS (an array of IDSubtitleFile value that given by server as SearchSubtiles results)</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseSubtitleDownload' which will hold downloaded subtitles</returns>
        public static IMethodResponse DownloadSubtitles(int[] subIDS)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            if (subIDS == null)
            {
                OSHConsole.UpdateLine("No subtitle id passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle id passed"); ;
            }
            if (subIDS.Length == 0)
            {
                OSHConsole.UpdateLine("No subtitle id passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle id passed"); ;
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Add subtitle search parameters. Each one will be like 'array' of structs.
            XmlRpcValueArray array = new XmlRpcValueArray();
            foreach (int id in subIDS)
            {
                array.Values.Add(new XmlRpcValueBasic(id, XmlRpcBasicValueType.Int));
            }
            // Add the array to the parameters
            parms.Add(array);
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("DownloadSubtitles", parms);
            OSHConsole.WriteLine("Sending DownloadSubtitles request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        // We expect Struct of 3 members:
                        //* the first is status
                        //* the second is [array of structs, each one includes subtitle file].
                        //* the third is [double basic value] represent seconds token by server.
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseSubtitleDownload R = new MethodResponseSubtitleDownload();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                if (MEMBER.Data is XmlRpcValueArray)
                                {
                                    OSHConsole.WriteLine("Download results:");
                                    XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue subStruct in rarray.Values)
                                    {
                                        if (subStruct == null) continue;
                                        if (!(subStruct is XmlRpcValueStruct)) continue;

                                        SubtitleDownloadResult result = new SubtitleDownloadResult();
                                        foreach (XmlRpcStructMember submember in ((XmlRpcValueStruct)subStruct).Members)
                                        {
                                            // To avoid errors of arranged info or missing ones, let's do it with switch..
                                            switch (submember.Name)
                                            {
                                                case "idsubtitlefile": result.IdSubtitleFile = (string)submember.Data.Data; break;
                                                case "data": result.Data = (string)submember.Data.Data; break;
                                            }
                                        }
                                        R.Results.Add(result);
                                        OSHConsole.WriteLine("> IDSubtilteFile= " + result.ToString());
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "DownloadSubtitles call failed !");
        }

        public static async Task<IMethodResponse> DownloadSubtitlesAsync(int[] subIDS, CancellationToken cancellationToken)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            if (subIDS == null)
            {
                OSHConsole.UpdateLine("No subtitle id passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle id passed"); ;
            }
            if (subIDS.Length == 0)
            {
                OSHConsole.UpdateLine("No subtitle id passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle id passed"); ;
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Add subtitle search parameters. Each one will be like 'array' of structs.
            XmlRpcValueArray array = new XmlRpcValueArray();
            foreach (int id in subIDS)
            {
                array.Values.Add(new XmlRpcValueBasic(id, XmlRpcBasicValueType.Int));
            }
            // Add the array to the parameters
            parms.Add(array);
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("DownloadSubtitles", parms);
            OSHConsole.WriteLine("Sending DownloadSubtitles request to the server ...", DebugCode.Good);
            // Send the request to the server

            var httpResponse = await Utilities.SendRequestAsync(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT, cancellationToken).ConfigureAwait(false);

            string response = Utilities.GetStreamString(httpResponse);
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        // We expect Struct of 3 members:
                        //* the first is status
                        //* the second is [array of structs, each one includes subtitle file].
                        //* the third is [double basic value] represent seconds token by server.
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseSubtitleDownload R = new MethodResponseSubtitleDownload();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                if (MEMBER.Data is XmlRpcValueArray)
                                {
                                    OSHConsole.WriteLine("Download results:");
                                    XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue subStruct in rarray.Values)
                                    {
                                        if (subStruct == null) continue;
                                        if (!(subStruct is XmlRpcValueStruct)) continue;

                                        SubtitleDownloadResult result = new SubtitleDownloadResult();
                                        foreach (XmlRpcStructMember submember in ((XmlRpcValueStruct)subStruct).Members)
                                        {
                                            // To avoid errors of arranged info or missing ones, let's do it with switch..
                                            switch (submember.Name)
                                            {
                                                case "idsubtitlefile": result.IdSubtitleFile = (string)submember.Data.Data; break;
                                                case "data": result.Data = (string)submember.Data.Data; break;
                                            }
                                        }
                                        R.Results.Add(result);
                                        OSHConsole.WriteLine("> IDSubtilteFile= " + result.ToString());
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "DownloadSubtitles call failed !");
        }
        
        /// <summary>
        /// Returns comments for subtitles
        /// </summary>
        /// <param name="subIDS">The subtitle IDS (an array of IDSubtitleFile value that given by server as SearchSubtiles results)</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseGetComments'</returns>
        public static IMethodResponse GetComments(int[] subIDS)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            if (subIDS == null)
            {
                OSHConsole.UpdateLine("No subtitle id passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle id passed"); ;
            }
            if (subIDS.Length == 0)
            {
                OSHConsole.UpdateLine("No subtitle id passed !!", DebugCode.Error);
                return new MethodResponseError("Fail", "No subtitle id passed"); ;
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN));
            // Add subtitle search parameters. Each one will be like 'array' of structs.
            XmlRpcValueArray array = new XmlRpcValueArray(subIDS);
            // Add the array to the parameters
            parms.Add(array);
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("GetComments", parms);
            OSHConsole.WriteLine("Sending GetComments request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseGetComments R = new MethodResponseGetComments();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                if (MEMBER.Data is XmlRpcValueArray)
                                {
                                    OSHConsole.WriteLine("Comments results:");
                                    XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue commentStruct in rarray.Values)
                                    {
                                        if (commentStruct == null) continue;
                                        if (!(commentStruct is XmlRpcValueStruct)) continue;

                                        GetCommentsResult result = new GetCommentsResult();
                                        foreach (XmlRpcStructMember commentmember in ((XmlRpcValueStruct)commentStruct).Members)
                                        {
                                            // To avoid errors of arranged info or missing ones, let's do it with switch..
                                            switch (commentmember.Name)
                                            {
                                                case "IDSubtitle": result.IDSubtitle = (string)commentmember.Data.Data; break;
                                                case "UserID": result.UserID = (string)commentmember.Data.Data; break;
                                                case "UserNickName": result.UserNickName = (string)commentmember.Data.Data; break;
                                                case "Comment": result.Comment = (string)commentmember.Data.Data; break;
                                                case "Created": result.Created = (string)commentmember.Data.Data; break;
                                            }
                                        }
                                        R.Results.Add(result);
                                        OSHConsole.WriteLine("> IDSubtitle= " + result.ToString());
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "GetComments call failed !");
        }

        /// <summary>
        /// Schedule a periodical search for subtitles matching given video files, send results to user's e-mail address.
        /// </summary>
        /// <param name="languageIDS">The language 3 lenght ids array</param>
        /// <param name="movies">The movies parameters</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseSearchToMail'</returns>
        public static IMethodResponse SearchToMail(string[] languageIDS, SearchToMailMovieParameter[] movies)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Array of sub langs
            XmlRpcValueArray a = new XmlRpcValueArray(languageIDS);
            parms.Add(a);
            // Array of video parameters
            a = new XmlRpcValueArray();
            foreach (SearchToMailMovieParameter p in movies)
            {
                XmlRpcValueStruct str = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                str.Members.Add(new XmlRpcStructMember("moviehash", new XmlRpcValueBasic(p.moviehash)));
                str.Members.Add(new XmlRpcStructMember("moviesize", new XmlRpcValueBasic(p.moviesize)));
                a.Values.Add(str);
            }
            parms.Add(a);
            XmlRpcMethodCall call = new XmlRpcMethodCall("SearchToMail", parms);

            OSHConsole.WriteLine("Sending SearchToMail request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseSearchToMail R = new MethodResponseSearchToMail();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "SearchToMail call failed !");
        }
        /*Movies*/
        /// <summary>
        /// Search for a movie (using movie title) 
        /// </summary>
        /// <param name="query">Movie title user is searching for, this is cleaned-up a bit (remove dvdrip, etc.) before searching </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseSubtitleSearch'</returns>
        public static IMethodResponse SearchMoviesOnIMDB(string query)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Add query param
            parms.Add(new XmlRpcValueBasic(query, XmlRpcBasicValueType.String));
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("SearchMoviesOnIMDB", parms);
            OSHConsole.WriteLine("Sending SearchMoviesOnIMDB request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseMovieSearch R = new MethodResponseMovieSearch();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                if (MEMBER.Data is XmlRpcValueArray)
                                {
                                    OSHConsole.WriteLine("Search results:");
                                    XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue subStruct in rarray.Values)
                                    {
                                        if (subStruct == null) continue;
                                        if (!(subStruct is XmlRpcValueStruct)) continue;

                                        MovieSearchResult result = new MovieSearchResult();
                                        foreach (XmlRpcStructMember submember in ((XmlRpcValueStruct)subStruct).Members)
                                        {
                                            // To avoid errors of arranged info or missing ones, let's do it with switch..
                                            switch (submember.Name)
                                            {
                                                case "id": result.ID = (string)submember.Data.Data; break;
                                                case "title": result.Title = (string)submember.Data.Data; break;
                                            }
                                        }
                                        R.Results.Add(result);
                                        OSHConsole.WriteLine(">" + result.ToString());
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "SearchMoviesOnIMDB call failed !");
        }
        /// <summary>
        /// Get movie details for given IMDb ID 
        /// </summary>
        /// <param name="imdbid">http://www.imdb.com/</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseMovieDetails'</returns>
        public static IMethodResponse GetIMDBMovieDetails(string imdbid)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN));
            // Add query param
            parms.Add(new XmlRpcValueBasic(imdbid));
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("GetIMDBMovieDetails", parms);
            OSHConsole.WriteLine("Sending GetIMDBMovieDetails request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseMovieDetails R = new MethodResponseMovieDetails();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "data")
                            {
                                // We expect struct with details...
                                if (MEMBER.Data is XmlRpcValueStruct)
                                {
                                    OSHConsole.WriteLine("Details result:");
                                    XmlRpcValueStruct detailsStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember dmem in detailsStruct.Members)
                                    {
                                        switch (dmem.Name)
                                        {
                                            case "id": R.ID = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "title": R.Title = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "year": R.Year = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "cover": R.CoverLink = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "duration": R.Duration = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "tagline": R.Tagline = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "plot": R.Plot = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "goofs": R.Goofs = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "trivia": R.Trivia = dmem.Data.Data.ToString(); OSHConsole.WriteLine(">" + dmem.Name + "= " + dmem.Data.Data.ToString()); break;
                                            case "cast":
                                                // this is another struct with cast members...
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                XmlRpcValueStruct castStruct = (XmlRpcValueStruct)dmem.Data;
                                                foreach (XmlRpcStructMember castMemeber in castStruct.Members)
                                                {
                                                    R.Cast.Add(castMemeber.Data.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + castMemeber.Data.Data.ToString());
                                                }
                                                break;
                                            case "directors":
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                // this is another struct with directors members...
                                                XmlRpcValueStruct directorsStruct = (XmlRpcValueStruct)dmem.Data;
                                                foreach (XmlRpcStructMember directorsMember in directorsStruct.Members)
                                                {
                                                    R.Directors.Add(directorsMember.Data.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + directorsMember.Data.Data.ToString());
                                                }
                                                break;
                                            case "writers":
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                // this is another struct with writers members...
                                                XmlRpcValueStruct writersStruct = (XmlRpcValueStruct)dmem.Data;
                                                foreach (XmlRpcStructMember writersMember in writersStruct.Members)
                                                {
                                                    R.Writers.Add(writersMember.Data.Data.ToString());
                                                    OSHConsole.WriteLine("+->" + writersMember.Data.Data.ToString());
                                                }
                                                break;
                                            case "awards":
                                                // this is an array of genres...
                                                XmlRpcValueArray awardsArray = (XmlRpcValueArray)dmem.Data;
                                                foreach (XmlRpcValueBasic award in awardsArray.Values)
                                                {
                                                    R.Awards.Add(award.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + award.Data.ToString());
                                                }
                                                break;
                                            case "genres":
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                // this is an array of genres...
                                                XmlRpcValueArray genresArray = (XmlRpcValueArray)dmem.Data;
                                                foreach (XmlRpcValueBasic genre in genresArray.Values)
                                                {
                                                    R.Genres.Add(genre.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + genre.Data.ToString());
                                                }
                                                break;
                                            case "country":
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                // this is an array of country...
                                                XmlRpcValueArray countryArray = (XmlRpcValueArray)dmem.Data;
                                                foreach (XmlRpcValueBasic country in countryArray.Values)
                                                {
                                                    R.Country.Add(country.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + country.Data.ToString());
                                                }
                                                break;
                                            case "language":
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                // this is an array of language...
                                                XmlRpcValueArray languageArray = (XmlRpcValueArray)dmem.Data;
                                                foreach (XmlRpcValueBasic language in languageArray.Values)
                                                {
                                                    R.Language.Add(language.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + language.Data.ToString());
                                                }
                                                break;
                                            case "certification":
                                                OSHConsole.WriteLine(">" + dmem.Name + "= ");
                                                // this is an array of certification...
                                                XmlRpcValueArray certificationArray = (XmlRpcValueArray)dmem.Data;
                                                foreach (XmlRpcValueBasic certification in certificationArray.Values)
                                                {
                                                    R.Certification.Add(certification.Data.ToString());
                                                    OSHConsole.WriteLine("  >" + certification.Data.ToString());
                                                }
                                                break;
                                        }
                                    }
                                }
                                else// Unknown data ?
                                {
                                    OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                }
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "GetIMDBMovieDetails call failed !");
        }
        /// <summary>
        /// Allows registered users to insert new movies (not stored in IMDb) to the database. 
        /// </summary>
        /// <param name="movieName">Movie title </param>
        /// <param name="movieyear">Release year </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseInsertMovie'</returns>
        public static IMethodResponse InsertMovie(string movieName, string movieyear)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // Add token param
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // Add movieinfo struct
            XmlRpcValueStruct movieinfo = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            movieinfo.Members.Add(new XmlRpcStructMember("moviename", new XmlRpcValueBasic(movieName)));
            movieinfo.Members.Add(new XmlRpcStructMember("movieyear", new XmlRpcValueBasic(movieyear)));
            parms.Add(movieinfo);
            // Call !
            XmlRpcMethodCall call = new XmlRpcMethodCall("InsertMovie", parms);
            OSHConsole.WriteLine("Sending InsertMovie request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseInsertMovie R = new MethodResponseInsertMovie();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            if (MEMBER.Name == "status")
                            {
                                R.Status = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Status= " + R.Status);
                            }
                            else if (MEMBER.Name == "seconds")
                            {
                                R.Seconds = (double)MEMBER.Data.Data;
                                OSHConsole.WriteLine("Seconds= " + R.Seconds);
                            }
                            else if (MEMBER.Name == "id")
                            {
                                R.ID = (string)MEMBER.Data.Data;
                                OSHConsole.WriteLine("ID= " + R.Seconds);
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "InsertMovie call failed !");
        }
        /// <summary>
        /// Inserts or updates data to tables, which are used for CheckMovieHash() and !CheckMovieHash2().
        /// </summary>
        /// <param name="parameters">The parameters</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseInsertMovieHash'</returns>
        public static IMethodResponse InsertMovieHash(InsertMovieHashParameters[] parameters)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            foreach (InsertMovieHashParameters p in parameters)
            {
                XmlRpcValueStruct pstruct = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                pstruct.Members.Add(new XmlRpcStructMember("moviehash", new XmlRpcValueBasic(p.moviehash)));
                pstruct.Members.Add(new XmlRpcStructMember("moviebytesize", new XmlRpcValueBasic(p.moviebytesize)));
                pstruct.Members.Add(new XmlRpcStructMember("imdbid", new XmlRpcValueBasic(p.imdbid)));
                pstruct.Members.Add(new XmlRpcStructMember("movietimems", new XmlRpcValueBasic(p.movietimems)));
                pstruct.Members.Add(new XmlRpcStructMember("moviefps", new XmlRpcValueBasic(p.moviefps)));
                pstruct.Members.Add(new XmlRpcStructMember("moviefilename", new XmlRpcValueBasic(p.moviefilename)));
                parms.Add(pstruct);
            }
            XmlRpcMethodCall call = new XmlRpcMethodCall("InsertMovieHash", parms);

            OSHConsole.WriteLine("Sending InsertMovieHash request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseInsertMovieHash R = new MethodResponseInsertMovieHash();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status":
                                    R.Status = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "seconds":
                                    R.Seconds = (double)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "data":
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember dataMember in dataStruct.Members)
                                    {
                                        switch (dataMember.Name)
                                        {
                                            case "accepted_moviehashes":
                                                XmlRpcValueArray mh = (XmlRpcValueArray)dataMember.Data;
                                                foreach (IXmlRpcValue val in mh.Values)
                                                {
                                                    if (val is XmlRpcValueBasic)
                                                    {
                                                        R.accepted_moviehashes.Add(val.Data.ToString());
                                                    }
                                                }
                                                break;
                                            case "new_imdbs":
                                                XmlRpcValueArray mi = (XmlRpcValueArray)dataMember.Data;
                                                foreach (IXmlRpcValue val in mi.Values)
                                                {
                                                    if (val is XmlRpcValueBasic)
                                                    {
                                                        R.new_imdbs.Add(val.Data.ToString());
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "InsertMovieHash call failed !");
        }
        /*Reporting and rating*/
        /// <summary>
        /// Get basic server information and statistics 
        /// </summary>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseServerInfo'</returns>
        public static IMethodResponse ServerInfo()
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            parms.Add(new XmlRpcValueBasic(XML_PRC_USERAGENT, XmlRpcBasicValueType.String));
            XmlRpcMethodCall call = new XmlRpcMethodCall("ServerInfo", parms);

            OSHConsole.WriteLine("Sending ServerInfo request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseServerInfo R = new MethodResponseServerInfo();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status":
                                    R.Status = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "seconds":
                                    R.Seconds = (double)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "xmlrpc_version":
                                    R.xmlrpc_version = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "xmlrpc_url":
                                    R.xmlrpc_url = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "application":
                                    R.application = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "contact":
                                    R.contact = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "website_url":
                                    R.website_url = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "users_online_total":
                                    R.users_online_total = (int)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "users_online_program":
                                    R.users_online_program = (int)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "users_loggedin":
                                    R.users_loggedin = (int)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "users_max_alltime":
                                    R.users_max_alltime = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "users_registered":
                                    R.users_registered = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "subs_downloads":
                                    R.subs_downloads = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "subs_subtitle_files":
                                    R.subs_subtitle_files = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "movies_total":
                                    R.movies_total = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "movies_aka":
                                    R.movies_aka = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "total_subtitles_languages":
                                    R.total_subtitles_languages = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "last_update_strings":
                                    //R.total_subtitles_languages = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + ":");
                                    XmlRpcValueStruct luStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember luMemeber in luStruct.Members)
                                    {
                                        R.last_update_strings.Add(luMemeber.Name + " [" + luMemeber.Data.Data.ToString() + "]");
                                        OSHConsole.WriteLine("  >" + luMemeber.Name + "= " + luMemeber.Data.Data.ToString());
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "ServerInfo call failed !");
        }
        /// <summary>
        /// Report wrong subtitle file <--> video file combination
        /// </summary>
        /// <param name="IDSubMovieFile">Identifier of the subtitle file <--> video file combination </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseReportWrongMovieHash'</returns>
        public static IMethodResponse ReportWrongMovieHash(string IDSubMovieFile)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            parms.Add(new XmlRpcValueBasic(IDSubMovieFile, XmlRpcBasicValueType.String));
            XmlRpcMethodCall call = new XmlRpcMethodCall("ReportWrongMovieHash", parms);

            OSHConsole.WriteLine("Sending ReportWrongMovieHash request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseReportWrongMovieHash R = new MethodResponseReportWrongMovieHash();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status":
                                    R.Status = (string)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                                case "seconds":
                                    R.Seconds = (double)MEMBER.Data.Data;
                                    OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString());
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "ReportWrongMovieHash call failed !");
        }
        /// <summary>
        /// This method is needed to report bad movie hash for imdbid. This method should be used for correcting wrong entries, 
        /// when using CheckMovieHash2. Pass moviehash and moviebytesize for file, and imdbid as new, corrected one IMDBID 
        /// (id number without trailing zeroes). After some reports, moviehash will be linked to new imdbid. 
        /// </summary>
        /// <param name="moviehash">The movie hash</param>
        /// <param name="moviebytesize">The movie size in bytes</param>
        /// <param name="imdbid">The movie imbid</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseReportWrongImdbMovie'</returns>
        public static IMethodResponse ReportWrongImdbMovie(string moviehash, string moviebytesize, string imdbid)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            XmlRpcValueStruct s = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            s.Members.Add(new XmlRpcStructMember("moviehash", new XmlRpcValueBasic(moviehash)));
            s.Members.Add(new XmlRpcStructMember("moviebytesize", new XmlRpcValueBasic(moviebytesize)));
            s.Members.Add(new XmlRpcStructMember("imdbid", new XmlRpcValueBasic(imdbid)));
            parms.Add(s);
            XmlRpcMethodCall call = new XmlRpcMethodCall("ReportWrongImdbMovie", parms);

            OSHConsole.WriteLine("Sending ReportWrongImdbMovie request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseAddComment R = new MethodResponseAddComment();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "ReportWrongImdbMovie call failed !");
        }
        /// <summary>
        /// Rate subtitles 
        /// </summary>
        /// <param name="idsubtitle">Id of subtitle (NOT subtitle file) user wants to rate </param>
        /// <param name="score">Subtitle rating, must be in interval 1 (worst) to 10 (best). </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseSubtitlesVote'</returns>
        public static IMethodResponse SubtitlesVote(int idsubtitle, int score)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            XmlRpcValueStruct s = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            s.Members.Add(new XmlRpcStructMember("idsubtitle", new XmlRpcValueBasic(idsubtitle)));
            s.Members.Add(new XmlRpcStructMember("score", new XmlRpcValueBasic(score)));
            parms.Add(s);
            XmlRpcMethodCall call = new XmlRpcMethodCall("SubtitlesVote", parms);

            OSHConsole.WriteLine("Sending SubtitlesVote request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseSubtitlesVote R = new MethodResponseSubtitlesVote();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember dataMemeber in dataStruct.Members)
                                    {
                                        OSHConsole.WriteLine("  >" + dataMemeber.Name + "= " + dataMemeber.Data.Data.ToString());
                                        switch (dataMemeber.Name)
                                        {
                                            case "SubRating": R.SubRating = dataMemeber.Data.Data.ToString(); break;
                                            case "SubSumVotes": R.SubSumVotes = dataMemeber.Data.Data.ToString(); break;
                                            case "IDSubtitle": R.IDSubtitle = dataMemeber.Data.Data.ToString(); break;
                                        }
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "SubtitlesVote call failed !");
        }
        /// <summary>
        /// Add comment to a subtitle 
        /// </summary>
        /// <param name="idsubtitle">Subtitle identifier (BEWARE! this is not the ID of subtitle file but of the whole subtitle (a subtitle can contain multiple subtitle files))</param>
        /// <param name="comment">User's comment</param>
        /// <param name="badsubtitle">Optional parameter. If set to 1, subtitles are marked as bad.</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseAddComment'</returns>
        public static IMethodResponse AddComment(int idsubtitle, string comment, int badsubtitle)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            XmlRpcValueStruct s = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            s.Members.Add(new XmlRpcStructMember("idsubtitle", new XmlRpcValueBasic(idsubtitle)));
            s.Members.Add(new XmlRpcStructMember("comment", new XmlRpcValueBasic(comment)));
            s.Members.Add(new XmlRpcStructMember("badsubtitle", new XmlRpcValueBasic(badsubtitle)));
            parms.Add(s);
            XmlRpcMethodCall call = new XmlRpcMethodCall("AddComment", parms);

            OSHConsole.WriteLine("Sending AddComment request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseAddComment R = new MethodResponseAddComment();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "AddComment call failed !");
        }
        /// <summary>
        /// Add new request for subtitles, user must be logged in
        /// </summary>
        /// <param name="sublanguageid">The subtitle language id 3 length</param>
        /// <param name="idmovieimdb">http://www.imdb.com/</param>
        /// <param name="comment">The comment</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseAddRequest'</returns>
        public static IMethodResponse AddRequest(string sublanguageid, string idmovieimdb, string comment)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            XmlRpcValueStruct s = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            s.Members.Add(new XmlRpcStructMember("sublanguageid", new XmlRpcValueBasic(sublanguageid)));
            s.Members.Add(new XmlRpcStructMember("idmovieimdb", new XmlRpcValueBasic(idmovieimdb)));
            s.Members.Add(new XmlRpcStructMember("comment", new XmlRpcValueBasic(comment)));
            parms.Add(s);
            XmlRpcMethodCall call = new XmlRpcMethodCall("AddRequest", parms);

            OSHConsole.WriteLine("Sending AddRequest request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseAddRequest R = new MethodResponseAddRequest();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember dataMemeber in dataStruct.Members)
                                    {
                                        switch (dataMemeber.Name)
                                        {
                                            case "request_url": R.request_url = dataMemeber.Data.Data.ToString(); OSHConsole.WriteLine(">" + dataMemeber.Name + "= " + dataMemeber.Data.Data.ToString()); break;
                                        }
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "AddRequest call failed !");
        }
        /*User interface*/
        /// <summary>
        /// Get list of supported subtitle languages
        /// </summary>
        /// <param name="language">ISO639-1 2-letter language code of user's interface language. </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseGetSubLanguages'</returns>
        public static IMethodResponse GetSubLanguages(string language)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueBasic(language));
            XmlRpcMethodCall call = new XmlRpcMethodCall("GetSubLanguages", parms);

            OSHConsole.WriteLine("Sending GetSubLanguages request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseGetSubLanguages R = new MethodResponseGetSubLanguages();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":// array of structs
                                    XmlRpcValueArray array = (XmlRpcValueArray)MEMBER.Data;
                                    foreach (IXmlRpcValue value in array.Values)
                                    {
                                        if (value is XmlRpcValueStruct)
                                        {
                                            XmlRpcValueStruct valueStruct = (XmlRpcValueStruct)value;
                                            SubtitleLanguage lang = new SubtitleLanguage();
                                            OSHConsole.WriteLine(">SubLanguage:");
                                            foreach (XmlRpcStructMember langMemeber in valueStruct.Members)
                                            {
                                                OSHConsole.WriteLine("  >" + langMemeber.Name + "= " + langMemeber.Data.Data.ToString());
                                                switch (langMemeber.Name)
                                                {
                                                    case "SubLanguageID": lang.SubLanguageID = langMemeber.Data.Data.ToString(); break;
                                                    case "LanguageName": lang.LanguageName = langMemeber.Data.Data.ToString(); break;
                                                    case "ISO639": lang.ISO639 = langMemeber.Data.Data.ToString(); break;
                                                }
                                            }
                                            R.Languages.Add(lang);
                                        }
                                        else
                                        {
                                            OSHConsole.WriteLine(">" + MEMBER.Name + "= " +
                                                MEMBER.Data.Data.ToString() + " [Struct expected !]", DebugCode.Warning);
                                        }
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "GetSubLanguages call failed !");
        }
        /// <summary>
        /// Detect language for given strings 
        /// </summary>
        /// <param name="texts">Array of strings you want language detected for</param>
        /// <param name="encodingUsed">The encoding that will be used to get buffer of given strings. (this is not OS official parameter)</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseDetectLanguage'</returns>
        public static IMethodResponse DetectLanguage(string[] texts, Encoding encodingUsed)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            // We need to gzip texts then code them with base 24
            List<string> decodedTexts = new List<string>();
            foreach (string text in texts)
            {
                // compress
                Stream str = new MemoryStream();
                byte[] stringData = encodingUsed.GetBytes(text);
                str.Write(stringData, 0, stringData.Length);
                str.Position = 0;
                byte[] data = Utilities.Compress(str);
                //base 64
                decodedTexts.Add(Convert.ToBase64String(data));
            }
            parms.Add(new XmlRpcValueArray(decodedTexts.ToArray()));
            XmlRpcMethodCall call = new XmlRpcMethodCall("DetectLanguage", parms);

            OSHConsole.WriteLine("Sending DetectLanguage request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseDetectLanguage R = new MethodResponseDetectLanguage();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    if (MEMBER.Data is XmlRpcValueStruct)
                                    {
                                        OSHConsole.WriteLine(">Languages:");
                                        XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                        foreach (XmlRpcStructMember dataMember in dataStruct.Members)
                                        {
                                            DetectLanguageResult lang = new DetectLanguageResult();
                                            lang.InputSample = dataMember.Name;
                                            lang.LanguageID = dataMember.Data.Data.ToString();
                                            R.Results.Add(lang);
                                            OSHConsole.WriteLine("  >" + dataMember.Name + " (" + dataMember.Data.Data.ToString() + ")");
                                        }
                                    }
                                    else
                                    {
                                        OSHConsole.WriteLine(">Languages ?? Struct expected but server return another type!!", DebugCode.Warning);
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "DetectLanguage call failed !");
        }
        /// <summary>
        /// Get available translations for given program
        /// </summary>
        /// <param name="program">Name of the program/client application you want translations for. Currently supported values: subdownloader, oscar</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseGetAvailableTranslations'</returns>
        public static IMethodResponse GetAvailableTranslations(string program)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueBasic(program));
            XmlRpcMethodCall call = new XmlRpcMethodCall("GetAvailableTranslations", parms);

            OSHConsole.WriteLine("Sending GetAvailableTranslations request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseGetAvailableTranslations R = new MethodResponseGetAvailableTranslations();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    OSHConsole.WriteLine(">data:");
                                    foreach (XmlRpcStructMember dataMember in dataStruct.Members)
                                    {
                                        if (dataMember.Data is XmlRpcValueStruct)
                                        {
                                            XmlRpcValueStruct resStruct = (XmlRpcValueStruct)dataMember.Data;
                                            GetAvailableTranslationsResult res = new GetAvailableTranslationsResult();
                                            res.LanguageID = dataMember.Name;
                                            OSHConsole.WriteLine("  >LanguageID: " + dataMember.Name);
                                            foreach (XmlRpcStructMember resMember in resStruct.Members)
                                            {
                                                switch (resMember.Name)
                                                {
                                                    case "LastCreated": res.LastCreated = resMember.Data.Data.ToString(); OSHConsole.WriteLine("  >" + resMember.Name + "= " + resMember.Data.Data.ToString()); break;
                                                    case "StringsNo": res.StringsNo = resMember.Data.Data.ToString(); OSHConsole.WriteLine("  >" + resMember.Name + "= " + resMember.Data.Data.ToString()); break;
                                                }
                                                R.Results.Add(res);
                                            }
                                        }
                                        else
                                        {
                                            OSHConsole.WriteLine("  >Struct expected !!", DebugCode.Warning);
                                        }
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "GetAvailableTranslations call failed !");
        }
        /// <summary>
        /// Get a translation for given program and language 
        /// </summary>
        /// <param name="iso639">language ​ISO639-1 2-letter code </param>
        /// <param name="format">available formats: [gnugettext compatible: mo, po] and [additional: txt, xml]</param>
        /// <param name="program">Name of the program/client application you want translations for. (currently supported values: subdownloader, oscar)</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseGetTranslation'</returns>
        public static IMethodResponse GetTranslation(string iso639, string format, string program)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueBasic(iso639));
            parms.Add(new XmlRpcValueBasic(format));
            parms.Add(new XmlRpcValueBasic(program));
            XmlRpcMethodCall call = new XmlRpcMethodCall("GetTranslation", parms);

            OSHConsole.WriteLine("Sending GetTranslation request to the server ...", DebugCode.Good);
            // Send the request to the server
            //File.WriteAllText(".\\REQUEST_GetTranslation.xml", Encoding.ASCII.GetString(XmlRpcGenerator.Generate(call)));
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseGetTranslation R = new MethodResponseGetTranslation();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data": R.ContentData = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "GetTranslation call failed !");
        }
        /// <summary>
        /// Check for the latest version of given application 
        /// </summary>
        /// <param name="program">name of the program/client application you want to check. (Currently supported values: subdownloader, oscar)</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseAutoUpdate'</returns>
        public static IMethodResponse AutoUpdate(string program)
        {
            /*if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }*/
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            // parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueBasic(program));
            // parms.Add(new XmlRpcValueBasic(XML_PRC_USERAGENT));

            XmlRpcMethodCall call = new XmlRpcMethodCall("AutoUpdate", parms);
            OSHConsole.WriteLine("Sending AutoUpdate request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseAutoUpdate R = new MethodResponseAutoUpdate();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "version": R.version = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "url_windows": R.url_windows = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "url_linux": R.url_linux = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "comments": R.comments = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "AutoUpdate call failed !");
        }
        /*Checking*/
        /// <summary>
        /// Check if video file hashes are already stored in the database
        /// </summary>
        /// <param name="hashes">Array of video file hashes</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseCheckMovieHash'</returns>
        public static IMethodResponse CheckMovieHash(string[] hashes)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueArray(hashes));
            XmlRpcMethodCall call = new XmlRpcMethodCall("CheckMovieHash", parms);

            OSHConsole.WriteLine("Sending CheckMovieHash request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseCheckMovieHash R = new MethodResponseCheckMovieHash();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    OSHConsole.WriteLine(">Data:");
                                    foreach (XmlRpcStructMember dataMember in dataStruct.Members)
                                    {
                                        CheckMovieHashResult res = new CheckMovieHashResult();
                                        res.Name = dataMember.Name;
                                        OSHConsole.WriteLine("  >" + res.Name + ":");
                                        XmlRpcValueStruct movieStruct = (XmlRpcValueStruct)dataMember.Data;
                                        foreach (XmlRpcStructMember movieMember in movieStruct.Members)
                                        {
                                            switch (movieMember.Name)
                                            {
                                                case "MovieHash": res.MovieHash = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                case "MovieImdbID": res.MovieImdbID = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                case "MovieName": res.MovieName = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                case "MovieYear": res.MovieYear = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                            }
                                        }
                                        R.Results.Add(res);
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "CheckMovieHash call failed !");
        }
        /// <summary>
        /// Check if video file hashes are already stored in the database. This method returns matching !MovieImdbID, MovieName, MovieYear, SeriesSeason, SeriesEpisode, 
        /// MovieKind if available for each $moviehash, always sorted by SeenCount DESC.
        /// </summary>
        /// <param name="hashes">Array of video file hashes</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseCheckMovieHash2'</returns>
        public static IMethodResponse CheckMovieHash2(string[] hashes)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueArray(hashes));
            XmlRpcMethodCall call = new XmlRpcMethodCall("CheckMovieHash2", parms);

            OSHConsole.WriteLine("Sending CheckMovieHash2 request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseCheckMovieHash2 R = new MethodResponseCheckMovieHash2();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    OSHConsole.WriteLine(">Data:");
                                    foreach (XmlRpcStructMember dataMember in dataStruct.Members)
                                    {
                                        CheckMovieHash2Result res = new CheckMovieHash2Result();
                                        res.Name = dataMember.Name;
                                        OSHConsole.WriteLine("  >" + res.Name + ":");

                                        XmlRpcValueArray dataArray = (XmlRpcValueArray)dataMember.Data;
                                        foreach (XmlRpcValueStruct movieStruct in dataArray.Values)
                                        {
                                            CheckMovieHash2Data d = new CheckMovieHash2Data();
                                            foreach (XmlRpcStructMember movieMember in movieStruct.Members)
                                            {
                                                switch (movieMember.Name)
                                                {
                                                    case "MovieHash": d.MovieHash = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "MovieImdbID": d.MovieImdbID = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "MovieName": d.MovieName = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "MovieYear": d.MovieYear = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "MovieKind": d.MovieKind = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "SeriesSeason": d.SeriesSeason = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "SeriesEpisode": d.SeriesEpisode = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                    case "SeenCount": d.MovieYear = movieMember.Data.Data.ToString(); OSHConsole.WriteLine("    >" + movieMember.Name + "= " + movieMember.Data.Data.ToString()); break;
                                                }
                                            }
                                            res.Items.Add(d);
                                        }
                                        R.Results.Add(res);
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "CheckMovieHash2 call failed !");
        }
        /// <summary>
        /// Check if given subtitle files are already stored in the database 
        /// </summary>
        /// <param name="hashes">Array of subtitle file hashes (MD5 hashes of subtitle file contents) </param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseCheckSubHash'</returns>
        public static IMethodResponse CheckSubHash(string[] hashes)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            parms.Add(new XmlRpcValueArray(hashes));
            XmlRpcMethodCall call = new XmlRpcMethodCall("CheckSubHash", parms);

            OSHConsole.WriteLine("Sending CheckSubHash request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseCheckSubHash R = new MethodResponseCheckSubHash();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    OSHConsole.WriteLine(">Data:");
                                    XmlRpcValueStruct dataStruct = (XmlRpcValueStruct)MEMBER.Data;
                                    foreach (XmlRpcStructMember dataMember in dataStruct.Members)
                                    {
                                        OSHConsole.WriteLine("  >" + dataMember.Name + "= " + dataMember.Data.Data.ToString());
                                        CheckSubHashResult r = new CheckSubHashResult();
                                        r.Hash = dataMember.Name;
                                        r.SubID = dataMember.Data.Data.ToString();
                                        R.Results.Add(r);
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "CheckSubHash call failed !");
        }
        /*Upload*/
        /// <summary>
        /// Try to upload subtitles, perform pre-upload checking (i.e. check if subtitles already exist on server) 
        /// </summary>
        /// <param name="subs">The subtitle parameters collection to try to upload</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseTryUploadSubtitles'</returns>
        public static IMethodResponse TryUploadSubtitles(TryUploadSubtitlesParameters[] subs)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN, XmlRpcBasicValueType.String));
            XmlRpcValueStruct s = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            int i = 1;
            foreach (TryUploadSubtitlesParameters cd in subs)
            {
                XmlRpcStructMember member = new XmlRpcStructMember("cd" + i, null);
                XmlRpcValueStruct memberStruct = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                memberStruct.Members.Add(new XmlRpcStructMember("subhash", new XmlRpcValueBasic(cd.subhash)));
                memberStruct.Members.Add(new XmlRpcStructMember("subfilename", new XmlRpcValueBasic(cd.subfilename)));
                memberStruct.Members.Add(new XmlRpcStructMember("moviehash", new XmlRpcValueBasic(cd.moviehash)));
                memberStruct.Members.Add(new XmlRpcStructMember("moviebytesize", new XmlRpcValueBasic(cd.moviebytesize)));
                memberStruct.Members.Add(new XmlRpcStructMember("moviefps", new XmlRpcValueBasic(cd.moviefps)));
                memberStruct.Members.Add(new XmlRpcStructMember("movietimems", new XmlRpcValueBasic(cd.movietimems)));
                memberStruct.Members.Add(new XmlRpcStructMember("movieframes", new XmlRpcValueBasic(cd.movieframes)));
                memberStruct.Members.Add(new XmlRpcStructMember("moviefilename", new XmlRpcValueBasic(cd.moviefilename)));
                member.Data = memberStruct;
                s.Members.Add(member);
                i++;
            }
            parms.Add(s);
            XmlRpcMethodCall call = new XmlRpcMethodCall("TryUploadSubtitles", parms);

            OSHConsole.WriteLine("Sending TryUploadSubtitles request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseTryUploadSubtitles R = new MethodResponseTryUploadSubtitles();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "alreadyindb": R.AlreadyInDB = (int)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data":
                                    if (MEMBER.Data is XmlRpcValueArray)
                                    {
                                        OSHConsole.WriteLine("Results: ");

                                        XmlRpcValueArray rarray = (XmlRpcValueArray)MEMBER.Data;
                                        foreach (IXmlRpcValue subStruct in rarray.Values)
                                        {
                                            if (subStruct == null) continue;
                                            if (!(subStruct is XmlRpcValueStruct)) continue;

                                            SubtitleSearchResult result = new SubtitleSearchResult();
                                            foreach (XmlRpcStructMember submember in ((XmlRpcValueStruct)subStruct).Members)
                                            {
                                                // To avoid errors of arranged info or missing ones, let's do it with switch..
                                                switch (submember.Name)
                                                {
                                                    case "IDMovie": result.IDMovie = submember.Data.Data.ToString(); break;
                                                    case "IDMovieImdb": result.IDMovieImdb = submember.Data.Data.ToString(); break;
                                                    case "IDSubMovieFile": result.IDSubMovieFile = submember.Data.Data.ToString(); break;
                                                    case "IDSubtitle": result.IDSubtitle = submember.Data.Data.ToString(); break;
                                                    case "IDSubtitleFile": result.IDSubtitleFile = submember.Data.Data.ToString(); break;
                                                    case "ISO639": result.ISO639 = submember.Data.Data.ToString(); break;
                                                    case "LanguageName": result.LanguageName = submember.Data.Data.ToString(); break;
                                                    case "MovieByteSize": result.MovieByteSize = submember.Data.Data.ToString(); break;
                                                    case "MovieHash": result.MovieHash = submember.Data.Data.ToString(); break;
                                                    case "MovieImdbRating": result.MovieImdbRating = submember.Data.Data.ToString(); break;
                                                    case "MovieName": result.MovieName = submember.Data.Data.ToString(); break;
                                                    case "MovieNameEng": result.MovieNameEng = submember.Data.Data.ToString(); break;
                                                    case "MovieReleaseName": result.MovieReleaseName = submember.Data.Data.ToString(); break;
                                                    case "MovieTimeMS": result.MovieTimeMS = submember.Data.Data.ToString(); break;
                                                    case "MovieYear": result.MovieYear = submember.Data.Data.ToString(); break;
                                                    case "SubActualCD": result.SubActualCD = submember.Data.Data.ToString(); break;
                                                    case "SubAddDate": result.SubAddDate = submember.Data.Data.ToString(); break;
                                                    case "SubAuthorComment": result.SubAuthorComment = submember.Data.Data.ToString(); break;
                                                    case "SubBad": result.SubBad = submember.Data.Data.ToString(); break;
                                                    case "SubDownloadLink": result.SubDownloadLink = submember.Data.Data.ToString(); break;
                                                    case "SubDownloadsCnt": result.SubDownloadsCnt = submember.Data.Data.ToString(); break;
                                                    case "SubFileName": result.SubFileName = submember.Data.Data.ToString(); break;
                                                    case "SubFormat": result.SubFormat = submember.Data.Data.ToString(); break;
                                                    case "SubHash": result.SubHash = submember.Data.Data.ToString(); break;
                                                    case "SubLanguageID": result.SubLanguageID = submember.Data.Data.ToString(); break;
                                                    case "SubRating": result.SubRating = submember.Data.Data.ToString(); break;
                                                    case "SubSize": result.SubSize = submember.Data.Data.ToString(); break;
                                                    case "SubSumCD": result.SubSumCD = submember.Data.Data.ToString(); break;
                                                    case "UserID": result.UserID = submember.Data.Data.ToString(); break;
                                                    case "UserNickName": result.UserNickName = submember.Data.Data.ToString(); break;
                                                    case "ZipDownloadLink": result.ZipDownloadLink = submember.Data.Data.ToString(); break;
                                                }
                                            }
                                            R.Results.Add(result);
                                            OSHConsole.WriteLine(">" + result.ToString());
                                        }
                                    }
                                    else// Unknown data ?
                                    {
                                        OSHConsole.WriteLine("Data= " + MEMBER.Data.Data.ToString(), DebugCode.Warning);
                                    }
                                    break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "TryUploadSubtitles call failed !");
        }
        /// <summary>
        /// Upload given subtitles to OSDb server 
        /// </summary>
        /// <param name="info">The pamaters of upload method</param>
        /// <returns>Status of the call operation. If the call success the response will be 'MethodResponseUploadSubtitles'</returns>
        public static IMethodResponse UploadSubtitles(UploadSubtitleInfoParameters info)
        {
            if (TOKEN == "")
            {
                OSHConsole.WriteLine("Can't do this call, 'token' value not set. Please use Log In method first.", DebugCode.Error);
                return new MethodResponseError("Fail", "Can't do this call, 'token' value not set. Please use Log In method first.");
            }
            // Method call ..
            List<IXmlRpcValue> parms = new List<IXmlRpcValue>();
            parms.Add(new XmlRpcValueBasic(TOKEN));
            // Main struct
            XmlRpcValueStruct s = new XmlRpcValueStruct(new List<XmlRpcStructMember>());

            // Base info member as struct
            XmlRpcStructMember member = new XmlRpcStructMember("baseinfo", null);
            XmlRpcValueStruct memberStruct = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
            memberStruct.Members.Add(new XmlRpcStructMember("idmovieimdb", new XmlRpcValueBasic(info.idmovieimdb)));
            memberStruct.Members.Add(new XmlRpcStructMember("sublanguageid", new XmlRpcValueBasic(info.sublanguageid))); 
            memberStruct.Members.Add(new XmlRpcStructMember("moviereleasename", new XmlRpcValueBasic(info.moviereleasename)));
            memberStruct.Members.Add(new XmlRpcStructMember("movieaka", new XmlRpcValueBasic(info.movieaka)));
            memberStruct.Members.Add(new XmlRpcStructMember("subauthorcomment", new XmlRpcValueBasic(info.subauthorcomment)));
            // memberStruct.Members.Add(new XmlRpcStructMember("hearingimpaired", new XmlRpcValueBasic(info.hearingimpaired)));
            // memberStruct.Members.Add(new XmlRpcStructMember("highdefinition", new XmlRpcValueBasic(info.highdefinition)));
            // memberStruct.Members.Add(new XmlRpcStructMember("automatictranslation", new XmlRpcValueBasic(info.automatictranslation)));
            member.Data = memberStruct;
            s.Members.Add(member);

            // CDS members
            int i = 1;
            foreach (UploadSubtitleParameters cd in info.CDS)
            {
                XmlRpcStructMember member2 = new XmlRpcStructMember("cd" + i, null);
                XmlRpcValueStruct memberStruct2 = new XmlRpcValueStruct(new List<XmlRpcStructMember>());
                memberStruct2.Members.Add(new XmlRpcStructMember("subhash", new XmlRpcValueBasic(cd.subhash)));
                memberStruct2.Members.Add(new XmlRpcStructMember("subfilename", new XmlRpcValueBasic(cd.subfilename)));
                memberStruct2.Members.Add(new XmlRpcStructMember("moviehash", new XmlRpcValueBasic(cd.moviehash)));
                memberStruct2.Members.Add(new XmlRpcStructMember("moviebytesize", new XmlRpcValueBasic(cd.moviebytesize)));
                memberStruct2.Members.Add(new XmlRpcStructMember("moviefps", new XmlRpcValueBasic(cd.moviefps)));
                memberStruct2.Members.Add(new XmlRpcStructMember("movietimems", new XmlRpcValueBasic(cd.movietimems)));
                memberStruct2.Members.Add(new XmlRpcStructMember("movieframes", new XmlRpcValueBasic(cd.movieframes)));
                memberStruct2.Members.Add(new XmlRpcStructMember("moviefilename", new XmlRpcValueBasic(cd.moviefilename)));
                memberStruct2.Members.Add(new XmlRpcStructMember("subcontent", new XmlRpcValueBasic(cd.subcontent)));
                member2.Data = memberStruct2;
                s.Members.Add(member2);
                i++;
            }

            // add main struct to parameters
            parms.Add(s);
            // add user agent
            //parms.Add(new XmlRpcValueBasic(XML_PRC_USERAGENT));
            XmlRpcMethodCall call = new XmlRpcMethodCall("UploadSubtitles", parms);
            OSHConsole.WriteLine("Sending UploadSubtitles request to the server ...", DebugCode.Good);
            // Send the request to the server
            string response = Utilities.GetStreamString(Utilities.SendRequest(XmlRpcGenerator.Generate(call), XML_PRC_USERAGENT));
            if (!response.Contains("ERROR:"))
            {
                // No error occur, get and decode the response. 
                XmlRpcMethodCall[] calls = XmlRpcGenerator.DecodeMethodResponse(response);
                if (calls.Length > 0)
                {
                    if (calls[0].Parameters.Count > 0)
                    {
                        XmlRpcValueStruct mainStruct = (XmlRpcValueStruct)calls[0].Parameters[0];
                        // Create the response, we'll need it later
                        MethodResponseUploadSubtitles R = new MethodResponseUploadSubtitles();

                        // To make sure response is not currepted by server, do it in loop
                        foreach (XmlRpcStructMember MEMBER in mainStruct.Members)
                        {
                            switch (MEMBER.Name)
                            {
                                case "status": R.Status = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "seconds": R.Seconds = (double)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "data": R.Data = (string)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                                case "subtitles": R.SubTitles = (bool)MEMBER.Data.Data; OSHConsole.WriteLine(">" + MEMBER.Name + "= " + MEMBER.Data.Data.ToString()); break;
                            }
                        }
                        // Return the response to user !!
                        return R;
                    }
                }
            }
            else
            {
                OSHConsole.WriteLine(response, DebugCode.Error);
                return new MethodResponseError("Fail", response);
            }
            return new MethodResponseError("Fail", "UploadSubtitles call failed !");
        }
    }
}
